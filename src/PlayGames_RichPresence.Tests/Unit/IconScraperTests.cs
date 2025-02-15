namespace PlayGames_RichPresence.Tests.Unit;

using System.Web;
using Dawn.PlayGames.RichPresence.PlayGames;
using FluentAssertions;

[TestFixture(TestOf = typeof(PlayGamesWebScraper))]
public class IconScraperTests
{
    private static readonly string[] _appPackages = ["com.YoStarEN.Arknights", "com.krafton.defensederby"];
    [Test]
    public async Task ShouldScrape_Links()
    {
        // Act
        foreach (var appPackage in _appPackages)
        {
            var packageInfo = await PlayGamesWebScraper.TryGetPackageInfo(appPackage);
            // Assert

            packageInfo.Should().NotBeNull();
            
            Uri.TryCreate(packageInfo!.IconLink, UriKind.Absolute, out var uri).Should().BeTrue();
            uri!.Scheme.Should().Be("https");
        }

    }
}