namespace InstagramUploader;

/// <summary>
/// 監視フォルダのファイルイベントをアップロードキューへ橋渡しします。
/// </summary>
public sealed class FolderWatchService : IFolderWatchService
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _folderPath;
    private readonly Action<string> _onImageDetected;
    private readonly IAppLogger _logger;

    /// <summary>
    /// <see cref="FolderWatchService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="folderPath">監視対象フォルダです。</param>
    /// <param name="onImageDetected">画像検知時のコールバックです。</param>
    /// <param name="logger">ロガーです。</param>
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher.Dispose();
    }

    /// <summary>
    /// 作成イベントを処理します。
    /// </summary>
    /// <param name="sender">イベント送信元です。</param>
    /// <param name="e">イベント引数です。</param>
    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        HandlePath(e.FullPath);
    }

    /// <summary>
    /// リネームイベントを処理します。
    /// </summary>
    /// <param name="sender">イベント送信元です。</param>
    /// <param name="e">イベント引数です。</param>
    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        HandlePath(e.FullPath);
    }

    /// <summary>
    /// 検知したパスが対象画像であればキューへ渡します。
    /// </summary>
    /// <param name="path">検知したファイルパスです。</param>
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
