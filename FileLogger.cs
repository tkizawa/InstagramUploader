using System.Diagnostics;
using System.Text;

namespace InstagramUploader;

/// <summary>
/// ファイルへログを書き出すロガーです。
/// </summary>
public sealed class FileLogger : IAppLogger
{
    private readonly object _syncRoot = new();
    private readonly string _logFilePath;

    /// <summary>
    /// <see cref="FileLogger"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="logFilePath">ログファイルの出力先です。</param>
    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    /// <inheritdoc />
    public void Info(string message)
    {
        Write("INFO", message);
    }

    /// <inheritdoc />
    public void Error(string message, Exception? exception = null)
    {
        var detail = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", detail);
    }

    /// <summary>
    /// 指定レベルのログをファイルへ書き込みます。
    /// </summary>
    /// <param name="level">ログレベルです。</param>
    /// <param name="message">ログ本文です。</param>
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
