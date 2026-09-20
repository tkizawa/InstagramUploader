using System.Drawing;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Forms;

namespace InstagramUploader;

/// <summary>
/// 設定画面のウィンドウ位置、サイズ、状態を保持・永続化するクラスです。
/// 保存先は %LocalAppData%\InstagramUploader\ui_settings.json です。
/// </summary>
public sealed class UiSettings
{
    /// <summary>
    /// ウィンドウの X 座標を取得または設定します。
    /// </summary>
    public int? X { get; set; }

    /// <summary>
    /// ウィンドウの Y 座標を取得または設定します。
    /// </summary>
    public int? Y { get; set; }

    /// <summary>
    /// ウィンドウの幅を取得または設定します。
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// ウィンドウの高さを取得または設定します。
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// ウィンドウが最大化されているかどうかを取得または設定します。
    /// </summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// UI 設定ファイルの既定の保存先パスを取得します。
    /// </summary>
    public static string DefaultFilePath =>
        Path.Combine(AppSettings.DefaultSettingsDirectory, "ui_settings.json");

    /// <summary>
    /// 設定ファイルから UI 設定を読み込みます。
    /// ファイルが存在しない場合や破損している場合は既定値を返します。
    /// </summary>
    /// <param name="filePath">読み込み先ファイルパス。省略時は既定パス。</param>
    /// <returns>読み込まれた UI 設定インスタンス。</returns>
    public static UiSettings Load(string? filePath = null)
    {
        var targetPath = filePath ?? DefaultFilePath;
        if (!File.Exists(targetPath))
        {
            return new UiSettings();
        }

        try
        {
            var json = File.ReadAllText(targetPath);
            return JsonSerializer.Deserialize<UiSettings>(json) ?? new UiSettings();
        }
        catch
        {
            return new UiSettings();
        }
    }

    /// <summary>
    /// UI 設定を JSON ファイルに保存します。日本語文字は UTF-8 でエスケープせずに保存します。
    /// </summary>
    /// <param name="filePath">保存先ファイルパス。省略時は既定パス。</param>
    public void Save(string? filePath = null)
    {
        var targetPath = filePath ?? DefaultFilePath;
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(targetPath, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// 指定されたフォームの現在の位置とサイズをキャプチャします。
    /// </summary>
    /// <param name="form">対象のフォーム。</param>
    public void CaptureFrom(Form form)
    {
        if (form.WindowState == FormWindowState.Minimized)
        {
            return;
        }

        IsMaximized = form.WindowState == FormWindowState.Maximized;

        // 最大化状態の場合は RestoreBounds（復元時の位置とサイズ）を使用
        var bounds = IsMaximized ? form.RestoreBounds : form.Bounds;
        X = bounds.X;
        Y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
    }

    /// <summary>
    /// 保存されている位置とサイズをフォームに適用します。
    /// 画面外にはみ出している場合は、画面内に収まるように補正します。
    /// </summary>
    /// <param name="form">適用先のフォーム。</param>
    public void ApplyTo(Form form)
    {
        if (!Width.HasValue || !Height.HasValue || Width.Value <= 0 || Height.Value <= 0)
        {
            // 保存値が不正または初期状態の場合は FormStartPosition.CenterScreen などの既定値を使用
            form.StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        form.StartPosition = FormStartPosition.Manual;
        form.Size = new Size(
            Math.Max(Width.Value, form.MinimumSize.Width > 0 ? form.MinimumSize.Width : 300),
            Math.Max(Height.Value, form.MinimumSize.Height > 0 ? form.MinimumSize.Height : 200));

        if (X.HasValue && Y.HasValue)
        {
            var targetRect = new Rectangle(X.Value, Y.Value, form.Width, form.Height);

            // 保存された座標がいずれかのスクリーンの作業領域と交差しているか検証
            var intersectsAnyScreen = false;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(targetRect))
                {
                    intersectsAnyScreen = true;
                    break;
                }
            }

            if (intersectsAnyScreen)
            {
                form.Location = new Point(X.Value, Y.Value);
            }
            else
            {
                // ディスプレイ構成の変更等で画面外に出ている場合はプライマリスクリーンの作業領域中央に配置
                var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
                form.Location = new Point(
                    workingArea.Left + Math.Max(0, (workingArea.Width - form.Width) / 2),
                    workingArea.Top + Math.Max(0, (workingArea.Height - form.Height) / 2));
            }
        }
        else
        {
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        if (IsMaximized)
        {
            form.WindowState = FormWindowState.Maximized;
        }
    }
}
