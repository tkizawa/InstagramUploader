using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InstagramUploader;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        var appDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        IAppLogger bootstrapLogger = new FileLogger(Path.Combine(appDir, "app_log.txt"));

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var host = BuildHost(args, appDir);
            await host.StartAsync();

            var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(trayContext.RequestExit);
            Application.Run(trayContext);

            await host.StopAsync();
        }
        catch (Exception ex)
        {
            bootstrapLogger.Error("起動に失敗しました。", ex);
            MessageBox.Show(
                $"起動に失敗しました。{Environment.NewLine}{ex.Message}{Environment.NewLine}詳細は app_log.txt を確認してください。",
                "Instagram Uploader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static IHost BuildHost(string[] args, string appDir)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

        builder.Services.AddSingleton(sp => AppSettings.Load(appDir, sp.GetRequiredService<IConfiguration>()));
        builder.Services.AddSingleton<IAppLogger>(sp => new FileLogger(sp.GetRequiredService<AppSettings>().LogFilePath));
        builder.Services.AddSingleton<IUserNotifier, WindowsUserNotifier>();
        builder.Services.AddSingleton<ICaptionBuilder, ExifCaptionBuilder>();
        builder.Services.AddSingleton<IFileReadinessChecker, FileReadinessChecker>();
        builder.Services.AddSingleton<IInstagramUploader, PlaywrightInstagramUploader>();
        builder.Services.AddSingleton<IUploadQueueProcessor, UploadQueueProcessor>();
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
