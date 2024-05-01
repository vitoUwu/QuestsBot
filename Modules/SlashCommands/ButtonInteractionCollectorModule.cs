using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using QuestsBot.Services;
using System.Diagnostics;

namespace QuestsBot.Modules.SlashCommands
{
    public class ButtonInteractionCollectorModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly string initialButtonLabel = "Click me!";
        private readonly string clickedButtonLabel = "You clicked me!";
        private readonly string buttonTimeoutMessage = "No interactions were made in time.";
        private readonly string buttonReplyMessage = "You clicked successfully!";

        private readonly int buttonInteractionTimeout = 5000;
        private readonly Dictionary<string, AutoResetEvent> autoResetEvents = new();
        private readonly LoggingService _logger;

        public ButtonInteractionCollectorModule(LoggingService logger)
        {
            _logger = logger;
        }

        [SlashCommand("button", "Button interaction collector test.")]
        public async Task ButtonInteractionCollectorAsync()
        {
            var button = new ButtonBuilder()
                .WithCustomId(GetButtonId())
                .WithLabel(initialButtonLabel)
                .WithStyle(ButtonStyle.Primary);

            var components = new ComponentBuilder()
                .WithButton(button)
                .Build();

            await RespondAsync("Click the button below to test the collector.", components: components);

            await AwaitButtonInteraction();
        }

        private string GetButtonId()
        {
            return $"{Context.Interaction.Id}_button";
        }

        private string GetAutoResetEventKey(ulong userId)
        {
            return $"{userId}_{Context.Interaction.Id}";
        }

        private async Task AwaitButtonInteraction()
        {
            var sw = Stopwatch.StartNew();

            Context.Client.ButtonExecuted += ButtonHandler;

            var auto = new AutoResetEvent(false);
            autoResetEvents.Add(GetAutoResetEventKey(Context.User.Id), auto);

            var timer = new Timer(HandleButtonTimeout, null, buttonInteractionTimeout, Timeout.Infinite);

            auto.WaitOne();
            timer.Dispose();
            auto.Dispose();

            Context.Client.ButtonExecuted -= ButtonHandler;
            autoResetEvents.Remove(GetAutoResetEventKey(Context.User.Id));

            await _logger.LogAsync(
                new LogMessage(
                    LogSeverity.Debug,
                    nameof(AwaitButtonInteraction),
                    $"Button interaction collector test completed in {sw.ElapsedMilliseconds}ms."
                )
            );
        }

        private async void HandleButtonTimeout(object? data)
        {
            await ModifyOriginalResponseAsync((p) =>
            {
                p.Content = "No interactions were made in time.";
                p.Components = null;
            });

            if (autoResetEvents.TryGetValue(GetAutoResetEventKey(Context.User.Id), out var auto))
            {
                await _logger.LogAsync(new LogMessage(LogSeverity.Debug, nameof(HandleButtonTimeout), "Collector has been disposed by inactivity."));
                auto.Set();
            }
        }

        public async Task ButtonHandler(SocketMessageComponent component)
        {
            if (component.Data.CustomId != GetButtonId())
                return;

            if (component.User.Id != Context.User.Id)
            {
                await component.RespondAsync("You are not allowed to interact with this button.", ephemeral: true);
                return;
            }

            await component.UpdateAsync((p) =>
            {
                var disabledButton = new ButtonBuilder()
                    .WithCustomId(GetButtonId())
                    .WithLabel(clickedButtonLabel)
                    .WithStyle(ButtonStyle.Primary)
                    .WithDisabled(true);

                p.Components = new ComponentBuilder()
                    .WithButton(disabledButton)
                    .Build();
                p.Content = buttonReplyMessage;
            });

            if (autoResetEvents.TryGetValue(GetAutoResetEventKey(component.User.Id), out var auto))
            {
                await _logger.LogAsync(new LogMessage(LogSeverity.Debug, nameof(ButtonHandler), "Collector has been disposed by max interactions."));
                auto.Set();
            }
        }
    }
}
