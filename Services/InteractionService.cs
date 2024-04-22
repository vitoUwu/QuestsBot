using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace QuestsBot.Services
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly LoggingService _logger;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, LoggingService logger)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _client.Ready += ReadyAsync;
            _handler.Log += _logger.LogAsync;

            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
            _handler.InteractionExecuted += HandleInteractionExecute;
        }

        private async Task ReadyAsync()
        {
            await _handler.RegisterCommandsGloballyAsync();
            var modules = _handler.Modules.ToList();

            foreach (var module in modules)
            {
                await _logger.LogAsync(new LogMessage(LogSeverity.Info, "InteractionHandler", $"Module {module.Name} registered successfully"));
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                // Due to async nature of InteractionFramework, the result here may always be success.
                // That's why we also need to handle the InteractionExecuted event.
                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            await HandleUnmetPrecondition(context, result.ErrorReason);
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        HandleUnmetPrecondition(context, result.ErrorReason);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                string username = context.Interaction.User.Username;
                string location = context.Interaction.IsDMInteraction ? "DM" : $"{context.Guild.Name} ({context.Guild.Id})";
                int executionTime = (DateTime.Now - context.Interaction.CreatedAt).Milliseconds;

                _logger.LogAsync(
                    new LogMessage(
                        LogSeverity.Info,
                        "InteractionHandler",
                        $"{username} used /{commandInfo.Name} in {location}. Execution time: {executionTime}ms"
                    )
                );
            }

            return Task.CompletedTask;
        }

        private Task HandleUnmetPrecondition(IInteractionContext context, string errorReason)
        {
            _logger.LogAsync(
                new LogMessage(
                    LogSeverity.Warning,
                    "InteractionHandler",
                    $"Precondition failed for {context.Interaction.Type} command. {errorReason}"
                )
            );
            return Task.CompletedTask;
        }
    }
}