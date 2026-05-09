namespace InstagramUploader;

public static class ImageFileHelper
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    public static bool IsSupportedImage(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path));
    }

    public static IReadOnlyList<string> EnumerateSupportedFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(folderPath)
            .Where(IsSupportedImage)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string GetUploadedFilePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("ファイルの親フォルダを取得できません。");
        var uploadedDirectory = Path.Combine(directory, "Uploaded");
        return Path.Combine(uploadedDirectory, Path.GetFileName(filePath));
    }
}
