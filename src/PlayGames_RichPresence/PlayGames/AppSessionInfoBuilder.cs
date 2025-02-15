namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Globalization;
using System.Text.RegularExpressions;
using Domain;
using global::Serilog;

internal static partial class AppSessionInfoBuilder
{
    private static bool TryParseRegex(Regex regex, string info, out string value)
    {
        var match = regex.Match(info);
        if (!match.Success)
        {
            value = string.Empty;
            return false;
        }

        value = GetValueFromMatch(match);
        return true;
    }

    private static readonly string[] SystemLevelPackageHints =
        [
            "com.android",
            "com.google"
        ];

    private static partial class AppSessionRegexes
    {
                                                       // 9/24/2024 1:05:02 PM +00:00
        internal const string STARTED_TIMESTAMP_FORMAT = "M/d/yyyy h:mm:ss tt zzz";

        [GeneratedRegex("package_name=(.+?)$", RegexOptions.Multiline)]
        internal static partial Regex PackageNameRegex(); // package_name=com.YoStarEN.Arknights

        [GeneratedRegex("title=(.+?)$", RegexOptions.Multiline)]
        internal static partial Regex TitleRegex(); // title=Arknights

        // This is UTC
        [GeneratedRegex("started_timestamp=(.+?)$", RegexOptions.Multiline)]
        internal static partial Regex StartedTimestampRegex(); // started_timestamp=9/24/2024 1:05:02 PM +00:00

        [GeneratedRegex("state=(.+?)=", RegexOptions.Multiline)]
        internal static partial Regex AppSessionStateRegex(); // state=Running={ }
    }
    public static PlayGamesSessionInfo? BuildFromAppSession(string info)
    {
        if (!TryParseRegex(AppSessionRegexes.PackageNameRegex(), info, out var packageName))
            return null;

        // We skip system level applications such as com.android.settings (The settings application for the emulator)
        if (SystemLevelPackageHints.Any(partialPackageName => packageName.StartsWith(partialPackageName)))
        {
            // Log.Verbose("Skipping system level application. {PackageName}", packageName);
            return null;
        }

        if (!(TryParseRegex(AppSessionRegexes.TitleRegex(), info, out var title) &&
            TryParseRegex(AppSessionRegexes.StartedTimestampRegex(), info, out var startedTimestampAsString) &&
            TryParseRegex(AppSessionRegexes.AppSessionStateRegex(), info, out var stateAsString)))
            return null;


        if (!DateTimeOffset.TryParseExact(startedTimestampAsString, AppSessionRegexes.STARTED_TIMESTAMP_FORMAT, null, DateTimeStyles.None , out var startedTimestamp))
        {
            Log.Warning("Failed to parse started timestamp: '{StartedTimestampString}' for {Info}", startedTimestampAsString, info);
            return null;
        }


        if (Enum.TryParse<AppSessionState>(stateAsString, out var appState))
            return new PlayGamesSessionInfo(packageName, startedTimestamp, title, appState) { RawText = info };

        Log.Warning("Failed to parse app state: '{AppStateString}' for {Info}", stateAsString, info);
        return null;
    }

    private static string GetValueFromMatch(Match match) => match.Groups[1].Value.Replace("\r", string.Empty);

    // We prefer matches that does not start with 'com.android.launcher'
    private static string GetOpinionatedValueFromMatch(MatchCollection packageNameMatches)
    {
        return packageNameMatches.Select(x => x.Groups[1].Value).FirstOrDefault(x => !IsSystemLevelPackage(x)) ??
            packageNameMatches.First().Groups[1].Value;
    }

    private static bool IsSystemLevelPackage(string packageName) => SystemLevelPackageHints.Any(packageName.StartsWith);

    private const string ANDROID_LAUNCHER_HINT = "com.android.launcher";
    public static PlayGamesSessionInfo? BuildFromEmulatorState(string info)
    {
        var packageNameMatch = EmulatorStateRegexes.DisplayedTaskPackageName().Matches(info);
        if (packageNameMatch.Count == 0)
            return null;

        var displayedTaskPackageName = GetOpinionatedValueFromMatch(packageNameMatch);

        // We skip system level applications such as com.android.settings (The settings application for the emulator)
        if (IsSystemLevelPackage(displayedTaskPackageName))
        {
            // Log.Verbose("Skipping system level application. {PackageName}", displayedTaskPackageName);
            return null;
        }

        if (!TryParseRegex(EmulatorStateRegexes.ForegroundPackageName(), info, out var foregroundPackageName))
            return null;

        var packageName = IsSystemLevelPackage(foregroundPackageName)
            ? displayedTaskPackageName
            : foregroundPackageName;


        if (string.IsNullOrWhiteSpace(packageName) || packageName.StartsWith(ANDROID_LAUNCHER_HINT))
            return null;

        if (!TryParseRegex(EmulatorStateRegexes.StartedTimestampRegex(), info, out var startedTimestampAsString))
            return null;


        if (!TryParseRegex(EmulatorStateRegexes.AppSessionStateRegex(), info, out var stateAsString))
            return null;

        var appState = AppSessionState.None;

        if (foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT) || displayedTaskPackageName.StartsWith(ANDROID_LAUNCHER_HINT))
            appState = foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT)
                ? AppSessionState.Stopped
                : AppSessionState.Starting;

        if (appState == AppSessionState.None && !Enum.TryParse(stateAsString, out appState))
        {
            Log.Warning("Failed to parse app state: {AppStateString}", stateAsString);
            return null;
        }

        DateTimeOffset startedTimestamp = default;
        if (appState != AppSessionState.Starting)
        {
            if (!DateTimeOffset.TryParseExact(startedTimestampAsString, EmulatorStateRegexes.STARTED_TIMESTAMP_FORMAT, null, DateTimeStyles.None , out startedTimestamp))
            {
                Log.Warning("Failed to parse started timestamp: {StartedTimestampString}", startedTimestampAsString);
                return null;
            }

            startedTimestamp = startedTimestamp.ToUniversalTime();
        }

        var title = packageName.Split('.').Last();

        return new PlayGamesSessionInfo(packageName, startedTimestamp, title, appState)
        {
            RawText = info
        };
    }

    private static partial class EmulatorStateRegexes
    {
                                                       // 250207 18:00:04.030+1
        internal const string STARTED_TIMESTAMP_FORMAT = "yyMMdd HH:mm:ss.fffz";

        [GeneratedRegex("foreground_task=(.+?) }", RegexOptions.Multiline)]
        internal static partial Regex ForegroundPackageName(); // foreground_task=com.YoStarEN.Arknights }

        [GeneratedRegex("task=(.+?), ", RegexOptions.Multiline)]
        internal static partial Regex DisplayedTaskPackageName(); //       { display_id=0, task=com.YoStarEN.Arknights, foreground=True },

        [GeneratedRegex(@"^(.+?\+\d) ", RegexOptions.Multiline)]
        internal static partial Regex StartedTimestampRegex(); // 250207 18:00:04.030+1 39 INFO  EmulatorStateLogger: Emulator state updated:

        // status=Running
        // Stopping (Emulator stopped normally (shutdown)) - lastKnownHealthStatus=No ERROR; emulator is healthy

        [GeneratedRegex("status=(.+?)(?= |$)", RegexOptions.Multiline)]
        internal static partial Regex AppSessionStateRegex();
    }

}
