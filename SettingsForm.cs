using System.Drawing;
using System.Windows.Forms;

namespace InstagramUploader;

/// <summary>
/// アカウント情報、監視フォルダ、および自動起動を設定するための画面フォームです。
/// 多言語表示、終了時のウィンドウ位置・サイズの復元、および設定の保存に対応します。
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly UiSettings _uiSettings;
    private readonly Action<AppSettings>? _onSaved;
    private readonly IStartupService _startupService;
    private AppSettings _currentSettings;

    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly CheckBox _chkShowPassword;
    private readonly TextBox _txtUploadFolder;
    private readonly Button _btnBrowseFolder;
    private readonly CheckBox _chkAutoStart;
    private readonly Button _btnSave;
    private readonly Button _btnCancel;

    /// <summary>
    /// 現在適用されている設定を取得します。
    /// </summary>
    public AppSettings CurrentSettings => _currentSettings;

    /// <summary>
    /// <see cref="SettingsForm"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="currentSettings">現在のアプリケーション設定です。</param>
    /// <param name="onSaved">設定保存完了時に呼び出されるコールバックです。</param>
    /// <param name="startupService">自動起動管理サービスです（省略時は既定の WindowsStartupService）。</param>
    public SettingsForm(
        AppSettings currentSettings,
        Action<AppSettings>? onSaved = null,
        IStartupService? startupService = null)
    {
        _currentSettings = currentSettings;
        _onSaved = onSaved;
        _startupService = startupService ?? new WindowsStartupService();
        _uiSettings = UiSettings.Load();

        // フォーム基本プロパティ設定
        Text = LocalizationResources.SettingsTitle;
        MinimumSize = new Size(480, 360);
        Size = new Size(540, 380);
        Font = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = true;
        Icon = LoadFormIcon();

        // メインレイアウトパネル（外枠パディング付き）
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 7,
            AutoSize = false
        };

        // カラム比率: ラベル幅(自動), 入力欄(100%), 参照ボタン(自動)
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // 行設定
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // Username
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // Password
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f)); // Show Password Checkbox
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // Upload Folder
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f)); // Auto Start Checkbox
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f)); // Buttons

        // 1. ユーザー名 (Username)
        var lblUsername = new Label
        {
            Text = LocalizationResources.UsernameLabel,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        _txtUsername = new TextBox
        {
            Text = _currentSettings.Username,
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 0)
        };
        mainLayout.Controls.Add(lblUsername, 0, 0);
        mainLayout.Controls.Add(_txtUsername, 1, 0);
        mainLayout.SetColumnSpan(_txtUsername, 2);

        // 2. パスワード (Password)
        var lblPassword = new Label
        {
            Text = LocalizationResources.PasswordLabel,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        _txtPassword = new TextBox
        {
            Text = _currentSettings.Password,
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            UseSystemPasswordChar = true,
            Margin = new Padding(0, 0, 0, 0)
        };
        mainLayout.Controls.Add(lblPassword, 0, 1);
        mainLayout.Controls.Add(_txtPassword, 1, 1);
        mainLayout.SetColumnSpan(_txtPassword, 2);

        // 3. パスワード表示トグル (Show password)
        _chkShowPassword = new CheckBox
        {
            Text = LocalizationResources.ShowPasswordLabel,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };
        _chkShowPassword.CheckedChanged += (_, _) =>
        {
            _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;
        };
        mainLayout.Controls.Add(_chkShowPassword, 1, 2);
        mainLayout.SetColumnSpan(_chkShowPassword, 2);

        // 4. 監視フォルダ (Upload Folder)
        var lblUploadFolder = new Label
        {
            Text = LocalizationResources.UploadFolderLabel,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        _txtUploadFolder = new TextBox
        {
            Text = _currentSettings.UploadFolder,
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 8, 0)
        };
        _btnBrowseFolder = new Button
        {
            Text = LocalizationResources.BrowseButton,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            Height = 28,
            Padding = new Padding(8, 0, 8, 0)
        };
        _btnBrowseFolder.Click += BrowseFolder_Click;

        mainLayout.Controls.Add(lblUploadFolder, 0, 3);
        mainLayout.Controls.Add(_txtUploadFolder, 1, 3);
        mainLayout.Controls.Add(_btnBrowseFolder, 2, 3);

        // 5. 自動起動チェックボックス (Auto Start with Windows)
        _chkAutoStart = new CheckBox
        {
            Text = LocalizationResources.AutoStartLabel,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Checked = _startupService.IsAutoStartEnabled(),
            Margin = new Padding(0, 4, 0, 0)
        };
        mainLayout.Controls.Add(_chkAutoStart, 1, 4);
        mainLayout.SetColumnSpan(_chkAutoStart, 2);

        // 6. ボタンパネル (Save / Cancel)
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnCancel = new Button
        {
            Text = LocalizationResources.CancelButton,
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 32),
            Margin = new Padding(6, 0, 0, 0)
        };
        _btnCancel.Click += (_, _) => Close();

        _btnSave = new Button
        {
            Text = LocalizationResources.SaveButton,
            Size = new Size(90, 32),
            Margin = new Padding(6, 0, 0, 0)
        };
        _btnSave.Click += Save_Click;

        buttonPanel.Controls.Add(_btnCancel);
        buttonPanel.Controls.Add(_btnSave);

        mainLayout.Controls.Add(buttonPanel, 0, 6);
        mainLayout.SetColumnSpan(buttonPanel, 3);

        Controls.Add(mainLayout);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        // ウィンドウ位置・サイズ復元および保存イベント
        Load += (_, _) => _uiSettings.ApplyTo(this);
        FormClosing += (_, _) =>
        {
            _uiSettings.CaptureFrom(this);
            _uiSettings.Save();
        };
    }

    /// <summary>
    /// フォルダ参照ダイアログを表示し、選択されたパスを入力欄に反映します。
    /// </summary>
    private void BrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = LocalizationResources.FolderBrowserDescription,
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_txtUploadFolder.Text)
                ? _txtUploadFolder.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            _txtUploadFolder.Text = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// 入力値を検証し、設定ファイルおよび自動起動設定へ保存します。
    /// </summary>
    private void Save_Click(object? sender, EventArgs e)
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        var uploadFolder = _txtUploadFolder.Text.Trim();

        // 必須項目バリデーション
        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show(
                this,
                LocalizationResources.ErrorUsernameRequired,
                LocalizationResources.ValidationDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _txtUsername.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                this,
                LocalizationResources.ErrorPasswordRequired,
                LocalizationResources.ValidationDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _txtPassword.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(uploadFolder))
        {
            MessageBox.Show(
                this,
                LocalizationResources.ErrorUploadFolderRequired,
                LocalizationResources.ValidationDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _txtUploadFolder.Focus();
            return;
        }

        // フォルダが存在しない場合は作成
        try
        {
            Directory.CreateDirectory(uploadFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                LocalizationResources.ValidationDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _txtUploadFolder.Focus();
            return;
        }

        // 自動起動設定の反映
        try
        {
            _startupService.SetAutoStartEnabled(_chkAutoStart.Checked);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"自動起動設定の更新に失敗しました: {ex.Message}",
                LocalizationResources.ValidationDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // 設定の更新と保存（UTF-8、非Unicodeエスケープ）
        _currentSettings = _currentSettings.WithValues(username, password, uploadFolder);
        _currentSettings.Save();

        _onSaved?.Invoke(_currentSettings);

        MessageBox.Show(
            this,
            LocalizationResources.SettingsSavedSuccess,
            LocalizationResources.InfoDialogTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// アイコンを安全に読み込みます。
    /// </summary>
    /// <returns>読み込まれたアイコン。失敗時は null。</returns>
    private static Icon? LoadFormIcon()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                var extracted = Icon.ExtractAssociatedIcon(exePath);
                if (extracted != null)
                {
                    return extracted;
                }
            }

            var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // アイコン読み込み失敗時はフォーム既定アイコンを使用
        }

        return null;
    }
}
