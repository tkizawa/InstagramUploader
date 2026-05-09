namespace InstagramUploader;

public interface IFolderWatchService : IDisposable
{
    void Start();
    void Stop();
}
