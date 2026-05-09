namespace InstagramUploader;

/// <summary>
/// 画像ファイルの判定とパス計算を補助します。
/// </summary>
public static class ImageFileHelper
{
    /// <summary>
    /// 対象拡張子の画像ファイルかどうかを判定します。
    /// </summary>
    /// <param name="path">判定対象のパスです。</param>
    /// <returns>対応画像であれば <see langword="true"/> です。</returns>
    public static bool IsSupportedImage(string path)
    {
        var extension = Path.GetExtension(path.AsSpan());
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// フォルダ内の対応画像ファイルを列挙します。
    /// </summary>
    /// <param name="folderPath">対象フォルダです。</param>
    /// <returns>ソート済みのファイル一覧です。</returns>
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

    /// <summary>
    /// アップロード済みファイルの移動先パスを返します。
    /// </summary>
    /// <param name="filePath">元ファイルのパスです。</param>
    /// <returns>Uploaded フォルダ配下の移動先パスです。</returns>
    public static string GetUploadedFilePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("ファイルの親フォルダを取得できません。");
        var uploadedDirectory = Path.Combine(directory, "Uploaded");
        return Path.Combine(uploadedDirectory, Path.GetFileName(filePath));
    }
}
