using Microsoft.Win32;

namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="WindowsStartupService"/> の自動起動設定（レジストリ登録・解除）を検証します。
/// </summary>
public sealed class WindowsStartupServiceTests : IDisposable
{
    private const string RunRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string TestValueName = "InstagramUploader_UnitTest";
    private const string TestExecutablePath = @"C:\TestApp\InstagramUploader.exe";

    private readonly WindowsStartupService _service;

    public WindowsStartupServiceTests()
    {
        _service = new WindowsStartupService(TestValueName, TestExecutablePath);
        CleanupRegistry();
    }

    /// <summary>
    /// 未登録状態で IsAutoStartEnabled が false を返すことを検証します。
    /// </summary>
    [Fact]
    public void IsAutoStartEnabled_ReturnsFalse_WhenNotRegistered()
    {
        Assert.False(_service.IsAutoStartEnabled());
    }

    /// <summary>
    /// SetAutoStartEnabled(true) で登録され、SetAutoStartEnabled(false) で解除されることを検証します。
    /// </summary>
    [Fact]
    public void SetAutoStartEnabled_RegistersAndUnregistersCorrectly()
    {
        // 1. 有効化
        _service.SetAutoStartEnabled(true);
        Assert.True(_service.IsAutoStartEnabled());

        using (var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false))
        {
            var value = key?.GetValue(TestValueName) as string;
            Assert.NotNull(value);
            Assert.Contains(TestExecutablePath, value);
        }

        // 2. 無効化
        _service.SetAutoStartEnabled(false);
        Assert.False(_service.IsAutoStartEnabled());

        using (var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false))
        {
            var value = key?.GetValue(TestValueName);
            Assert.Null(value);
        }
    }

    public void Dispose()
    {
        CleanupRegistry();
    }

    private static void CleanupRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: true);
            key?.DeleteValue(TestValueName, false);
        }
        catch
        {
            // テスト後始末の失敗は無視
        }
    }
}
