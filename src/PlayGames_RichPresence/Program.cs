using System.Diagnostics;

namespace Dawn.PlayGames.RichPresence;

using Logging;
using Models;
using PlayGames;
using Tools;
using DiscordRichPresence;
using DiscordRPC;
using Tray;

internal static class Program
{
    internal static LaunchArgs Arguments { get; private set; }

    private static RichPresence_Tray _trayIcon = null!;
    private static RichPresenceHandler _richPresenceHandler = null!;
    private static ProcessBinding? _processBinding;
    private const string FILE_PATH = @"Google\Play Games\Logs\Service.log";
    private static readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FILE_PATH);
    private const string DEV_FILE_PATH = @"Google\Play Games Developer Emulator\Logs\Service.log";
    private static readonly string _devFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DEV_FILE_PATH);

    [STAThread]
    private static async Task Main(string[] args)
    {
        Arguments = new(args);

        ApplicationLogs.Initialize(false);

        if (!Arguments.NoAutoUpdate)
        {
            var supportsVelopack = await AutoUpdate.Velopack();
            if (supportsVelopack)
                ApplicationLogs.Initialize(true);
        }
        ApplicationLogs.ListenToEvents();

        SingleInstanceApplication.Ensure();

        _richPresenceHandler = new();
        var reader = new PlayGamesAppSessionMessageReader(_filePath);
        var devReader = new PlayGamesAppSessionMessageReader(_devFilePath);

        _trayIcon = new(_filePath);
        _trayIcon.RichPresenceEnabledChanged += OnRichPresenceEnabledChanged;

        reader.OnSessionInfoReceived += SessionInfoReceived;
        reader.StartAsync();

        devReader.OnSessionInfoReceived += SessionInfoReceived;
        devReader.StartAsync();

        if (Arguments.HasProcessBinding)
            _processBinding = new ProcessBinding(Arguments.ProcessBinding);

        Application.Run();
        _richPresenceHandler.Dispose();
        _processBinding?.Dispose();
    }

    private static void OnRichPresenceEnabledChanged(object? sender, bool active)
    {
        if (active)
        {
            if (_currentPresence != null)
                _richPresenceHandler.SetPresence(_currentPresence);
            return;
        }

        _richPresenceHandler.RemovePresence();
    }

    private static CancellationTokenSource _cts = new();

    private static void SessionInfoReceived(object? sender, PlayGamesSessionInfo sessionInfo) => Task.Run(() => SetPresenceFromSessionInfoAsync(sessionInfo));

    private static void SubscribeToAppExit(string processName, EventHandler onExit, CancellationToken ctsToken)
    {
        var process = Process.GetProcessesByName(processName).OrderBy(x => x.StartTime).FirstOrDefault();
        if (process is null)
        {
            Log.Warning("Process {ProcessName} not found", processName);
            return;
        }

        try
        {
            process.EnableRaisingEvents = true;
            process.Exited += onExit;
            ctsToken.Register(() => process.Exited -= onExit);

            Log.Information("Subscribed to app exit for {ProcessName}", $"{processName}.exe");
        }
        catch (AccessViolationException e)
        {
            Log.Error(e, "Failed to subscribe to app exit");
        }

    }

    private static AppSessionState _currentAppState;
    private static async ValueTask SetPresenceFromSessionInfoAsync(PlayGamesSessionInfo sessionInfo)
    {
        if (_currentAppState == sessionInfo.AppState)
            return;

        if (Process.GetProcessesByName("crosvm").Length == 0)
        {
            Log.Debug("Emulator is not running, likely a log-artifact, crosvm.exe ({SessionTitle})", sessionInfo.Title);
            return;
        }

        // This is a bit of a loaded if statement. Let me break it down a bit
        // If the state went from Starting -> Started we don't do anything
        // If the state went from anything -> Starting / Started we subscribe to the app exit
        // This should prevent a double subscribe if weird app orders start appearing (Running -> Starting)
        if (_currentAppState is not (AppSessionState.Starting or AppSessionState.Running) && sessionInfo.AppState is AppSessionState.Starting or AppSessionState.Running)
        {
            _cts = new ();
            SubscribeToAppExit("crosvm", (_, _) =>
            {
                Log.Information("crosvm.exe has exited");
                var previousAppState = _currentAppState;
                _currentAppState = AppSessionState.Stopped;
                Log.Information("App State Changed from {PreviousAppState} -> {CurrentAppState}", previousAppState, _currentAppState);
                ClearPresenceFor(sessionInfo);
            }, _cts.Token);
        }



        Log.Information("App State Changed from {PreviousAppState} -> {CurrentAppState} | {Timestamp}", _currentAppState, sessionInfo.AppState, sessionInfo.StartTime);
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
            case AppSessionState.Stopping or AppSessionState.Stopped:
                await _cts.CancelAsync();
                ClearPresenceFor(sessionInfo);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void ClearPresenceFor(PlayGamesSessionInfo sessionInfo)
    {
        Log.Information("Clearing Rich Presence for {GameTitle}", sessionInfo.Title);

        _richPresenceHandler.RemovePresence();
    }

    private static RichPresence? _currentPresence;
    private static async Task SetPresenceFor(PlayGamesSessionInfo sessionInfo, RichPresence presence)
    {
        var scrapedInfo = await PlayGamesWebScraper.TryGetPackageInfo(sessionInfo.PackageName);
        var iconUrl = scrapedInfo?.IconLink;
        if (scrapedInfo != null && !string.IsNullOrWhiteSpace(scrapedInfo.Title))
        {
            Log.Information("Using remedied App Title: {PreviousTitle} -> {CurrentTitle}", sessionInfo.Title, scrapedInfo.Title);
            sessionInfo.Title = scrapedInfo.Title;
        }


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
        presence.Assets.LargeImageText = presence.Details;

        _richPresenceHandler.SetPresence(presence);
        _currentPresence = presence;
    }
}
