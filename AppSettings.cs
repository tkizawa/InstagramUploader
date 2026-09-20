using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace InstagramUploader;

/// <summary>
/// アプリケーションの実行設定を表します。
/// </summary>
/// <param name="Username">Facebook ログインに使用するユーザー名です。</param>
/// <param name="Password">Facebook ログインに使用するパスワードです。</param>
/// <param name="UploadFolder">監視対象のフォルダです。</param>
/// <param name="BrowserStateDirectory">Playwright のブラウザー状態を保存するフォルダです。</param>
/// <param name="LogFilePath">アプリケーションログの出力先です。</param>
public sealed record AppSettings(
    string Username,
    string Password,
    string UploadFolder,
    string BrowserStateDirectory,
    string LogFilePath)
{
    /// <summary>
    /// 設定ファイルおよびデータ保存先ディレクトリ（%LocalAppData%\InstagramUploader）を取得します。
    /// </summary>
    public static string DefaultSettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InstagramUploader");

    /// <summary>
    /// 設定ファイルの既定の保存先パスを取得します。
    /// </summary>
    public static string DefaultCredentialsPath =>
        Path.Combine(DefaultSettingsDirectory, "credentials.json");

    /// <summary>
    /// 設定が正しく構成されているか（必須項目が入力されているか）を取得します。
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(UploadFolder);

    /// <summary>
    /// 未設定時の既定の <see cref="AppSettings"/> インスタンスを生成します。
    /// </summary>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <returns>既定の設定です。</returns>
    public static AppSettings CreateDefault(string appDirectory)
    {
        var dataDir = DefaultSettingsDirectory;
        return new AppSettings(
            string.Empty,
            string.Empty,
            Path.Combine(dataDir, "Uploads"),
            Path.Combine(dataDir, "BrowserState"),
            Path.Combine(dataDir, "app_log.txt"));
    }

    /// <summary>
    /// 設定の安全な読み込みを試みます。ファイルがない場合や読み込めない場合は null を返します。
    /// </summary>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <param name="configuration">ホスト構成です。</param>
    /// <param name="credentialsFilePath">設定ファイルのパス（省略時は LocalAppData 配下）。</param>
    /// <returns>読み込まれた設定。失敗時は null。</returns>
    public static AppSettings? TryLoad(string appDirectory, IConfiguration? configuration = null, string? credentialsFilePath = null)
    {
        try
        {
            var primaryPath = credentialsFilePath ?? DefaultCredentialsPath;
            if (File.Exists(primaryPath))
            {
                return Parse(File.ReadAllText(primaryPath), Path.GetDirectoryName(primaryPath) ?? DefaultSettingsDirectory);
            }

            var localCredentialsPath = Path.Combine(appDirectory, "credentials.json");
            if (File.Exists(localCredentialsPath))
            {
                return Parse(File.ReadAllText(localCredentialsPath), appDirectory);
            }

            if (configuration != null)
            {
                return FromConfiguration(configuration, appDirectory);
            }
        }
        catch
        {
            // 読み込み失敗時は null を返す
        }

        return null;
    }

    /// <summary>
    /// アカウント情報および監視フォルダを変更した新しい設定インスタンスを返します。
    /// </summary>
    /// <param name="username">ユーザー名です。</param>
    /// <param name="password">パスワードです。</param>
    /// <param name="uploadFolder">監視対象フォルダです。</param>
    /// <returns>更新された新しい設定です。</returns>
    public AppSettings WithValues(string username, string password, string uploadFolder)
    {
        return this with
        {
            Username = username,
            Password = password,
            UploadFolder = uploadFolder
        };
    }

    /// <summary>
    /// 設定ファイルまたは User Secrets から設定を読み込みます。
    /// </summary>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <param name="configuration">ホスト構成です。</param>
    /// <param name="credentialsFilePath">設定ファイルのパス（省略時は LocalAppData 配下）。</param>
    /// <returns>読み込まれた設定です。</returns>
    public static AppSettings Load(string appDirectory, IConfiguration configuration, string? credentialsFilePath = null)
    {
        // 1. 指定パスまたは AppData\Local\InstagramUploader\credentials.json を優先
        var primaryPath = credentialsFilePath ?? DefaultCredentialsPath;
        if (File.Exists(primaryPath))
        {
            return Parse(File.ReadAllText(primaryPath), Path.GetDirectoryName(primaryPath) ?? DefaultSettingsDirectory);
        }

        // 2. アプリ実行ディレクトリの credentials.json を確認
        var localCredentialsPath = Path.Combine(appDirectory, "credentials.json");
        if (File.Exists(localCredentialsPath))
        {
            return Parse(File.ReadAllText(localCredentialsPath), appDirectory);
        }

        return FromConfiguration(configuration, appDirectory);
    }

    /// <summary>
    /// 設定ファイルに設定内容を UTF-8（非 Unicode エスケープ）で保存します。
    /// </summary>
    /// <param name="targetPath">保存先ファイルパス。省略時は LocalAppData 配下。</param>
    public void Save(string? targetPath = null)
    {
        var filePath = targetPath ?? DefaultCredentialsPath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var model = new
        {
            Username,
            Password,
            UploadFolder
        };

        var json = JsonSerializer.Serialize(model, options);
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// JSON 文字列から設定を生成します。
    /// </summary>
    /// <param name="json">設定 JSON です。</param>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <returns>生成された設定です。</returns>
    public static AppSettings Parse(string json, string appDirectory)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var username = GetRequiredString(root, "Username");
        var password = GetRequiredString(root, "Password");

        var uploadFolder = Path.Combine(appDirectory, "Uploads");
        if (root.TryGetProperty("UploadFolder", out var folderProperty) && folderProperty.ValueKind == JsonValueKind.String)
        {
            var configuredFolder = folderProperty.GetString();
            if (!string.IsNullOrWhiteSpace(configuredFolder))
            {
                uploadFolder = configuredFolder;
            }
        }

        return new AppSettings(
            username,
            password,
            uploadFolder,
            Path.Combine(appDirectory, "BrowserState"),
            Path.Combine(appDirectory, "app_log.txt"));
    }

    /// <summary>
    /// 構成オブジェクトから設定を生成します。
    /// </summary>
    /// <param name="configuration">設定値の取得元です。</param>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <returns>生成された設定です。</returns>
    public static AppSettings FromConfiguration(IConfiguration configuration, string appDirectory)
    {
        var username = GetRequiredString(configuration["Username"], "Username", "User Secrets");
        var password = GetRequiredString(configuration["Password"], "Password", "User Secrets");

        var uploadFolder = configuration["UploadFolder"];
        if (string.IsNullOrWhiteSpace(uploadFolder))
        {
            uploadFolder = Path.Combine(appDirectory, "Uploads");
        }

        return new AppSettings(
            username,
            password,
            uploadFolder,
            Path.Combine(appDirectory, "BrowserState"),
            Path.Combine(appDirectory, "app_log.txt"));
    }

    /// <summary>
    /// JSON 要素から必須文字列を取得します。
    /// </summary>
    /// <param name="root">設定 JSON のルート要素です。</param>
    /// <param name="propertyName">取得するプロパティ名です。</param>
    /// <returns>検証済みの文字列です。</returns>
    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{propertyName} が credentials.json に設定されていません。");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{propertyName} が空です。");
        }

        return value;
    }

    /// <summary>
    /// 構成値から必須文字列を取得します。
    /// </summary>
    /// <param name="value">取得した設定値です。</param>
    /// <param name="propertyName">設定項目名です。</param>
    /// <param name="sourceName">設定の取得元名です。</param>
    /// <returns>検証済みの文字列です。</returns>
    private static string GetRequiredString(string? value, string propertyName, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{propertyName} が {sourceName} に設定されていません。");
        }

        return value;
    }
}
