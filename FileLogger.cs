using System.Diagnostics;
using System.Text;

namespace InstagramUploader;

public sealed class FileLogger : IAppLogger
{
    private readonly object _syncRoot = new();
    private readonly string _logFilePath;

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var detail = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", detail);
    }

    private void Write(string level, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = new StringBuilder()
                .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
                .Append('[').Append(level).Append("] ")
                .Append(message)
                .AppendLine()
                .ToString();

            lock (_syncRoot)
            {
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"Log write failed: {ex}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Log write failed: {ex}");
        }
    }
}
