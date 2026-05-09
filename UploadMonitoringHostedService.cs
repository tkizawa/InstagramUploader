using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

/// <summary>
/// ホストの開始停止に合わせて監視処理を起動する Hosted Service です。
/// </summary>
/// <remarks>
/// <see cref="UploadMonitoringHostedService"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="settings">アプリケーション設定です。</param>
/// <param name="folderWatchService">フォルダ監視サービスです。</param>
/// <param name="uploadQueueProcessor">アップロードキューです。</param>
/// <param name="logger">ロガーです。</param>
public sealed class UploadMonitoringHostedService(
    AppSettings settings,
    IFolderWatchService folderWatchService,
    IUploadQueueProcessor uploadQueueProcessor,
    IAppLogger logger) : IHostedService
{
    private readonly AppSettings _settings = settings;
    private readonly IFolderWatchService _folderWatchService = folderWatchService;
    private readonly IUploadQueueProcessor _uploadQueueProcessor = uploadQueueProcessor;
    private readonly IAppLogger _logger = logger;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("=== アプリケーション起動 ===");
        _logger.Info($"フォルダの監視を開始します: {_settings.UploadFolder}");

        _uploadQueueProcessor.Start();
        _folderWatchService.Start();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _folderWatchService.Stop();
        await _uploadQueueProcessor.StopAsync();
        _logger.Info("アプリケーションを終了しました。");
    }
}
