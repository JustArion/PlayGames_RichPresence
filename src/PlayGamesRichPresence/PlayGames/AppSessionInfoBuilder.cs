namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Globalization;
using System.Text.RegularExpressions;
using Domain;
using global::Serilog;

internal static partial class AppSessionInfoBuilder
{
    private const string STARTED_TIMESTAMP_FORMAT = "M/d/yyyy h:mm:ss tt zzz";
    public static PlayGamesSessionInfo? Build(string info)
    {
        var packageNameMatch = PackageNameRegex().Match(info);
        if (!packageNameMatch.Success)
            return null;

        var titleMatch = TitleRegex().Match(info);
        if (!titleMatch.Success)
            return null;

        var startedTimestampMatch = StartedTimestampRegex().Match(info);
        if (!startedTimestampMatch.Success)
            return null;

        var stateMatch = AppSessionStateRegex().Match(info);
        if (!stateMatch.Success)
            return null;

        var startedTimestampAsString = startedTimestampMatch.Groups[1].Value.Replace("\r", string.Empty);

        if (!DateTimeOffset.TryParseExact(startedTimestampAsString, STARTED_TIMESTAMP_FORMAT, null, DateTimeStyles.None , out var startedTimestamp))
        {
            Log.Warning("Failed to parse started timestamp: {StartedTimestampString}", startedTimestampAsString);
            return null;
        }

        var stateAsString = stateMatch.Groups[1].Value.Replace("\r", string.Empty);

        if (!Enum.TryParse<AppSessionState>(stateAsString, out var appState))
        {
            Log.Warning("Failed to parse app state: {AppStateString}", stateAsString);
            return null;
        }

        var packageName = packageNameMatch.Groups[1].Value.Replace("\r", string.Empty);
        var title = titleMatch.Groups[1].Value.Replace("\r", string.Empty);

        return new PlayGamesSessionInfo(packageName, startedTimestamp, title, appState);
    }

    [GeneratedRegex("package_name=(.+?)$", RegexOptions.Multiline)]
    private static partial Regex PackageNameRegex(); // package_name=com.YoStarEN.Arknights

    [GeneratedRegex("title=(.+?)$", RegexOptions.Multiline)]
    private static partial Regex TitleRegex(); // title=Arknights

    [GeneratedRegex("started_timestamp=(.+?)$", RegexOptions.Multiline)]
    private static partial Regex StartedTimestampRegex(); // started_timestamp=9/24/2024 1:05:02 PM +00:00

    [GeneratedRegex("state=(.+?)=", RegexOptions.Multiline)]
    private static partial Regex AppSessionStateRegex(); // state=Running={ }
}
