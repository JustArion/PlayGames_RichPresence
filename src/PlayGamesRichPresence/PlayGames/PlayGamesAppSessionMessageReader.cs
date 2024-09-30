#define LOG_APP_SESSION_MESSAGES
using Dawn.PlayGames.RichPresence.Domain;
using Dawn.Serilog.CustomEnrichers;

namespace Dawn.PlayGames.RichPresence.PlayGames;

using System.Text;
using global::Serilog;
using FileAccess = System.IO.FileAccess;

public class PlayGamesAppSessionMessageReader : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<PlayGamesAppSessionMessageReader>();
    private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FILE_PATH);
    private const string FILE_PATH = @"Google\Play Games\Logs\Service.log";

    private bool _started;
    private long _lastStreamPosition;
    private readonly FileSystemWatcher _logFileWatcher = new();
    public event EventHandler<PlayGamesSessionInfo>? OnSessionInfoReceived;

    public void StartAsync()
    {
        if (_started)
            return;
        _started = true;

        Task.Factory.StartNew(InitiateWatchOperation, TaskCreationOptions.LongRunning);
    }

    private async Task InitiateWatchOperation()
    {
        Log.Verbose("Doing fresh read-operation pass");

        // Wait till the file exists
        if (!File.Exists(_filePath))
        {
            _logger.Debug("File not found: {FilePath}", FILE_PATH);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        _logFileWatcher.Path = Path.GetDirectoryName(_filePath)!;
        _logFileWatcher.Filter = Path.GetFileName(_filePath);
        _logFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _logFileWatcher.Changed += LogFileWatcherOnFileChanged;
        _logFileWatcher.Error += LogFileWatcherOnError;

        await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);

        // We read the old entries (To check if there's a game currently running)
        if (_lastStreamPosition == default)
        {
            using (Warn.OnLongerThan(TimeSpan.FromSeconds(2), "Catch-Up took unusually long"))
                await CatchUpAsync(reader);

            _lastStreamPosition = fs.Position;
        }

        _logFileWatcher.EnableRaisingEvents = true;
    }

    private void LogFileWatcherOnError(object sender, ErrorEventArgs e) => _logger.Error(e.GetException(), "File watcher error");

    private bool _reading;
    private void LogFileWatcherOnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (_reading)
            return;
        _reading = true;

        Task.Run(ReadFileChanges).ContinueWith(_ => _reading = false);
    }

    private async Task ReadFileChanges()
    {
        try
        {
            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            // We read new things being added from here onwards
            fs.Seek(_lastStreamPosition, SeekOrigin.Begin);
            reader.DiscardBufferedData();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                await ProcessLogChunkAsync(line, reader);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to read the change in file {FilePath}", _filePath);
        }
    }

    /// <summary>
    /// The method ensures that a Rich Presence will be enabled if a game is running before this program started.
    /// </summary>
    /// <param name="reader"></param>
    private async Task CatchUpAsync(StreamReader reader)
    {
        Log.Verbose("Catching up...");
        var events = 0;
        PlayGamesSessionInfo? sessionInfo = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.Contains("AppSessionModule: sessions updated:"))
                continue;

            var sb = new StringBuilder();
            sb.AppendLine("{");
            line = await reader.ReadLineAsync();

            while (!string.IsNullOrWhiteSpace(line) && line != "}")
            {
                sb.AppendLine(line);
                line = await reader.ReadLineAsync();
            }

            sb.AppendLine("}");
            var appSessionMessage = sb.ToString();

            sessionInfo = AppSessionInfoBuilder.Build(appSessionMessage);
            events++;

            if (sessionInfo?.AppState != AppSessionState.Running)
                sessionInfo = null;
        }

        if (sessionInfo == null)
            Log.Verbose("Caught up, no games are currently running (Processed {EventsProcessed} events)", events);
        else
        {
            Log.Verbose("Caught up (Processed {EventsProcessed} events), emitting {SessionInfo}", events, sessionInfo);
            OnSessionInfoReceived?.Invoke(this, sessionInfo);
        }
    }

    private async Task ProcessLogChunkAsync(string? line, StreamReader reader)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (!line.Contains("AppSessionModule: sessions updated:"))
            return;

        await Task.Delay(TimeSpan.FromSeconds(1));

        var sb = new StringBuilder();
        sb.AppendLine("{");
        line = await reader.ReadLineAsync();

        while (!string.IsNullOrWhiteSpace(line) && line != "}")
        {
            sb.AppendLine(line);
            line = await reader.ReadLineAsync();
        }

        sb.AppendLine("}");
        var appSessionMessage = sb.ToString();
        #if LOG_APP_SESSION_MESSAGES
        _logger.Debug("Received AppSession Message: \n{Line}", appSessionMessage);
        #endif

        var sessionInfo = AppSessionInfoBuilder.Build(appSessionMessage);

        if (sessionInfo == null)
            return;

        OnSessionInfoReceived?.Invoke(this, sessionInfo);
    }

    public void Stop()
    {
        _started = false;
        _logFileWatcher.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Stop();
    }
}
