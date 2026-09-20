using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

/// <summary>
/// アプリケーションの起動とホスト構築を担当します。
/// </summary>
internal static class Program
{
    /// <summary>
    /// アプリケーションを起動し、トレイアプリケーションのメッセージループを開始します。
    /// </summary>
    /// <param name="args">起動引数です。</param>
    [STAThread]
    private static async Task Main(string[] args)
    {
        var appDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var dataDir = AppSettings.DefaultSettingsDirectory;
        try
        {
            Directory.CreateDirectory(dataDir);
        }
        catch
        {
            dataDir = appDir;
        }

        IAppLogger bootstrapLogger = new FileLogger(Path.Combine(dataDir, "app_log.txt"));

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var host = BuildHost(args, appDir);
            await host.StartAsync();

            var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
            var settings = host.Services.GetRequiredService<AppSettings>();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(trayContext.RequestExit);

            // 初回起動時などで設定が未完了の場合は、設定画面を自動表示
            if (!settings.IsConfigured)
            {
                trayContext.OpenSettingsForm();
            }

            Application.Run(trayContext);

            await host.StopAsync();
        }
        catch (Exception ex)
        {
            bootstrapLogger.Error("起動に失敗しました。", ex);
            var errorMessage = string.Format(
                LocalizationResources.StartupErrorMessageFormat,
                Environment.NewLine,
                ex.Message);

            MessageBox.Show(
                errorMessage,
                LocalizationResources.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Generic Host と依存関係注入コンテナーを構築します。
    /// </summary>
    /// <param name="args">起動引数です。</param>
    /// <param name="appDir">実行ファイルの配置ディレクトリです。</param>
    /// <returns>構築済みのホストです。</returns>
    private static IHost BuildHost(string[] args, string appDir)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

        // 設定の安全なロード（設定ファイルが存在しないか不完全な場合はデフォルトを適用）
        builder.Services.AddSingleton(sp =>
            AppSettings.TryLoad(appDir, sp.GetRequiredService<IConfiguration>()) ?? AppSettings.CreateDefault(appDir));

        builder.Services.AddSingleton<IAppLogger>(sp => new FileLogger(sp.GetRequiredService<AppSettings>().LogFilePath));
        builder.Services.AddSingleton<IUserNotifier, WindowsUserNotifier>();
        builder.Services.AddSingleton<ICaptionBuilder, ExifCaptionBuilder>();
        builder.Services.AddSingleton<IFileReadinessChecker, FileReadinessChecker>();
        builder.Services.AddSingleton<IInstagramUploader, PlaywrightInstagramUploader>();
        builder.Services.AddSingleton<IUploadQueueProcessor, UploadQueueProcessor>();
        builder.Services.AddSingleton<IStartupService>(sp =>
            new WindowsStartupService(sp.GetRequiredService<IAppLogger>()));
        builder.Services.AddSingleton<IFolderWatchService>(sp =>
            new FolderWatchService(
                sp.GetRequiredService<AppSettings>().UploadFolder,
                sp.GetRequiredService<IUploadQueueProcessor>().Enqueue,
                sp.GetRequiredService<IAppLogger>()));
        builder.Services.AddSingleton<TrayApplicationContext>();
        builder.Services.AddHostedService<UploadMonitoringHostedService>();

        return builder.Build();
    }
}
