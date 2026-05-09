using System.Windows.Forms;

namespace InstagramUploader;

public sealed class WindowsUserNotifier : IUserNotifier
{
    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
