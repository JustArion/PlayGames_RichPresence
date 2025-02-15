using System.Collections.Concurrent;
using Polly;
using Polly.Retry;

namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Text.RegularExpressions;

public static partial class PlayGamesWebScraper
{
    public record PlayGamesWebInfo(string IconLink, string Title);

    private static readonly AsyncRetryPolicy<PlayGamesWebInfo?> _retryPolicy = Policy<PlayGamesWebInfo?>
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) - 1));

    private static readonly ConcurrentDictionary<string, PlayGamesWebInfo> _scraperCache = new();
    public static async ValueTask<PlayGamesWebInfo?> TryGetPackageInfo(string packageName)
    {
        if (_scraperCache.TryGetValue(packageName, out var link))
            return link;

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                using var client = new HttpClient();

                var storePageContent = await client.GetStringAsync($"https://play.google.com/store/apps/details?id={packageName}");

                var match = GetImageRegex().Match(storePageContent);

                if (!match.Success)
                {
                    Log.Warning("Failed to find icon link for {PackageName}", packageName);
                    return null;
                }

                var imageLink = match.Groups[1].Value;
                var titleMatch = GetTitleRegex().Match(storePageContent);
                var title = titleMatch.Success ? titleMatch.Groups[1].Value : string.Empty;

                var info = new PlayGamesWebInfo(imageLink, title);
                _scraperCache.TryAdd(packageName, info);

                return info;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get icon link for {PackageName}", packageName);
            return null;
        }
    }

    [GeneratedRegex("<meta property=\"og:image\" content=\"(.+?)\">")]
    private static partial Regex GetImageRegex();

    [GeneratedRegex("<meta property=\"og:title\" content=\"(.+?) - Apps on Google Play\">")]
    private static partial Regex GetTitleRegex();
}
