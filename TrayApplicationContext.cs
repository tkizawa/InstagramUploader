using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly Control _dispatcher;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IAppLogger _logger;
    private readonly NotifyIcon _notifyIcon;
    private bool _exitRequested;

    public TrayApplicationContext(IHostApplicationLifetime applicationLifetime, IAppLogger logger)
    {
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _dispatcher = new Control();
        _dispatcher.CreateControl();

        var contextMenu = new ContextMenuStrip();
        var exitItem = new ToolStripMenuItem("監視を終了する");
        exitItem.Click += (_, _) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "Instagram Uploader 監視中",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.ShowBalloonTip(
            3000,
            "Instagram Uploader",
            "フォルダの監視を開始しました。終了する場合はタスクトレイのアイコンを右クリックしてください。",
            ToolTipIcon.Info);
    }

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

    protected override void ExitThreadCore()
    {
        _dispatcher.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }

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
}
