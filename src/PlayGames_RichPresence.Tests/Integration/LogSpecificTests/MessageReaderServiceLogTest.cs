﻿namespace PlayGames_RichPresence.Tests.Integration;

using Dawn.PlayGames.RichPresence.Models;
using Dawn.PlayGames.RichPresence.PlayGames;

[TestFixture(TestOf = typeof(PlayGamesAppSessionMessageReader))]
public class LogSpecificMessageReaderTests
{
    [SetUp]
    public void SetUp()
    {
        _sut = new PlayGamesAppSessionMessageReader("Assets/Service.log");
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
        sessionInfos.Count.Should().Be(321);
    }

    [Test]
    public async Task FirstSession_ShouldBe_DefenseDerby()
    {
        // Arrange
        await using var fileLock = _sut.AquireFileLock();
        
        // Act
        var sessionInfos = await _sut.GetAllSessionInfos(fileLock);

        // Assert
        sessionInfos.Should().HaveCountGreaterThanOrEqualTo(1);

        var first = sessionInfos[0];

        first.Title.Should().Be("Defense Derby");
        first.PackageName.Should().Be("com.krafton.defensederby");
        first.AppState.Should().Be(AppSessionState.Starting);
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