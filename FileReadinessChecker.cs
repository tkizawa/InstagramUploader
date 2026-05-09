namespace InstagramUploader;

public sealed class FileReadinessChecker : IFileReadinessChecker
{
    public FileReadinessChecker(TimeSpan? pollInterval = null, int maxAttempts = 30)
    {
        PollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        MaxAttempts = maxAttempts;
    }

    public TimeSpan PollInterval { get; }

    public int MaxAttempts { get; }

    public async Task<bool> WaitUntilReadyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        long previousLength = -1;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsReady(filePath, previousLength, out var currentLength))
            {
                return true;
            }

            previousLength = currentLength;
            await Task.Delay(PollInterval, cancellationToken);
        }

        return false;
    }

    private static bool IsReady(string filePath, long previousLength, out long currentLength)
    {
        currentLength = -1;

        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var info = new FileInfo(filePath);
            currentLength = info.Length;

            if (currentLength <= 0 || currentLength != previousLength)
            {
                return false;
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length == currentLength;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
