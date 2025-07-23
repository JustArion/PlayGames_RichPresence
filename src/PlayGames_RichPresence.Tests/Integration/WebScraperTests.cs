﻿namespace PlayGames_RichPresence.Tests.Integration;

using System.Collections.Frozen;
using Dawn.PlayGames.RichPresence.PlayGames;

[TestFixture(TestOf = typeof(PlayStoreWebScraper))]
public class WebScraperTests
{
    private static readonly FrozenDictionary<string, string> _appPackagesToTitles = new Dictionary<string, string>
    {
        ["com.YoStarEN.Arknights"] = "Arknights",
        ["com.nexon.bluearchive"] = "Blue Archive"
    }.ToFrozenDictionary();
    
    [TestCaseSource(nameof(_appPackagesToTitles))]
    public async Task ShouldScrape_Titles(KeyValuePair<string, string> package)
    {
        // Act
        var packageInfo = await PlayStoreWebScraper.TryGetPackageInfo(package.Key);

        // Assert
        packageInfo.Should().NotBeNull();

        packageInfo!.Title.Should().Be(package.Value);
    }
}