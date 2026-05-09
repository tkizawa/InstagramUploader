namespace InstagramUploader;

/// <summary>
/// ユーザー通知の表示契約を表します。
/// </summary>
public interface IUserNotifier
{
    /// <summary>
    /// 情報通知を表示します。
    /// </summary>
    /// <param name="title">通知タイトルです。</param>
    /// <param name="message">通知本文です。</param>
    void ShowInfo(string title, string message);

    /// <summary>
    /// エラー通知を表示します。
    /// </summary>
    /// <param name="title">通知タイトルです。</param>
    /// <param name="message">通知本文です。</param>
    void ShowError(string title, string message);
}
