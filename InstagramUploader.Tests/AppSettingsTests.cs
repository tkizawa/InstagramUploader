namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="AppSettings"/> の設定読み込み動作を検証します。
/// </summary>
public sealed class AppSettingsTests
{
    /// <summary>
    /// UploadFolder が指定されている場合にその値が優先されることを検証します。
    /// </summary>
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

    /// <summary>
    /// UploadFolder 未指定時に既定の Uploads フォルダへフォールバックすることを検証します。
    /// </summary>
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

    /// <summary>
    /// 必須項目が欠けている JSON を拒否することを検証します。
    /// </summary>
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

    /// <summary>
    /// 構成オブジェクトから User Secrets 相当の値を読み取れることを検証します。
    /// </summary>
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

    /// <summary>
    /// 構成オブジェクトで UploadFolder 未指定時に既定値を使用することを検証します。
    /// </summary>
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

    /// <summary>
    /// credentials.json がない場合に構成オブジェクトへフォールバックすることを検証します。
    /// </summary>
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
