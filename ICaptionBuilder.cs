namespace InstagramUploader;

/// <summary>
/// 画像ファイルからキャプションを生成する契約を表します。
/// </summary>
public interface ICaptionBuilder
{
    /// <summary>
    /// 画像ファイルに対応するキャプションを生成します。
    /// </summary>
    /// <param name="filePath">画像ファイルのパスです。</param>
    /// <returns>生成されたキャプションです。</returns>
    string BuildCaption(string filePath);
}
