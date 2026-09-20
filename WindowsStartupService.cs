using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace InstagramUploader;

/// <summary>
/// Windows レジストリ（HKCU\Software\Microsoft\Windows\CurrentVersion\Run）を使用して
/// アプリケーションの自動起動を管理するサービスです。
/// </summary>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultValueName = "InstagramUploader";

    private readonly string _valueName;
    private readonly string _executablePath;
    private readonly IAppLogger? _logger;

    /// <summary>
    /// <see cref="WindowsStartupService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="logger">ロガーです（省略可能）。</param>
    public WindowsStartupService(IAppLogger? logger = null)
        : this(DefaultValueName, GetDefaultExecutablePath(), logger)
    {
    }

    /// <summary>
    /// テストまたは特定の設定用のコンストラクタです。
    /// </summary>
    /// <param name="valueName">レジストリ値の名前です。</param>
    /// <param name="executablePath">登録する実行ファイルのパスです。</param>
    /// <param name="logger">ロガーです。</param>
    public WindowsStartupService(string valueName, string executablePath, IAppLogger? logger = null)
    {
        _valueName = valueName;
        _executablePath = executablePath;
        _logger = logger;
    }

    /// <inheritdoc />
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows 専用機能")]
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false);
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(_valueName) as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // 登録されているパスが現在の実行ファイルを含んでいるか確認
            return value.Contains(_executablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger?.Error($"自動起動設定の確認に失敗しました: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc />
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows 専用機能")]
    public void SetAutoStartEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: true);
            if (key == null)
            {
                _logger?.Error($"レジストリキーを開けませんでした: {RunRegistrySubKey}");
                return;
            }

            if (enabled)
            {
                // パスに空白が含まれる場合を考慮してダブルクォーテーションで囲む
                var command = $"\"{_executablePath}\"";
                key.SetValue(_valueName, command, RegistryValueKind.String);
                _logger?.Info($"自動起動を有効に設定しました: {command}");
            }
            else
            {
                if (key.GetValue(_valueName) != null)
                {
                    key.DeleteValue(_valueName, false);
                    _logger?.Info("自動起動を無効に設定しました。");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"自動起動設定の変更に失敗しました: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 現在のプロセスの実行可能ファイルパスを取得します。
    /// </summary>
    /// <returns>実行可能ファイルのフルパス。</returns>
    private static string GetDefaultExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "InstagramUploader.exe");
    }
}
