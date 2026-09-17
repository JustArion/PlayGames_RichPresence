using System.Runtime.Versioning;

namespace Dawn.PlayGames.RichPresence.Extensions;

public static class ConsoleEx
{
    extension(Console)
    {
        [SupportedOSPlatform("windows")]
        public static bool Attach() => AttachConsole(ATTACH_PARENT_PROCESS);
    }
}
