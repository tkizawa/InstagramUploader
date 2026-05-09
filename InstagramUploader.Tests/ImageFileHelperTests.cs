namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="ImageFileHelper"/> の補助処理を検証します。
/// </summary>
public sealed class ImageFileHelperTests
{
    /// <summary>
    /// 対応拡張子判定が期待どおりに動作することを検証します。
    /// </summary>
    /// <param name="path">判定対象パスです。</param>
    /// <param name="expected">期待値です。</param>
    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.gif", false)]
    [InlineData("photo.txt", false)]
    public void IsSupportedImage_ReturnsExpectedResult(string path, bool expected)
    {
        Assert.Equal(expected, ImageFileHelper.IsSupportedImage(path));
    }

    /// <summary>
    /// Uploaded フォルダ配下の移動先パスが生成されることを検証します。
    /// </summary>
    [Fact]
    public void GetUploadedFilePath_ReturnsUploadedSubfolderPath()
    {
        var path = ImageFileHelper.GetUploadedFilePath("C:\\Work\\image.jpg");

        Assert.Equal("C:\\Work\\Uploaded\\image.jpg", path);
    }
}
