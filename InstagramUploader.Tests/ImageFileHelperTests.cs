namespace InstagramUploader.Tests;

public sealed class ImageFileHelperTests
{
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

    [Fact]
    public void GetUploadedFilePath_ReturnsUploadedSubfolderPath()
    {
        var path = ImageFileHelper.GetUploadedFilePath("C:\\Work\\image.jpg");

        Assert.Equal("C:\\Work\\Uploaded\\image.jpg", path);
    }
}
