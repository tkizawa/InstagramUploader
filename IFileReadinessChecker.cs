namespace InstagramUploader;

public interface IFileReadinessChecker
{
    Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default);
}
