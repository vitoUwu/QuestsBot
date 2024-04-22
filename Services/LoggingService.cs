using Discord;
using Discord.Commands;

namespace QuestsBot.Services
{
    public class LoggingService
    {
        public Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{Colorize(message.Severity)}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{Colorize(message.Severity)}] {message}");

            return Task.CompletedTask;
        }

        static private string Colorize(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => $"\u001b[31;1mCritical\u001b[0m",
                LogSeverity.Error => $"\u001b[31mError\u001b[0m",
                LogSeverity.Warning => $"\u001b[33mWarning\u001b[0m",
                LogSeverity.Info => $"\u001b[32mInfo\u001b[0m",
                LogSeverity.Verbose => $"\u001b[34mVerbose\u001b[0m",
                LogSeverity.Debug => $"\u001b[34mDebug\u001b[0m",
                _ => $"\u001b[37m{severity}\u001b[0m"
            };
        }
    }
}