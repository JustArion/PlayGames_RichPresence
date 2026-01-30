using System.Diagnostics;
using System.Reactive.Subjects;
using Dawn.PlayGames.RichPresence.Discord;
using NuGet.Versioning;
using Velopack;

namespace Dawn.PlayGames.RichPresence;

using Logging;
using Models;
using PlayGames;
using Tools;
using DiscordRPC;
using Tray;

internal static class Program
{
    internal static LaunchArgs Arguments { get; private set; }

    private static RichPresence_Tray _trayIcon = null!;
    private static RichPresenceHandler _richPresenceHandler = null!;
    private static DiscoverabilityHandler _discoverabilityHandler = null!;
    private static ProcessBinding? _processBinding;
    private const string FILE_PATH = @"Google\Play Games\Logs\Service.log";
    private static readonly FileInfo _filePath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FILE_PATH));
    private const string DEV_FILE_PATH = @"Google\Play Games Developer Emulator\Logs\Service.log";
    private static readonly FileInfo _devFilePath = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DEV_FILE_PATH));

    [STAThread]
    private static void Main(string[] args)
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory; // Startup sets it to %windir%

        // This might throw an access violation if we don't have permissions to read it, we just don't read further when that happens
        SuppressExceptions(()=> DotNetEnv.Env
            .TraversePath()
            .Load());

        Arguments = new(args)
        {
            #if DEBUG
            ExtendedLogging = true
            #endif
        };
        InitializeVelopack();

        ApplicationLogs.Initialize();

        SingleInstanceApplication.Ensure();

        ApplicationLogs.ListenToEvents();

        if (!Arguments.NoAutoUpdate)
            Task.Run(AutoUpdate.CheckForUpdates);


        _richPresenceHandler = new();
        var reader = new PlayGamesAppSessionMessageReader(_filePath);
        var devReader = new PlayGamesAppSessionMessageReader(_devFilePath);

        _trayIcon = new(new(_filePath));
        _trayIcon.RichPresenceEnabledChanged += OnRichPresenceEnabledChanged;
        _discoverabilityHandler = new();

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

    private static void InitializeVelopack()
    {
        var app = VelopackApp.Build();
        app.OnBeforeUninstallFastCallback(OnUninstall);
        app.Run();
    }

    private static void OnUninstall(SemanticVersion version) => Startup.RemoveStartup(Application.ProductName!);

    private static void SuppressExceptions(Action act)
    {
        try
        {
            act();
        }
        catch
        {
            // ignored
        }
    }

    private static void OnRichPresenceEnabledChanged(object? sender, bool active)
    {
        if (active)
        {
            if (_currentPresence is not { } presence)
                return;
            if (_currentSessionInfo is not { } sessionInfo)
            {
                Log.Error("Trying to set a rich presence without an associated lifetime! Known details are: AppId: {AppId}, Details: {Details}", _currentApplicationId, presence.Details);
                return;
            }

            _richPresenceHandler.TrySetPresence(sessionInfo.Title, presence, _currentApplicationId);
            return;
        }

        _richPresenceHandler.ClearPresence(_currentSessionInfo?.Title);
        _currentSessionInfo = null;
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

    private static PlayGamesSessionInfo? _currentSessionInfo;
    private static string? _currentApplicationId;
    private static AppSessionState _currentAppState;
    private static async ValueTask SetPresenceFromSessionInfoAsync(PlayGamesSessionInfo sessionInfo)
    {
        var currentState = _currentAppState;
        // Why were we comparing this here and not using the local variable?
        if (_currentAppState == sessionInfo.AppState)
            return;

        if (Process.GetProcessesByName("crosvm").Length == 0)
        {
            Log.Debug("Emulator is not running, likely a log-artifact, crosvm.exe ({SessionTitle})", sessionInfo.Title);
            return;
        }

        // There's a missing state here, it should be Starting -> Running -> Stopping -> Stopped
        if (_currentAppState == AppSessionState.Stopping && sessionInfo.AppState == AppSessionState.Running)
            return;

        _currentAppState = sessionInfo.AppState;
        _currentSessionInfo = sessionInfo;
        Log.Information("App State Changed from {PreviousAppState} -> {CurrentAppState} | {Timestamp}", currentState, sessionInfo.AppState, sessionInfo.StartTime);


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

        switch (sessionInfo.AppState)
        {
            case AppSessionState.Starting:
                Log.Debug("Setting Rich Presence as {Title}(Starting Up)", sessionInfo.Title);
                await SetPresenceFor(sessionInfo, new()
                {
                    Assets = new()
                    {
                        LargeImageText = "Starting up..."
                    }
                });
                break;
            case AppSessionState.Running:
                Log.Debug("Setting Rich Presence as {Title}(Running)", sessionInfo.Title);
                await SetPresenceFor(sessionInfo, new()
                {
                    Timestamps = new Timestamps(sessionInfo.StartTime.DateTime)
                });
                break;
            case AppSessionState.Stopping or AppSessionState.Stopped:
                if (!HasRichPresence())
                    return;

                await _cts.CancelAsync();
                ClearPresenceFor(sessionInfo);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sessionInfo));
        }
    }

    private static bool HasRichPresence() => _currentPresence != null;

    private static void ClearPresenceFor(PlayGamesSessionInfo sessionInfo)
    {
        if (Interlocked.Exchange(ref _currentPresence, null) == null)
            return;

        Log.Information("Clearing Rich Presence for {GameTitle}", sessionInfo.Title);
        _richPresenceHandler.ClearPresence(_currentSessionInfo?.Title);
        _currentSessionInfo = null;
    }

    // Tray Enabled / Disabled Restorer
    private static RichPresence? _currentPresence;
    private static async Task SetPresenceFor(PlayGamesSessionInfo sessionInfo, RichPresence presence)
    {
        _currentPresence = presence;
        var scrapedInfo = await PlayStoreWebScraper.TryGetPackageInfo(sessionInfo.PackageName);
        var iconUrl = scrapedInfo?.IconLink ?? string.Empty;
        if (scrapedInfo != null && !string.IsNullOrWhiteSpace(scrapedInfo.Title) && sessionInfo.Title != scrapedInfo.Title)
        {
            Log.Information("Using remedied App Title: {PreviousTitle} -> {CurrentTitle}", sessionInfo.Title, scrapedInfo.Title);
            sessionInfo.Title = scrapedInfo.Title;
        }

        var officialApplicationId = await _discoverabilityHandler.TryGetOfficialApplicationId(sessionInfo.Title);
        if (officialApplicationId == null)
        {
            presence.Details ??= sessionInfo.Title;
            presence.WithStatusDisplay(StatusDisplayType.Details);
        }

        if (!string.IsNullOrWhiteSpace(iconUrl))
            PopulatePresenceAssets(sessionInfo, presence, iconUrl, officialApplicationId == null);

        _currentApplicationId = officialApplicationId;
        _richPresenceHandler.TrySetPresence(sessionInfo.Title, presence, officialApplicationId);
    }

    private static void PopulatePresenceAssets(PlayGamesSessionInfo sessionInfo, RichPresence presence, string iconLink, bool linkToStorePage)
    {
        if (!presence.HasAssets())
            presence.Assets = new();

        var assets = presence.Assets;
        assets.LargeImageKey = iconLink;
        assets.LargeImageText = presence.Details;

        if (linkToStorePage)
            assets.LargeImageUrl = PlayStoreWebScraper.GetPlayStoreLinkForPackage(sessionInfo.PackageName);
    }
}
