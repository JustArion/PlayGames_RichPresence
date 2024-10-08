namespace Dawn.PlayGames.RichPresence.Domain;

public record PlayGamesSessionInfo(string PackageName, DateTimeOffset StartTime, string Title, AppSessionState AppState);