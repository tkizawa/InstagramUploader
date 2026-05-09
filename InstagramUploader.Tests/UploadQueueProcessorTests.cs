namespace InstagramUploader.Tests;

public sealed class UploadQueueProcessorTests
{
    [Fact]
    public async Task ProcessFileAsync_MovesFile_WhenUploadSucceeds()
    {
        using var tempDirectory = new TemporaryDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "image.jpg");
        await File.WriteAllTextAsync(filePath, "payload");

        var uploader = new FakeUploader(UploadResult.Success());
        var processor = CreateProcessor(uploader, new StubReadinessChecker(true), new StubCaptionBuilder("caption"));

        await processor.ProcessFileAsync(filePath);

        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(Path.Combine(tempDirectory.Path, "Uploaded", "image.jpg")));
        Assert.Equal("caption", uploader.LastCaption);
    }

    [Fact]
    public async Task ProcessFileAsync_LeavesFileInPlace_WhenUploadFails()
    {
        using var tempDirectory = new TemporaryDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "image.jpg");
        await File.WriteAllTextAsync(filePath, "payload");

        var uploader = new FakeUploader(UploadResult.Failure("failed"));
        var processor = CreateProcessor(uploader, new StubReadinessChecker(true), new StubCaptionBuilder("caption"));

        await processor.ProcessFileAsync(filePath);

        Assert.True(File.Exists(filePath));
        Assert.False(File.Exists(Path.Combine(tempDirectory.Path, "Uploaded", "image.jpg")));
    }

    [Fact]
    public async Task ProcessFileAsync_DoesNotCallUploader_WhenFileIsNotReady()
    {
        using var tempDirectory = new TemporaryDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "image.jpg");
        await File.WriteAllTextAsync(filePath, "payload");

        var uploader = new FakeUploader(UploadResult.Success());
        var processor = CreateProcessor(uploader, new StubReadinessChecker(false), new StubCaptionBuilder("caption"));

        await processor.ProcessFileAsync(filePath);

        Assert.Equal(0, uploader.CallCount);
        Assert.True(File.Exists(filePath));
    }

    private static UploadQueueProcessor CreateProcessor(
        FakeUploader uploader,
        IFileReadinessChecker readinessChecker,
        ICaptionBuilder captionBuilder)
    {
        return new UploadQueueProcessor(uploader, captionBuilder, readinessChecker, new TestLogger());
    }

    private sealed class FakeUploader : IInstagramUploader
    {
        private readonly UploadResult _result;

        public FakeUploader(UploadResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public string? LastCaption { get; private set; }

        public Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCaption = caption;
            return Task.FromResult(_result);
        }
    }

    private sealed class StubReadinessChecker : IFileReadinessChecker
    {
        private readonly bool _result;

        public StubReadinessChecker(bool result)
        {
            _result = result;
        }

        public Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class StubCaptionBuilder : ICaptionBuilder
    {
        private readonly string _caption;

        public StubCaptionBuilder(string caption)
        {
            _caption = caption;
        }

        public string BuildCaption(string filePath)
        {
            return _caption;
        }
    }

    private sealed class TestLogger : IAppLogger
    {
        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
