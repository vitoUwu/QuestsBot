using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using QuestsBot.Services;

namespace QuestsBot
{
    class Program
    {
        private static IServiceProvider? _services;

        private static readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true,
        };

        private static ServiceProvider StartServices()
        {
            return new ServiceCollection()
                .AddSingleton<LoggingService>()
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .BuildServiceProvider();
        }

        public static async Task Main()
        {
            _services = StartServices();

            var client = _services.GetRequiredService<DiscordSocketClient>();
            client.Log += _services.GetRequiredService<LoggingService>().LogAsync;

            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}