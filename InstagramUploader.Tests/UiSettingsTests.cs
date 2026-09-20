using System.Drawing;
using System.Windows.Forms;

namespace InstagramUploader.Tests;

/// <summary>
/// <see cref="UiSettings"/> の設定保存・読み込みおよびフォームへの適用を検証します。
/// </summary>
public sealed class UiSettingsTests
{
    /// <summary>
    /// ファイルが存在しない場合に既定のインスタンスが返されることを検証します。
    /// </summary>
    [Fact]
    public void Load_ReturnsDefault_WhenFileDoesNotExist()
    {
        using var tempDir = new TemporaryDirectory();
        var filePath = Path.Combine(tempDir.Path, "non_existent.json");

        var settings = UiSettings.Load(filePath);

        Assert.NotNull(settings);
        Assert.Null(settings.X);
        Assert.Null(settings.Y);
        Assert.Null(settings.Width);
        Assert.Null(settings.Height);
        Assert.False(settings.IsMaximized);
    }

    /// <summary>
    /// 保存した値が正しく読み込まれることを検証します。
    /// </summary>
    [Fact]
    public void SaveAndLoad_PreservesValues()
    {
        using var tempDir = new TemporaryDirectory();
        var filePath = Path.Combine(tempDir.Path, "ui_settings.json");

        var original = new UiSettings
        {
            X = 150,
            Y = 200,
            Width = 600,
            Height = 450,
            IsMaximized = true
        };

        original.Save(filePath);

        var loaded = UiSettings.Load(filePath);

        Assert.NotNull(loaded);
        Assert.Equal(150, loaded.X);
        Assert.Equal(200, loaded.Y);
        Assert.Equal(600, loaded.Width);
        Assert.Equal(450, loaded.Height);
        Assert.True(loaded.IsMaximized);
    }

    /// <summary>
    /// CaptureFrom で Form のサイズと通常時 Bounds が正しく取得できることを検証します。
    /// </summary>
    [Fact]
    public void CaptureFrom_CapturesFormBounds()
    {
        using var form = new Form
        {
            Size = new Size(520, 380),
            Location = new Point(100, 120),
            WindowState = FormWindowState.Normal
        };

        var settings = new UiSettings();
        settings.CaptureFrom(form);

        Assert.Equal(520, settings.Width);
        Assert.Equal(380, settings.Height);
        Assert.False(settings.IsMaximized);
    }
}
