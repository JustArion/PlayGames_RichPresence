namespace Dawn.PlayGames.RichPresence.Logs.PlayGames;

using System.Text.RegularExpressions;
using global::Serilog;

public static partial class PlayGamesAppIconScraper
{
    private static readonly Dictionary<string, string> _iconLinks = new();
    public static async ValueTask<string> GetIconLink(string packageName)
    {
        if (_iconLinks.TryGetValue(packageName, out var link))
            return link;

        try
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