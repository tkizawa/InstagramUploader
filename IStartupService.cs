namespace InstagramUploader;

/// <summary>
/// Windows 起動時（ログオン時）の自動起動登録および解除を管理する契約です。
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// 現在のユーザーに対して自動起動が有効になっているかどうかを取得します。
    /// </summary>
    /// <returns>自動起動が有効な場合は true、それ以外は false。</returns>
    bool IsAutoStartEnabled();

    /// <summary>
    /// 自動起動の有効/無効を切り替えます。
    /// </summary>
    /// <param name="enabled">自動起動を有効にする場合は true、無効にする場合は false。</param>
    void SetAutoStartEnabled(bool enabled);
}
