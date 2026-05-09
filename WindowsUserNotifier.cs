using System.Windows.Forms;

namespace InstagramUploader;

/// <summary>
/// MessageBox を使って通知を表示する実装です。
/// </summary>
public sealed class WindowsUserNotifier : IUserNotifier
{
    /// <inheritdoc />
    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <inheritdoc />
    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
