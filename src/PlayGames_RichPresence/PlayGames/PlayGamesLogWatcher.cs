namespace Dawn.PlayGames.RichPresence.PlayGames;

public class PlayGamesLogWatcher : IDisposable
{
    private readonly FileSystemWatcher _logFileWatcher;
    public PlayGamesLogWatcher(string filePath)
    {
        _logFileWatcher = new();
        _logFileWatcher.Path = Path.GetDirectoryName(filePath)!;
        _logFileWatcher.Filter = Path.GetFileName(filePath);
        _logFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _logFileWatcher.Changed += (_, args) => FileChanged?.Invoke(this, args);
        _logFileWatcher.Error += (_, args) => Error?.Invoke(this, args);
    }

    public event EventHandler<FileSystemEventArgs>? FileChanged;
    public event EventHandler<ErrorEventArgs>? Error;

    public void Initialize() => _logFileWatcher.EnableRaisingEvents = true;
    public void Stop() => _logFileWatcher.EnableRaisingEvents = false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _logFileWatcher.EnableRaisingEvents = false;
        _logFileWatcher.Dispose();
    }
}
