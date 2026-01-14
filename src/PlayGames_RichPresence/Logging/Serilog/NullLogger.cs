using Serilog.Events;

namespace Dawn.PlayGames.RichPresence.Logging.Serilog;

public sealed class NullLogger : ILogger
{
    public void Write(LogEvent logEvent) { }
}
