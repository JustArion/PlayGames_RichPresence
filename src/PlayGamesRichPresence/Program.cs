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

        _richPresenceHandler = new();
        var reader = new PlayGamesAppSessionMessageReader();

        _trayIcon = new();
        _trayIcon.RichPresenceEnabledChanged += OnRichPresenceEnabledChanged;

        reader.StartAsync();
        reader.OnAppSessionMessageReceived += OnAppMessageReceived;

        Application.Run();
    }

    private static void OnRichPresenceEnabledChanged(object? sender, bool active)
    {
        if (!active)
            _richPresenceHandler.SetPresence(null);
    }

    private static void OnAppMessageReceived(string message)
    {
        var sessionInfo = AppSessionInfoBuilder.Build(message);

        if (sessionInfo == null)
            return;

        Task.Run(()=> SetPresenceFromSessionInfoAsync(sessionInfo));
    }

    private static AppSessionState _currentState;
    private static async Task SetPresenceFromSessionInfoAsync(PlayGamesSessionInfo sessionInfo)
    {

        switch (sessionInfo.AppState)
        {
            case AppSessionState.Running:
                var iconUrl = await PlayGamesAppIconScraper.GetIconLink(sessionInfo.PackageName).AsTask();

                if (_currentState != sessionInfo.AppState)
                    Log.Information("Setting Rich Presence for {GameTitle}", sessionInfo.Title);
                if (string.IsNullOrWhiteSpace(iconUrl))
                {
                    _richPresenceHandler.SetPresence(new()
                    {
                        Details = sessionInfo.Title,
                        Timestamps = new Timestamps(sessionInfo.StartTime.DateTime),
                        Buttons = [ new () { Label = "Open Play Store", Url = $"https://play.google.com/store/apps/details?id={sessionInfo.PackageName}" }]
                    });
                }
                else
                {
                    _richPresenceHandler.SetPresence(new()
                    {
                        Details = sessionInfo.Title,
                        Timestamps = new Timestamps(sessionInfo.StartTime.DateTime),
                        Assets = new()
                        {
                            LargeImageKey = iconUrl,
                            LargeImageText = sessionInfo.PackageName
                        },
                        Buttons = [ new () { Label = "Open Play Store", Url = $"https://play.google.com/store/apps/details?id={sessionInfo.PackageName}" }]
                    });
                }
                break;
            case AppSessionState.Stopped or AppSessionState.Stopping:
                if (_currentState != sessionInfo.AppState)
                    Log.Information("Clearing Rich Presence for {GameTitle}", sessionInfo.Title);
                _richPresenceHandler.ClearPresence();
                break;
        }
        _currentState = sessionInfo.AppState;
    }
}
