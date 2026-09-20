using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

/// <summary>
/// タスクトレイ UI、設定画面、終了要求の仲介を行うアプリケーションコンテキストです。
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly Control _dispatcher;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IAppLogger _logger;
    private readonly IFolderWatchService _folderWatchService;
    private readonly IStartupService _startupService;
    private readonly NotifyIcon _notifyIcon;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private bool _exitRequested;

    /// <summary>
    /// <see cref="TrayApplicationContext"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="settings">アプリケーション設定です。</param>
    /// <param name="folderWatchService">フォルダ監視サービスです。</param>
    /// <param name="startupService">自動起動管理サービスです。</param>
    /// <param name="applicationLifetime">ホストのライフサイクルです。</param>
    /// <param name="logger">ロガーです。</param>
    public TrayApplicationContext(
        AppSettings settings,
        IFolderWatchService folderWatchService,
        IStartupService startupService,
        IHostApplicationLifetime applicationLifetime,
        IAppLogger logger)
    {
        _settings = settings;
        _folderWatchService = folderWatchService;
        _startupService = startupService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _dispatcher = new Control();
        _dispatcher.CreateControl();

        var contextMenu = new ContextMenuStrip();

        // 1. 設定メニュー
        var settingsItem = new ToolStripMenuItem(LocalizationResources.TrayMenuSettings);
        settingsItem.Click += (_, _) => OpenSettingsForm();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 2. 終了メニュー
        var exitItem = new ToolStripMenuItem(LocalizationResources.TrayMenuExit);
        exitItem.Click += (_, _) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        var appIcon = LoadApplicationIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = appIcon,
            Text = _settings.IsConfigured
                ? LocalizationResources.TrayTooltipWatching
                : LocalizationResources.TrayTooltipNotConfigured,
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        // ダブルクリックで設定画面を表示
        _notifyIcon.DoubleClick += (_, _) => OpenSettingsForm();

        // 監視開始通知バルーン表示（設定済みの場合のみ）
        if (_settings.IsConfigured)
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                LocalizationResources.AppTitle,
                LocalizationResources.TrayBalloonStarted,
                ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// 設定画面を表示します。既に開いている場合は前面にアクティブ化します。
    /// </summary>
    public void OpenSettingsForm()
    {
        if (_dispatcher.InvokeRequired)
        {
            _dispatcher.BeginInvoke(new MethodInvoker(OpenSettingsForm));
            return;
        }

        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }
            _settingsForm.Activate();
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new SettingsForm(_settings, OnSettingsSaved, _startupService);
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
    }

    /// <summary>
    /// 設定画面で設定が保存された際のコールバックです。
    /// </summary>
    /// <param name="newSettings">新しく保存された設定です。</param>
    private void OnSettingsSaved(AppSettings newSettings)
    {
        _logger.Info("設定画面から新しい設定が保存されました。");
        var previousFolder = _settings.UploadFolder;
        _settings = newSettings;

        _notifyIcon.Text = _settings.IsConfigured
            ? LocalizationResources.TrayTooltipWatching
            : LocalizationResources.TrayTooltipNotConfigured;

        if (!string.Equals(previousFolder, newSettings.UploadFolder, StringComparison.OrdinalIgnoreCase))
        {
            _folderWatchService.UpdateWatchFolder(newSettings.UploadFolder);
        }
    }

    /// <summary>
    /// ホスト停止に合わせて UI スレッドへ終了要求を転送します。
    /// </summary>
    public void RequestExit()
    {
        if (_exitRequested)
        {
            return;
        }

        _exitRequested = true;

        if (_dispatcher.IsHandleCreated)
        {
            _dispatcher.BeginInvoke(new MethodInvoker(ExitThread));
            return;
        }

        ExitThread();
    }

    /// <summary>
    /// トレイ資源および設定画面を破棄しながらメッセージループを終了します。
    /// </summary>
    protected override void ExitThreadCore()
    {
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Close();
            _settingsForm.Dispose();
            _settingsForm = null;
        }

        _dispatcher.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }

    /// <summary>
    /// ユーザー操作によるアプリケーション終了を開始します。
    /// </summary>
    private void ExitApplication()
    {
        if (_exitRequested)
        {
            return;
        }

        _exitRequested = true;
        _logger.Info("終了メニューが選択されました。");
        _applicationLifetime.StopApplication();
        ExitThread();
    }

    /// <summary>
    /// 実行ファイルまたはアセットからアプリケーションアイコンを読み込みます。
    /// </summary>
    /// <returns>読み込まれたアイコン。失敗時は情報システムアイコン。</returns>
    public static Icon LoadApplicationIcon()
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
            // アイコン読み込み失敗時は既定アイコンにフォールバック
        }

        return SystemIcons.Information;
    }
}
