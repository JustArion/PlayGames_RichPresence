namespace PlayGames_RichPresence.Tests.Unit;

using Dawn.PlayGames.RichPresence.Models;
using Dawn.PlayGames.RichPresence.PlayGames;

[TestFixture(TestOf = typeof(PlayGamesAppSessionMessageReader))]
public class AppSessionMessageReaderTests
{
    private static readonly PlayGamesAppSessionMessageReader[] _readers = 
    [
        new("Assets/Service.log"),
        new("Assets/Additional/Service.1.log"),
        new("Assets/Additional/Service.2.log"),
        new("Assets/Service - Developer.log"),
        new("Assets/Additional/Service.dev.1.log")
    ];

    [TearDown]
    public void Cleanup()
    {
        foreach (var reader in _readers)
            reader.Dispose();
    }

    [Test]
    [TestCaseSource(nameof(_readers))]
    public async Task ShouldGet_FileLock(PlayGamesAppSessionMessageReader sut)
    {
        await using var fileLock = sut.AquireFileLock();
    }
    
    [Test]
    [TestCaseSource(nameof(_readers))]
    public async Task Packages_ShouldNot_Contain_SystemLevelPackages(PlayGamesAppSessionMessageReader sut)
    {
        // Arrange
        await using var fileLock = sut.AquireFileLock();

        // Act
        var sessionInfos = await sut.GetAllSessionInfos(fileLock);

        // Assert
        sessionInfos.Should()
            .AllSatisfy(x => 
                AppLifetimeParser.IsSystemLevelPackage(x.PackageName).Should().BeFalse());
    }
}