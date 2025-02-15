namespace PlayGames_RichPresence.Tests.Unit;

using Dawn.PlayGames.RichPresence.Models;
using Dawn.PlayGames.RichPresence.PlayGames;

[TestFixture(TestOf = typeof(PlayGamesAppSessionMessageReader))]
public class DevLogSpecificMessageReaderTests
{
    [SetUp]
    public void SetUp()
    {
        _sut = new PlayGamesAppSessionMessageReader("Assets/Service - Developer.log");
    }
    private PlayGamesAppSessionMessageReader _sut;


    [TearDown]
    public void Cleanup()
    {
        _sut.Dispose();
    }
    
    [Test]
    public async Task Sessions_ShouldBe_ExpectedAmount()
    {
        // Arrange
        await using var fileLock = _sut.AquireFileLock();
        
        // Act
        var sessionInfos = await _sut.GetAllSessionInfos(fileLock);

        // Assert
        sessionInfos.Count.Should().Be(80);
    }
    
    [Test]
    public async Task LastSession_ShouldBe_StoppedArknights()
    {
        // Arrange
        await using var fileLock = _sut.AquireFileLock();
        
        // Act
        var sessionInfos = await _sut.GetAllSessionInfos(fileLock);

        // Assert
        sessionInfos.Should().HaveCountGreaterThanOrEqualTo(1);
        
        var last = sessionInfos[^1];

        last.Title.Should().Be("Arknights");
        last.PackageName.Should().Be("com.YoStarEN.Arknights");
        last.AppState.Should().Be(AppSessionState.Stopped);
    }
}