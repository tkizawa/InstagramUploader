namespace InstagramUploader.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void Parse_UsesConfiguredUploadFolder_WhenProvided()
    {
        const string json = """
            {
              "Username": "user@example.com",
              "Password": "secret",
              "UploadFolder": "C:\\Uploads"
            }
            """;

        var settings = AppSettings.Parse(json, "C:\\App");

        Assert.Equal("user@example.com", settings.Username);
        Assert.Equal("secret", settings.Password);
        Assert.Equal("C:\\Uploads", settings.UploadFolder);
        Assert.Equal("C:\\App\\BrowserState", settings.BrowserStateDirectory);
        Assert.Equal("C:\\App\\app_log.txt", settings.LogFilePath);
    }

    [Fact]
    public void Parse_FallsBackToDefaultUploadFolder_WhenUploadFolderIsMissing()
    {
        const string json = """
            {
              "Username": "user@example.com",
              "Password": "secret"
            }
            """;

        var settings = AppSettings.Parse(json, "C:\\App");

        Assert.Equal("C:\\App\\Uploads", settings.UploadFolder);
    }

    [Fact]
    public void Parse_Throws_WhenRequiredPropertyIsMissing()
    {
        const string json = """
            {
              "Username": "user@example.com"
            }
            """;

        Assert.Throws<InvalidDataException>(() => AppSettings.Parse(json, "C:\\App"));
    }
}
