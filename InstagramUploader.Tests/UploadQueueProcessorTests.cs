namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="UploadQueueProcessor"/> のアップロード制御を検証します。
/// </summary>
public sealed class UploadQueueProcessorTests
{
    /// <summary>
    /// 成功時にファイルが Uploaded フォルダへ移動することを検証します。
    /// </summary>
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

    /// <summary>
    /// 失敗時にファイルを元の場所へ残すことを検証します。
    /// </summary>
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

    /// <summary>
    /// 準備未完了のファイルに対してアップローダーを呼び出さないことを検証します。
    /// </summary>
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

    /// <summary>
    /// テスト用のプロセッサーを構築します。
    /// </summary>
    /// <param name="uploader">テスト用アップローダーです。</param>
    /// <param name="readinessChecker">テスト用準備判定器です。</param>
    /// <param name="captionBuilder">テスト用キャプション生成器です。</param>
    /// <returns>テスト対象のプロセッサーです。</returns>
    private static UploadQueueProcessor CreateProcessor(
        FakeUploader uploader,
        IFileReadinessChecker readinessChecker,
        ICaptionBuilder captionBuilder)
    {
        return new UploadQueueProcessor(uploader, captionBuilder, readinessChecker, new TestLogger());
    }

    /// <summary>
    /// アップロード結果を固定で返すテストダブルです。
    /// </summary>
    /// <remarks>
    /// <see cref="FakeUploader"/> の新しいインスタンスを初期化します。
    /// </remarks>
    /// <param name="result">返却する結果です。</param>
    private sealed class FakeUploader(UploadResult result) : IInstagramUploader
    {
        private readonly UploadResult _result = result;

        /// <summary>
        /// 呼び出し回数です。
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// 最後に受け取ったキャプションです。
        /// </summary>
        public string? LastCaption { get; private set; }

        /// <inheritdoc />
        public Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCaption = caption;
            return Task.FromResult(_result);
        }
    }

    /// <summary>
    /// ファイル準備完了判定を固定値で返すテストダブルです。
    /// </summary>
    /// <remarks>
    /// <see cref="StubReadinessChecker"/> の新しいインスタンスを初期化します。
    /// </remarks>
    /// <param name="result">返却する判定結果です。</param>
    private sealed class StubReadinessChecker(bool result) : IFileReadinessChecker
    {
        private readonly bool _result = result;

        /// <inheritdoc />
        public Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    /// <summary>
    /// 固定キャプションを返すテストダブルです。
    /// </summary>
    /// <remarks>
    /// <see cref="StubCaptionBuilder"/> の新しいインスタンスを初期化します。
    /// </remarks>
    /// <param name="caption">返却するキャプションです。</param>
    private sealed class StubCaptionBuilder(string caption) : ICaptionBuilder
    {
        private readonly string _caption = caption;

        /// <inheritdoc />
        public string BuildCaption(string filePath)
        {
            return _caption;
        }
    }

    /// <summary>
    /// 出力を行わないテスト用ロガーです。
    /// </summary>
    private sealed class TestLogger : IAppLogger
    {
        /// <inheritdoc />
        public void Info(string message)
        {
        }

        /// <inheritdoc />
        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
