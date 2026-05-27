namespace InstagramUploader.Tests;

/// <summary>
/// テスト用の一時ディレクトリを管理します。
/// </summary>
public sealed class TemporaryDirectory : IDisposable
{
    /// <summary>
    /// <see cref="TemporaryDirectory"/> の新しいインスタンスを初期化します。
    /// </summary>
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// 作成された一時ディレクトリのパスです。
    /// </summary>
    public string Path { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
