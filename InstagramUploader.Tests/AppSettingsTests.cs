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

    [Fact]
    public void FromConfiguration_UsesUserSecretsValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Username"] = "user@example.com",
                ["Password"] = "secret",
                ["UploadFolder"] = "C:\\SecretUploads"
            })
            .Build();

        var settings = AppSettings.FromConfiguration(configuration, "C:\\App");

        Assert.Equal("user@example.com", settings.Username);
        Assert.Equal("secret", settings.Password);
        Assert.Equal("C:\\SecretUploads", settings.UploadFolder);
    }

    [Fact]
    public void FromConfiguration_FallsBackToDefaultUploadFolder_WhenUploadFolderIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Username"] = "user@example.com",
                ["Password"] = "secret"
            })
            .Build();

        var settings = AppSettings.FromConfiguration(configuration, "C:\\App");

        Assert.Equal("C:\\App\\Uploads", settings.UploadFolder);
    }

    [Fact]
    public void Load_UsesConfiguration_WhenCredentialsFileIsMissing()
    {
        using var tempDirectory = new TemporaryDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Username"] = "user@example.com",
                ["Password"] = "secret"
            })
            .Build();

        var settings = AppSettings.Load(tempDirectory.Path, configuration);

        Assert.Equal("user@example.com", settings.Username);
        Assert.Equal("secret", settings.Password);
        Assert.Equal(Path.Combine(tempDirectory.Path, "Uploads"), settings.UploadFolder);
    }
}
