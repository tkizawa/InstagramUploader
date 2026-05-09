namespace InstagramUploader;

public sealed record UploadResult(bool Succeeded, string? Message = null)
{
    public static UploadResult Success(string? message = null)
    {
        return new UploadResult(true, message);
    }

    public static UploadResult Failure(string message)
    {
        return new UploadResult(false, message);
    }
}
