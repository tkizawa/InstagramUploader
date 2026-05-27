namespace InstagramUploader;

/// <summary>
/// アップロード処理の結果を表します。
/// </summary>
/// <param name="Succeeded">アップロードが成功したかどうかです。</param>
/// <param name="Message">結果の補足メッセージです。</param>
public sealed record UploadResult(bool Succeeded, string? Message = null)
{
    /// <summary>
    /// 成功結果を生成します。
    /// </summary>
    /// <param name="message">補足メッセージです。</param>
    /// <returns>成功結果です。</returns>
    public static UploadResult Success(string? message = null)
    {
        return new UploadResult(true, message);
    }

    /// <summary>
    /// 失敗結果を生成します。
    /// </summary>
    /// <param name="message">失敗理由です。</param>
    /// <returns>失敗結果です。</returns>
    public static UploadResult Failure(string message)
    {
        return new UploadResult(false, message);
    }
}
