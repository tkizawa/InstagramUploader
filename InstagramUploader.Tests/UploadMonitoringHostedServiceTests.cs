namespace InstagramUploader.Tests;

public sealed class UploadMonitoringHostedServiceTests
{
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

    private sealed class FakeWatcher : IFolderWatchService
    {
        public bool StartCalled { get; private set; }

        public bool StopCalled { get; private set; }

        public void Start()
        {
            StartCalled = true;
        }

        public void Stop()
        {
            StopCalled = true;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeProcessor : IUploadQueueProcessor
    {
        public bool StartCalled { get; private set; }

        public bool StopCalled { get; private set; }

        public void Start()
        {
            StartCalled = true;
        }

        public void Enqueue(string filePath)
        {
        }

        public Task StopAsync()
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        public Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestLogger : IAppLogger
    {
        public List<string> Messages { get; } = new();

        public void Info(string message)
        {
            Messages.Add(message);
        }

        public void Error(string message, Exception? exception = null)
        {
            Messages.Add(message);
        }
    }
}
