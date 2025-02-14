﻿using System.Collections.Concurrent;
using Polly;
using Polly.Retry;

namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Text.RegularExpressions;

public static partial class PlayGamesAppIconScraper
{
    private static readonly AsyncRetryPolicy<string> _retryPolicy = Policy<string>
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) - 1));

    private static readonly ConcurrentDictionary<string, string> _iconLinks = new();
    public static async ValueTask<string> TryGetIconLinkAsync(string packageName)
    {
        if (_iconLinks.TryGetValue(packageName, out var link))
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
                    return string.Empty;
                }

                var imageLink = match.Groups[1].Value;
                _iconLinks.TryAdd(packageName, imageLink);
                return imageLink;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get icon link for {PackageName}", packageName);
            return string.Empty;
        }
    }

    [GeneratedRegex("<meta property=\"og:image\" content=\"(.+?)\">")]
    private static partial Regex GetImageRegex();
}
