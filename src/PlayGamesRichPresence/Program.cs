using Dawn.PlayGames.RichPresence.Logging;
using Dawn.PlayGames.RichPresence.PlayGames;

namespace Dawn.PlayGames.RichPresence;

using System.Diagnostics.CodeAnalysis;
using DiscordRichPresence;
using DiscordRPC;
using Domain;
using global::Serilog;
using Tray;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
internal static class Program
{
    private static RichPresence_Tray _trayIcon = null!;
    private static RichPresenceHandler _richPresenceHandler = null!;

    [STAThread]
    private static void Main()
    {
        ApplicationLogs.Initialize();

        SingleInstanceApplication.Ensure();

        _richPresenceHandler = new();
        var reader = new PlayGamesAppSessionMessageReader();

        _trayIcon = new();
        _trayIcon.RichPresenceEnabledChanged += OnRichPresenceEnabledChanged;

        reader.StartAsync();
        reader.OnSessionInfoReceived += SessionInfoReceived;

        Application.Run();
    }

    private static void OnRichPresenceEnabledChanged(object? sender, bool active)
    {
        if (active)
            return;

        _richPresenceHandler.RemovePresence();
    }

    private static void SessionInfoReceived(object? sender, PlayGamesSessionInfo sessionInfo) => Task.Run(()=> SetPresenceFromSessionInfoAsync(sessionInfo));

    private static AppSessionState _currentAppState;
    private static async ValueTask SetPresenceFromSessionInfoAsync(PlayGamesSessionInfo sessionInfo)
    {

        if (_currentAppState == sessionInfo.AppState)
            return;

        switch (sessionInfo.AppState)
        {
            case AppSessionState.Running:
                await SetPresenceAsRunning(sessionInfo);
                break;
            case AppSessionState.Starting:
                await SetPresenceAsStarting(sessionInfo);
                break;
            case AppSessionState.Stopped or AppSessionState.Stopping:
                ClearPresence(sessionInfo);
                break;
        }


        _currentAppState = sessionInfo.AppState;
    }

    private static async Task SetPresenceAsStarting(PlayGamesSessionInfo sessionInfo)
    {
        var iconUrl = await PlayGamesAppIconScraper.TryGetIconLinkAsync(sessionInfo.PackageName);

        if (string.IsNullOrWhiteSpace(iconUrl))
            _richPresenceHandler.SetPresence(new()
            {
                Details = sessionInfo.Title,
            });
        else
            _richPresenceHandler.SetPresence(new()
            {
                Details = sessionInfo.Title,
                Assets = new()
                {
                    LargeImageKey = iconUrl,
                    LargeImageText = sessionInfo.PackageName
                },
            });
    }

    private static void ClearPresence(PlayGamesSessionInfo sessionInfo)
    {
        if (_currentAppState != sessionInfo.AppState)
            Log.Information("Clearing Rich Presence for {GameTitle}", sessionInfo.Title);

        _richPresenceHandler.RemovePresence();
    }

    private static async Task SetPresenceAsRunning(PlayGamesSessionInfo sessionInfo)
    {
        var iconUrl = await PlayGamesAppIconScraper.TryGetIconLinkAsync(sessionInfo.PackageName);

        if (string.IsNullOrWhiteSpace(iconUrl))
            _richPresenceHandler.SetPresence(new()
            {
                Details = sessionInfo.Title,
                Timestamps = new Timestamps(sessionInfo.StartTime.DateTime),
            });
        else
            _richPresenceHandler.SetPresence(new()
            {
                Details = sessionInfo.Title,
                Timestamps = new Timestamps(sessionInfo.StartTime.DateTime),
                Assets = new()
                {
                    LargeImageKey = iconUrl,
                    LargeImageText = sessionInfo.PackageName
                },
            });
    }
}
