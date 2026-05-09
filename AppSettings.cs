using System.Text.Json;

namespace InstagramUploader;

public sealed record AppSettings(
    string Username,
    string Password,
    string UploadFolder,
    string BrowserStateDirectory,
    string LogFilePath)
{
    public static AppSettings Load(string appDirectory)
    {
        var credentialsPath = Path.Combine(appDirectory, "credentials.json");
        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException("credentials.json が見つかりません。", credentialsPath);
        }

        return Parse(File.ReadAllText(credentialsPath), appDirectory);
    }

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
}
