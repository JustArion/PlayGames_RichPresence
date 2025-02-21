namespace PlayGames_RichPresence.Tests.Integration;

using Dawn.PlayGames.RichPresence.PlayGames;

[TestFixture(TestOf = typeof(PlayGamesWebScraper))]
public class WebScraperTests
{
    private static readonly string[] _appPackages = ["com.YoStarEN.Arknights", "com.krafton.defensederby"];
    
    [TestCaseSource(nameof(_appPackages))]
    public async Task ShouldScrape_Links(string package)
    {
        // Act
        var packageInfo = await PlayGamesWebScraper.TryGetPackageInfo(package);
        // Assert

        packageInfo.Should().NotBeNull();
            
        Uri.TryCreate(packageInfo!.IconLink, UriKind.Absolute, out var uri).Should().BeTrue();
        uri!.Scheme.Should().Be("https");
    }
    
    [TestCaseSource(nameof(_appPackages))]
    public async Task ShouldScrape_Titles(string package)
    {
        // Act
        var packageInfo = await PlayGamesWebScraper.TryGetPackageInfo(package);

        // Assert
        packageInfo.Should().NotBeNull();

        packageInfo!.Title.Should().NotBeNullOrEmpty();

        packageInfo.Title.Should().NotContain("Google");
    }
}