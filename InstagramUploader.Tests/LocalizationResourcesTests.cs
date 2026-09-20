using System.Globalization;

namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="LocalizationResources"/> の多言語（日本語・英語）切り替え動作を検証します。
/// </summary>
public sealed class LocalizationResourcesTests
{
    /// <summary>
    /// 日本語カルチャの場合に日本語の文字列が返されることを検証します。
    /// </summary>
    [Fact]
    public void Resources_ReturnJapanese_WhenCultureIsJapanese()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");

            Assert.True(LocalizationResources.IsJapanese);
            Assert.Equal("Instagram Uploader - 設定", LocalizationResources.SettingsTitle);
            Assert.Equal("保存", LocalizationResources.SaveButton);
            Assert.Equal("キャンセル", LocalizationResources.CancelButton);
            Assert.Equal("設定(&S)...", LocalizationResources.TrayMenuSettings);
            Assert.Equal("監視を終了する(&X)", LocalizationResources.TrayMenuExit);
            Assert.Equal("Windows 起動時に自動起動する", LocalizationResources.AutoStartLabel);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    /// 英語カルチャの場合に英語の文字列が返されることを検証します。
    /// </summary>
    [Fact]
    public void Resources_ReturnEnglish_WhenCultureIsEnglish()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            Assert.False(LocalizationResources.IsJapanese);
            Assert.Equal("Instagram Uploader - Settings", LocalizationResources.SettingsTitle);
            Assert.Equal("Save", LocalizationResources.SaveButton);
            Assert.Equal("Cancel", LocalizationResources.CancelButton);
            Assert.Equal("&Settings...", LocalizationResources.TrayMenuSettings);
            Assert.Equal("E&xit", LocalizationResources.TrayMenuExit);
            Assert.Equal("Start automatically when Windows starts", LocalizationResources.AutoStartLabel);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
