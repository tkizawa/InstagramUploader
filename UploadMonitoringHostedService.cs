using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

public sealed class UploadMonitoringHostedService : IHostedService
{
    private readonly AppSettings _settings;
    private readonly IFolderWatchService _folderWatchService;
    private readonly IUploadQueueProcessor _uploadQueueProcessor;
    private readonly IAppLogger _logger;

    public UploadMonitoringHostedService(
        AppSettings settings,
        IFolderWatchService folderWatchService,
        IUploadQueueProcessor uploadQueueProcessor,
        IAppLogger logger)
    {
        _settings = settings;
        _folderWatchService = folderWatchService;
        _uploadQueueProcessor = uploadQueueProcessor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("=== アプリケーション起動 ===");
        _logger.Info($"フォルダの監視を開始します: {_settings.UploadFolder}");

        _uploadQueueProcessor.Start();
        _folderWatchService.Start();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _folderWatchService.Stop();
        await _uploadQueueProcessor.StopAsync();
        _logger.Info("アプリケーションを終了しました。");
    }
}
