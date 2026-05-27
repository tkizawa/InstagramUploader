namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="UploadMonitoringHostedService"/> の起動停止動作を検証します。
/// </summary>
public sealed class UploadMonitoringHostedServiceTests
{
    /// <summary>
    /// StartAsync が監視サービスとキューを開始することを検証します。
    /// </summary>
    [Fact]
    public async Task StartAsync_StartsProcessorAndWatcher()
    {
        var watcher = new FakeWatcher();
        var processor = new FakeProcessor();
        var logger = new TestLogger();
        var settings = new AppSettings("user", "password", "C:\\Uploads", "C:\\BrowserState", "C:\\app_log.txt");
        var service = new UploadMonitoringHostedService(settings, watcher, processor, logger);

        await service.StartAsync(CancellationToken.None);

        Assert.True(watcher.StartCalled);
        Assert.True(processor.StartCalled);
        Assert.Contains(logger.Messages, message => message.Contains("フォルダの監視を開始します"));
    }

    /// <summary>
    /// StopAsync が監視サービスとキューを停止することを検証します。
    /// </summary>
    [Fact]
    public async Task StopAsync_StopsWatcherAndProcessor()
    {
        var watcher = new FakeWatcher();
        var processor = new FakeProcessor();
        var logger = new TestLogger();
        var settings = new AppSettings("user", "password", "C:\\Uploads", "C:\\BrowserState", "C:\\app_log.txt");
        var service = new UploadMonitoringHostedService(settings, watcher, processor, logger);

        await service.StopAsync(CancellationToken.None);

        Assert.True(watcher.StopCalled);
        Assert.True(processor.StopCalled);
        Assert.Contains(logger.Messages, message => message.Contains("アプリケーションを終了しました"));
    }

    /// <summary>
    /// フォルダ監視の呼び出しを記録するテストダブルです。
    /// </summary>
    private sealed class FakeWatcher : IFolderWatchService
    {
        /// <summary>
        /// Start が呼ばれたかどうかです。
        /// </summary>
        public bool StartCalled { get; private set; }

        /// <summary>
        /// Stop が呼ばれたかどうかです。
        /// </summary>
        public bool StopCalled { get; private set; }

        /// <inheritdoc />
        public void Start()
        {
            StartCalled = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            StopCalled = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// アップロードキューの呼び出しを記録するテストダブルです。
    /// </summary>
    private sealed class FakeProcessor : IUploadQueueProcessor
    {
        /// <summary>
        /// Start が呼ばれたかどうかです。
        /// </summary>
        public bool StartCalled { get; private set; }

        /// <summary>
        /// StopAsync が呼ばれたかどうかです。
        /// </summary>
        public bool StopCalled { get; private set; }

        /// <inheritdoc />
        public void Start()
        {
            StartCalled = true;
        }

        /// <inheritdoc />
        public void Enqueue(string filePath)
        {
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 記録専用のテスト用ロガーです。
    /// </summary>
    private sealed class TestLogger : IAppLogger
    {
        /// <summary>
        /// 記録されたメッセージ一覧です。
        /// </summary>
        public List<string> Messages { get; } = new();

        /// <inheritdoc />
        public void Info(string message)
        {
            Messages.Add(message);
        }

        /// <inheritdoc />
        public void Error(string message, Exception? exception = null)
        {
            Messages.Add(message);
        }
    }
}
