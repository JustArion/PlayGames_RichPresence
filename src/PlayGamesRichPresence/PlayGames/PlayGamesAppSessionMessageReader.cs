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
    private const string FILE_PATH = @"Google\Play Games\Logs\Service.log";

    private bool _started;
    private bool _shouldContinue;
    private FileStream? _fileStream;

    public event EventHandler<PlayGamesSessionInfo>? OnSessionInfoReceived;

    public void StartAsync()
    {
        if (_started)
            return;
        _started = true;
        _shouldContinue = true;

        Task.Factory.StartNew(DoReadOperation, TaskCreationOptions.LongRunning);
    }

    private async Task DoReadOperation()
    {
        while (_shouldContinue)
        {
            Log.Verbose("Doing fresh read-operation pass");
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FILE_PATH);

            // Wait till the file exists
            if (!File.Exists(filePath))
            {
                _logger.Debug("File not found: {FilePath}", FILE_PATH);
                await Task.Delay(TimeSpan.FromSeconds(5));
                continue;
            }

            // Wait till we can open the file
            try
            {
                _fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to open file: {FilePath}", filePath);
                if (_fileStream != null)
                    await _fileStream.DisposeAsync().AsTask();

                await Task.Delay(TimeSpan.FromSeconds(5));
                continue;
            }

            using var reader = new StreamReader(_fileStream);

            using (Warn.OnLongerThan(TimeSpan.FromSeconds(2), "Catch-Up took unusually long"))
                await CatchUpAsync(reader);

            // We read new things being added from here onwards
            while (_shouldContinue)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {

                    // 'Polling' limiter
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                await ProcessLogChunkAsync(line, reader);
            }
        }
    }

    /// <summary>
    /// The method ensures that a Rich Presence will be enabled if a game is running before this program started.
    /// </summary>
    /// <param name="reader"></param>
    private async Task CatchUpAsync(StreamReader reader)
    {
        Log.Verbose("Catching up...");
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

            if (sessionInfo?.AppState != AppSessionState.Running)
                sessionInfo = null;
        }

        if (sessionInfo == null)
            Log.Verbose("Caught up, no games are currently running");
        else
        {
            Log.Verbose("Caught up, emitting {SessionInfo}", sessionInfo);
            OnSessionInfoReceived?.Invoke(this, sessionInfo);
        }
    }

    private async Task ProcessLogChunkAsync(string? line, StreamReader reader)
    {
        if (string.IsNullOrWhiteSpace(line))
            return; // This should never happen as the code above ensures it. But for convinience we just add it here too.

        // This actually worked first try ;) Nice!!
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
        _shouldContinue = false;
        _fileStream?.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Stop();
    }
}
