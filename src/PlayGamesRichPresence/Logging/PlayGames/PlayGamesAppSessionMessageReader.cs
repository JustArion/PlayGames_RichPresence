namespace Dawn.PlayGames.RichPresence.Logs.PlayGames;

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

    public event Action<string> OnAppSessionMessageReceived = _ => { };

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

            // We read new things being added only.
            _fileStream.Position = _fileStream.Length;
            using var reader = new StreamReader(_fileStream);

            while (_shouldContinue)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    
                    // 'Polling' limiter
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                // This actually worked first try ;) Nice!!
                if (!line.Contains("AppSessionModule: sessions updated:")) 
                    continue;
                
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
                // _logger.Debug("Received AppSession Message: \n{Line}", appSessionMessage);

                try
                {
                    OnAppSessionMessageReceived?.Invoke(appSessionMessage);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Failed to invoke {nameof(OnAppSessionMessageReceived)}");
                }

            }
        }
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