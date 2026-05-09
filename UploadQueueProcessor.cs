using System.Collections.Concurrent;
using System.Threading.Channels;

namespace InstagramUploader;

public sealed class UploadQueueProcessor : IUploadQueueProcessor
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly ConcurrentDictionary<string, byte> _scheduledFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly IInstagramUploader _uploader;
    private readonly ICaptionBuilder _captionBuilder;
    private readonly IFileReadinessChecker _readinessChecker;
    private readonly IAppLogger _logger;
    private Task? _processingTask;
    private bool _stopRequested;

    public UploadQueueProcessor(
        IInstagramUploader uploader,
        ICaptionBuilder captionBuilder,
        IFileReadinessChecker readinessChecker,
        IAppLogger logger)
    {
        _uploader = uploader;
        _captionBuilder = captionBuilder;
        _readinessChecker = readinessChecker;
        _logger = logger;
    }

    public void Start()
    {
        _processingTask ??= Task.Run(ProcessLoopAsync);
    }

    public void Enqueue(string filePath)
    {
        if (_stopRequested || !ImageFileHelper.IsSupportedImage(filePath))
        {
            return;
        }

        var normalizedPath = Path.GetFullPath(filePath);
        if (_scheduledFiles.TryAdd(normalizedPath, 0))
        {
            _logger.Info($"アップロード待ちに追加しました: {normalizedPath}");
            _queue.Writer.TryWrite(normalizedPath);
        }
    }

    public async Task StopAsync()
    {
        if (_stopRequested)
        {
            if (_processingTask is not null)
            {
                await _processingTask;
            }

            return;
        }

        _stopRequested = true;
        _queue.Writer.TryComplete();

        if (_processingTask is not null)
        {
            await _processingTask;
        }
    }

    public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await _readinessChecker.WaitUntilReadyAsync(filePath, cancellationToken))
        {
            _logger.Error($"ファイルの書き込み完了を確認できませんでした: {filePath}");
            return;
        }

        if (!File.Exists(filePath))
        {
            _logger.Error($"アップロード対象ファイルが見つかりません: {filePath}");
            return;
        }

        var caption = _captionBuilder.BuildCaption(filePath);
        var result = await _uploader.UploadAsync(filePath, caption, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.Error($"アップロードに失敗しました: {filePath} {result.Message}");
            return;
        }

        MoveToUploadedFolder(filePath);
        _logger.Info($"アップロード済みフォルダへ移動しました: {filePath}");
    }

    private async Task ProcessLoopAsync()
    {
        await foreach (var filePath in _queue.Reader.ReadAllAsync())
        {
            try
            {
                await ProcessFileAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.Error($"アップロード処理でエラーが発生しました: {filePath}", ex);
            }
            finally
            {
                _scheduledFiles.TryRemove(filePath, out _);
            }
        }
    }

    private static void MoveToUploadedFolder(string filePath)
    {
        var destinationPath = ImageFileHelper.GetUploadedFilePath(filePath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException("Uploaded フォルダを作成できません。");
        Directory.CreateDirectory(destinationDirectory);
        File.Move(filePath, destinationPath, overwrite: true);
    }
}
