namespace Dawn.PlayGames.RichPresence.Models;

public record PlayGamesSessionInfo(string PackageName, DateTimeOffset StartTime, string Title, AppSessionState AppState)
{
    public string Title { get; set; } = Title;
    public string RawText { get; init; } = "";
}
