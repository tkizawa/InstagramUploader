namespace InstagramUploader;

/// <summary>
/// ファイルが読み取り可能になるまで待機する契約を表します。
/// </summary>
public interface IFileReadinessChecker
{
    /// <summary>
    /// ファイルが安定して読み取れる状態になるまで待機します。
    /// </summary>
    /// <param name="filePath">確認対象のファイルです。</param>
    /// <param name="cancellationToken">キャンセル トークンです。</param>
    /// <returns>準備完了なら <see langword="true"/> です。</returns>
    Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default);
}
