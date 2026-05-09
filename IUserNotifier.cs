namespace InstagramUploader;

public interface IUserNotifier
{
    void ShowInfo(string title, string message);
    void ShowError(string title, string message);
}
