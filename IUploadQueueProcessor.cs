namespace InstagramUploader;

public interface IUploadQueueProcessor
{
    void Start();
    void Enqueue(string filePath);
    Task StopAsync();
    Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
}
