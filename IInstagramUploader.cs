namespace InstagramUploader;

public interface IInstagramUploader
{
    Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default);
}
