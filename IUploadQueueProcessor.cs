namespace InstagramUploader;

/// <summary>
/// アップロードキューの制御契約を表します。
/// </summary>
public interface IUploadQueueProcessor
{
    /// <summary>
    /// バックグラウンド処理ループを開始します。
    /// </summary>
    void Start();

    /// <summary>
    /// ファイルをアップロード待ちキューへ追加します。
    /// </summary>
    /// <param name="filePath">対象ファイルです。</param>
    void Enqueue(string filePath);

    /// <summary>
    /// バックグラウンド処理を停止します。
    /// </summary>
    /// <returns>停止完了タスクです。</returns>
    Task StopAsync();

    /// <summary>
    /// 単一ファイルのアップロード処理を実行します。
    /// </summary>
    /// <param name="filePath">対象ファイルです。</param>
    /// <param name="cancellationToken">キャンセル トークンです。</param>
    /// <returns>処理完了タスクです。</returns>
    Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
}
