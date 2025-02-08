namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Globalization;
using System.Text.RegularExpressions;
using Domain;
using global::Serilog;

internal static partial class AppSessionInfoBuilder
{
    private static readonly string[] SystemLevelPackageNames =
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
        var packageNameMatch = AppSessionRegexes.PackageNameRegex().Match(info);
        if (!packageNameMatch.Success)
            return null;

        var packageName = GetValueFromMatch(packageNameMatch);

        // We skip system level applications such as com.android.settings (The settings application for the emulator)
        if (SystemLevelPackageNames.Any(partialPackageName => packageName.StartsWith(partialPackageName)))
        {
            Log.Verbose("Skipping system level application. {PackageName}", packageName);
            return null;
        }

        var titleMatch = AppSessionRegexes.TitleRegex().Match(info);
        if (!titleMatch.Success)
            return null;

        var startedTimestampMatch = AppSessionRegexes.StartedTimestampRegex().Match(info);
        if (!startedTimestampMatch.Success)
            return null;

        var stateMatch = AppSessionRegexes.AppSessionStateRegex().Match(info);
        if (!stateMatch.Success)
            return null;

        var startedTimestampAsString = GetValueFromMatch(startedTimestampMatch);

        if (!DateTimeOffset.TryParseExact(startedTimestampAsString, AppSessionRegexes.STARTED_TIMESTAMP_FORMAT, null, DateTimeStyles.None , out var startedTimestamp))
        {
            Log.Warning("Failed to parse started timestamp: {StartedTimestampString}", startedTimestampAsString);
            return null;
        }

        var stateAsString = GetValueFromMatch(stateMatch);

        if (!Enum.TryParse<AppSessionState>(stateAsString, out var appState))
        {
            Log.Warning("Failed to parse app state: {AppStateString}", stateAsString);
            return null;
        }

        var title = GetValueFromMatch(titleMatch);

        return new PlayGamesSessionInfo(packageName, startedTimestamp, title, appState)
        {
            RawText = info
        };
    }

    private static string GetValueFromMatch(Match match) => match.Groups[1].Value.Replace("\r", string.Empty);

    private const string ANDROID_LAUNCHER_HINT = "com.android.launcher";
    public static PlayGamesSessionInfo? BuildFromEmulatorState(string info)
    {
        var packageNameMatch = EmulatorStateRegexes.DisplayedTaskPackageName().Match(info);
        if (!packageNameMatch.Success)
            return null;

        var displayedTaskPackageName = GetValueFromMatch(packageNameMatch);

        // We skip system level applications such as com.android.settings (The settings application for the emulator)
        if (!displayedTaskPackageName.StartsWith(ANDROID_LAUNCHER_HINT) && SystemLevelPackageNames.Any(partialPackageName => displayedTaskPackageName.StartsWith(partialPackageName)))
        {
            Log.Verbose("Skipping system level application. {PackageName}", displayedTaskPackageName);
            return null;
        }

        var foregroundPackageNameMatch = EmulatorStateRegexes.ForegroundPackageName().Match(info);
        if (!packageNameMatch.Success)
            return null;

        var foregroundPackageName = GetValueFromMatch(foregroundPackageNameMatch);


        var packageName = foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT)
            ? displayedTaskPackageName
            : foregroundPackageName;

        if (string.IsNullOrWhiteSpace(packageName) || packageName.StartsWith(ANDROID_LAUNCHER_HINT))
            return null;

        var startedTimestampMatch = EmulatorStateRegexes.StartedTimestampRegex().Match(info);
        if (!startedTimestampMatch.Success)
            return null;

        var stateMatch = EmulatorStateRegexes.AppSessionStateRegex().Match(info);
        if (!stateMatch.Success)
            return null;

        var stateAsString = GetValueFromMatch(stateMatch);

        var appState = AppSessionState.None;

        if (foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT) || displayedTaskPackageName.StartsWith(ANDROID_LAUNCHER_HINT))
            appState = foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT)
                ? AppSessionState.Stopped
                : AppSessionState.Starting;

        if (appState == AppSessionState.None)
        {
            if (!Enum.TryParse(stateAsString, out appState))
            {
                Log.Warning("Failed to parse app state: {AppStateString}", stateAsString);
                return null;
            }
        }

        DateTimeOffset startedTimestamp = default;
        if (appState != AppSessionState.Starting)
        {
            var startedTimestampAsString = GetValueFromMatch(startedTimestampMatch);

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
                                                       // 250207 17:59:08.311+00:00
        internal const string STARTED_TIMESTAMP_FORMAT = "yyMMdd HH:mm:ss.fffK";

        [GeneratedRegex("foreground_task=(.+?) }", RegexOptions.Multiline)]
        internal static partial Regex ForegroundPackageName(); // foreground_task=com.YoStarEN.Arknights }

        [GeneratedRegex("task=(.+?), ", RegexOptions.Multiline)]
        internal static partial Regex DisplayedTaskPackageName(); //       { display_id=0, task=com.YoStarEN.Arknights, foreground=True },

        [GeneratedRegex("readyTimestamp=(.+?)$", RegexOptions.Multiline)]
        internal static partial Regex StartedTimestampRegex(); // readyTimestamp=250207 17:59:08.311+00:00

        [GeneratedRegex("status=(.+?)$", RegexOptions.Multiline)]
        internal static partial Regex AppSessionStateRegex(); // status=Running
    }

}
