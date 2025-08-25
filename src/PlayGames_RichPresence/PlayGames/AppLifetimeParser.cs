using System.Text;
using Dawn.PlayGames.RichPresence.Models;

namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Globalization;
using System.Text.RegularExpressions;

internal static partial class AppLifetimeParser
{
    internal static bool IsSystemLevelPackage(string packageName) => SystemLevelPackageHints.Any(packageName.StartsWith);

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
    private static string GetValueFromMatch(Match match) => match.Groups[1].Value.Replace("\r", string.Empty);


    private static readonly string[] SystemLevelPackageHints =
        [
            "com.android",
            "com.google"
        ];

    private static partial class AppSessionRegexes
    {
                                                       // 9/24/2024 1:05:02 PM +00:00
        internal const string STARTED_TIMESTAMP_FORMAT = "M/d/yyyy h:mm:ss tt zzz";

        [GeneratedRegex("package_name=(?'PackageName'.+?)$", RegexOptions.Multiline)]
        internal static partial Regex PackageNameRegex(); // package_name=com.YoStarEN.Arknights

        [GeneratedRegex("title=(?'Title'.+?)$", RegexOptions.Multiline)]
        internal static partial Regex TitleRegex(); // title=Arknights

        // This is UTC
        [GeneratedRegex("started_timestamp=(?'StartTimestamp'.+?)$", RegexOptions.Multiline)]
        internal static partial Regex StartedTimestampRegex(); // started_timestamp=9/24/2024 1:05:02 PM +00:00

        [GeneratedRegex("state=(?'AppSessionState'.+?)=", RegexOptions.Multiline)]
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


        if (!DateTimeOffset.TryParseExact(startedTimestampAsString, AppSessionRegexes.STARTED_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None , out var startedTimestamp))
        {
            Log.Warning("Failed to parse started timestamp: '{StartedTimestampString}' for {Info}", startedTimestampAsString, info);
            return null;
        }


        if (Enum.TryParse<AppSessionState>(stateAsString, out var appState))
            return new PlayGamesSessionInfo(packageName, startedTimestamp, title, appState) { RawText = info };

        Log.Warning("Failed to parse app state: '{AppStateString}' for {Info}", stateAsString, info);
        return null;
    }




    private static string GetPrioritizedValue(MatchCollection matches, Func<Match, bool> predicate)
    {
        var nonSystemPackage = matches.FirstOrDefault(predicate);
        return nonSystemPackage == null
            ? GetValueFromMatch(matches.First())
            : GetValueFromMatch(nonSystemPackage);
    }

    private const string ANDROID_LAUNCHER_HINT = "com.android.launcher";
    public static PlayGamesSessionInfo? BuildFromEmulatorState(string info)
    {
        var packageNameMatch = EmulatorStateRegexes.DisplayedTaskPackageName().Matches(info);
        if (packageNameMatch.Count == 0)
            return null;

        // We prefer matches that are not System Level Packages
        var taskPackageName = GetPrioritizedValue(packageNameMatch, x => !IsSystemLevelPackage(GetValueFromMatch(x)));

        // We skip system level applications such as com.android.settings (The settings application for the emulator)
        if (IsSystemLevelPackage(taskPackageName))
            return null;

        if (!TryParseRegex(EmulatorStateRegexes.ForegroundPackageName(), info, out var foregroundPackageName))
            return null;

        var packageName = IsSystemLevelPackage(foregroundPackageName)
            ? taskPackageName
            : foregroundPackageName;


        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        if (!TryParseRegex(EmulatorStateRegexes.StartedTimestampRegex(), info, out var startedTimestampAsString))
            return null;


        if (!TryParseRegex(EmulatorStateRegexes.AppSessionStateRegex(), info, out var stateAsString))
            return null;

        var appState = AppSessionState.None;

        // I don't remember why we checked for this, this may very well be redundant as taskPackageName is guaranteed not to start with the system level thing.
        // :shrug:
        if (foregroundPackageName.StartsWith(ANDROID_LAUNCHER_HINT) || taskPackageName.StartsWith(ANDROID_LAUNCHER_HINT))
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

        // Since we can't accurately get the window Title anymore on the dev version
        // We settle for the package name title
        // This will be further cleaned up by the web scraper if successful
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

        [GeneratedRegex("foreground_task=(?'ForegroundPackageName'.+?) }", RegexOptions.Multiline)]
        internal static partial Regex ForegroundPackageName(); // foreground_task=com.YoStarEN.Arknights }

        [GeneratedRegex("task=(?'TaskPackageName'.+?), ", RegexOptions.Multiline)]
        internal static partial Regex DisplayedTaskPackageName(); //       { display_id=0, task=com.YoStarEN.Arknights, foreground=True },

        [GeneratedRegex(@"^(?'Timestamp'.+?\+\d) ", RegexOptions.Multiline)]
        internal static partial Regex StartedTimestampRegex(); // 250207 18:00:04.030+1 39 INFO  EmulatorStateLogger: Emulator state updated:

        // status=Running
        // Stopping (Emulator stopped normally (shutdown)) - lastKnownHealthStatus=No ERROR; emulator is healthy

        [GeneratedRegex("status=(?'AppSessionState'.+?)(?= |$)", RegexOptions.Multiline)]
        internal static partial Regex AppSessionStateRegex();
    }

}
