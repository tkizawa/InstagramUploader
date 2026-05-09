namespace InstagramUploader;

/// <summary>
/// アプリケーションログの出力契約を表します。
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// 情報ログを書き込みます。
    /// </summary>
    /// <param name="message">ログメッセージです。</param>
    void Info(string message);

    /// <summary>
    /// エラーログを書き込みます。
    /// </summary>
    /// <param name="message">ログメッセージです。</param>
    /// <param name="exception">関連する例外です。</param>
    void Error(string message, Exception? exception = null);
}
