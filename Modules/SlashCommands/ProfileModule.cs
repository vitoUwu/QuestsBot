using Discord;
using Discord.Interactions;
using QuestsBot.Services;

namespace QuestsBot.Modules.SlashCommands
{
    public class ProfileModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly LoggingService _logger;

        public ProfileModule(LoggingService logger)
        {
            _logger = logger;
        }

        [SlashCommand("profile", "View your profile.")]
        [CommandContextType(InteractionContextType.Guild)]
        public async Task ProfileAsync()
        {
            var member = Context.Guild.GetUser(Context.User.Id);
            if (member == null)
            {
                await _logger.LogAsync(
                    new LogMessage(
                        LogSeverity.Error,
                        "ProfileModule",
                        $"Guild Member {Context.User.Username} ({Context.User.Id}) not found in {Context.Guild.Name}"
                    )
                );
                await RespondAsync("An error has occured, try again");
                return;
            }

            var highestRole = member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
            var color = highestRole?.Color ?? Color.Default;

            var embed = new EmbedBuilder()
                .WithTitle(member.Username)
                .WithDescription("You have no badges yet")
                .WithThumbnailUrl(member.GetAvatarUrl())
                .WithColor(color)
                .Build();

            await RespondAsync(embed: embed);
        }
    }
}
