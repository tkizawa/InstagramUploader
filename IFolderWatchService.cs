namespace InstagramUploader;

/// <summary>
/// 監視フォルダの開始と停止を表す契約です。
/// </summary>
public interface IFolderWatchService : IDisposable
{
    /// <summary>
    /// フォルダ監視を開始します。
    /// </summary>
    void Start();

    /// <summary>
    /// フォルダ監視を停止します。
    /// </summary>
    void Stop();

    /// <summary>
    /// 監視対象フォルダを変更します。
    /// </summary>
    /// <param name="newFolderPath">新しい監視対象フォルダです。</param>
    void UpdateWatchFolder(string newFolderPath);
}
