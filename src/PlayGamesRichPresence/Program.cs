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
    internal static LaunchArgs Arguments { get; private set; }

    private static RichPresence_Tray _trayIcon = null!;
    private static RichPresenceHandler _richPresenceHandler = null!;

    [STAThread]
    private static void Main(string[] args)
    {
        Arguments = new(args);

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
        Log.Information("App State Changed from {PreviousAppState} -> {CurrentAppState}", _currentAppState, sessionInfo.AppState);
        _currentAppState = sessionInfo.AppState;

        switch (sessionInfo.AppState)
        {
            case AppSessionState.Starting:
                await SetPresenceFor(sessionInfo, new()
                {
                    Assets = new()
                    {
                        LargeImageText = "Starting up..."
                    }
                });
                break;
            case AppSessionState.Running:
                await SetPresenceFor(sessionInfo, new()
                {
                    Timestamps = new Timestamps(sessionInfo.StartTime.DateTime)
                });
                break;
            case AppSessionState.Stopping:
                await SetPresenceFor(sessionInfo, new()
                {
                    Timestamps = new Timestamps(sessionInfo.StartTime.DateTime),
                    Assets = new()
                    {
                        LargeImageText = "Finishing up..."
                    }
                });
                break;
            case AppSessionState.Stopped:
                if (_currentAppState != sessionInfo.AppState)
                    Log.Information("Clearing Rich Presence for {GameTitle}", sessionInfo.Title);

                _richPresenceHandler.RemovePresence();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static async Task SetPresenceFor(PlayGamesSessionInfo sessionInfo, RichPresence presence)
    {
        var iconUrl = await PlayGamesAppIconScraper.TryGetIconLinkAsync(sessionInfo.PackageName);

        presence.Details ??= sessionInfo.Title;

        if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            if (presence.HasAssets())
                presence.Assets!.LargeImageKey = iconUrl;
            else
                presence.Assets = new()
                {
                    LargeImageKey = iconUrl
                };
        }

        _richPresenceHandler.SetPresence(presence);
    }
}
