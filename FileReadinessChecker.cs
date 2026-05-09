namespace InstagramUploader;

/// <summary>
/// ファイルのサイズ安定と排他オープン可否で書き込み完了を判定します。
/// </summary>
public sealed class FileReadinessChecker : IFileReadinessChecker
{
    /// <summary>
    /// <see cref="FileReadinessChecker"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="pollInterval">再試行間隔です。</param>
    /// <param name="maxAttempts">最大試行回数です。</param>
    public FileReadinessChecker(TimeSpan? pollInterval = null, int maxAttempts = 30)
    {
        PollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        MaxAttempts = maxAttempts;
    }

    /// <summary>
    /// 再試行間隔です。
    /// </summary>
    public TimeSpan PollInterval { get; }

    /// <summary>
    /// 最大試行回数です。
    /// </summary>
    public int MaxAttempts { get; }

    /// <inheritdoc />
    public async Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        long previousLength = -1;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsReady(filePath, previousLength, out var currentLength))
            {
                return true;
            }

            previousLength = currentLength;
            await Task.Delay(PollInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// 現時点でファイルが安定して読み取れるかを判定します。
    /// </summary>
    /// <param name="filePath">確認対象のファイルです。</param>
    /// <param name="previousLength">前回確認時のファイルサイズです。</param>
    /// <param name="currentLength">今回確認したファイルサイズです。</param>
    /// <returns>読み取り可能なら <see langword="true"/> です。</returns>
    private static bool IsReady(string filePath, long previousLength, out long currentLength)
    {
        currentLength = -1;

        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var info = new FileInfo(filePath);
            currentLength = info.Length;

            if (currentLength <= 0 || currentLength != previousLength)
            {
                return false;
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length == currentLength;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
