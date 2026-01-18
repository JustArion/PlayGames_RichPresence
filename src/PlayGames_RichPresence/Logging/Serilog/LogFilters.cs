using Serilog.Core;

namespace Dawn.PlayGames.RichPresence.Logging.Serilog;

internal static class LogFilters
{
    public static ILogEventFilter DeduplicationFilter(TimeSpan ts) => new DeduplicationFilter(ts);
}
