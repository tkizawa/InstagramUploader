namespace InstagramUploader;

/// <summary>
/// Instagram へのアップロード処理を表す契約です。
/// </summary>
public interface IInstagramUploader
{
    /// <summary>
    /// 指定ファイルを Instagram へアップロードします。
    /// </summary>
    /// <param name="filePath">アップロード対象ファイルです。</param>
    /// <param name="caption">投稿キャプションです。</param>
    /// <param name="cancellationToken">キャンセル トークンです。</param>
    /// <returns>アップロード結果です。</returns>
    Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default);
}
