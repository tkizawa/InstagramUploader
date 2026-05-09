namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="FileReadinessChecker"/> の判定ロジックを検証します。
/// </summary>
public sealed class FileReadinessCheckerTests
{
    /// <summary>
    /// 読み取り可能なファイルを準備完了と判定することを検証します。
    /// </summary>
    [Fact]
    public async Task WaitUntilReadyAsync_ReturnsTrue_WhenFileIsReadable()
    {
        using var tempDirectory = new TemporaryDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "image.jpg");
        await File.WriteAllTextAsync(filePath, "ready");

        var checker = new FileReadinessChecker(TimeSpan.FromMilliseconds(10), maxAttempts: 5);

        var isReady = await checker.WaitUntilReadyAsync(filePath);

        Assert.True(isReady);
    }

    /// <summary>
    /// 存在しないファイルを未準備と判定することを検証します。
    /// </summary>
    [Fact]
    public async Task WaitUntilReadyAsync_ReturnsFalse_WhenFileNeverAppears()
    {
        using var tempDirectory = new TemporaryDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "missing.jpg");
        var checker = new FileReadinessChecker(TimeSpan.FromMilliseconds(10), maxAttempts: 3);

        var isReady = await checker.WaitUntilReadyAsync(filePath);

        Assert.False(isReady);
    }
}
