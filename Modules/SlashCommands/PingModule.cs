using Discord.Interactions;
using System.Diagnostics;

namespace QuestsBot.Modules.SlashCommands
{
    public class PingModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ping", "Check the bot's latency.")]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            await RespondAsync("Pong!");
            sw.Stop();

            await ModifyOriginalResponseAsync((p) =>
            {
                p.Content = $"Pong! ({sw.ElapsedMilliseconds}ms)";
            });
        }
    }
}