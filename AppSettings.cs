using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
    /// 設定ファイルまたは User Secrets から設定を読み込みます。
    /// </summary>
    /// <param name="appDirectory">実行ディレクトリです。</param>
    /// <param name="configuration">ホスト構成です。</param>
    /// <returns>読み込まれた設定です。</returns>
    public static AppSettings Load(string appDirectory, IConfiguration configuration)
    {
        var credentialsPath = Path.Combine(appDirectory, "credentials.json");
        if (File.Exists(credentialsPath))
        {
            return Parse(File.ReadAllText(credentialsPath), appDirectory);
        }

        return FromConfiguration(configuration, appDirectory);
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
