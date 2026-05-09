namespace InstagramUploader;

public sealed class FolderWatchService : IFolderWatchService
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _folderPath;
    private readonly Action<string> _onImageDetected;
    private readonly IAppLogger _logger;

    public FolderWatchService(string folderPath, Action<string> onImageDetected, IAppLogger logger)
    {
        _folderPath = folderPath;
        _onImageDetected = onImageDetected;
        _logger = logger;
        Directory.CreateDirectory(_folderPath);

        _watcher = new FileSystemWatcher(_folderPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "*.*",
            IncludeSubdirectories = false
        };

        _watcher.Created += OnCreated;
        _watcher.Renamed += OnRenamed;
    }

    public void Start()
    {
        Directory.CreateDirectory(_folderPath);

        foreach (var filePath in ImageFileHelper.EnumerateSupportedFiles(_folderPath))
        {
            _logger.Info($"既存ファイルを検知しました: {filePath}");
            _onImageDetected(filePath);
        }

        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        HandlePath(e.FullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        HandlePath(e.FullPath);
    }

    private void HandlePath(string path)
    {
        if (!ImageFileHelper.IsSupportedImage(path))
        {
            return;
        }

        _logger.Info($"新しい画像を検知しました: {path}");
        _onImageDetected(path);
    }
}
