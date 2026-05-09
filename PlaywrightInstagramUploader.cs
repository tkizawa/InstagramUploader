using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace InstagramUploader;

/// <summary>
/// Playwright を使って Instagram のブラウザー操作を行うアップローダーです。
/// </summary>
public sealed class PlaywrightInstagramUploader : IInstagramUploader
{
    private static readonly string[] CreateButtonSelectors =
    {
        "[aria-label='新規投稿を作成']",
        "a[href='#']:has-text('作成')"
    };

    private static readonly string[] FacebookLoginSelectors =
    {
        "button:has-text('Facebookでログイン')",
        "a:has-text('Facebookでログイン')",
        "span:has-text('Facebookでログイン')"
    };

    private static readonly string[] NotNowSelectors =
    {
        "button:has-text('後で')"
    };

    private static readonly string[] SelectFromComputerSelectors =
    {
        "button:has-text('コンピューターから選択')"
    };

    private static readonly string[] NextSelectors =
    {
        "text=次へ"
    };

    private static readonly string[] CaptionSelectors =
    {
        "div[aria-label='キャプションを入力…']",
        "div[aria-label='キャプションを入力...']"
    };

    private static readonly string[] ShareSelectors =
    {
        "div[role='dialog'] >> text=シェア"
    };

    private static readonly string[] CompletionSelectors =
    {
        "div[role='dialog'] >> text=完了",
        "text=投稿をシェアしました"
    };

    private readonly AppSettings _settings;
    private readonly IAppLogger _logger;
    private readonly IUserNotifier _notifier;

    /// <summary>
    /// <see cref="PlaywrightInstagramUploader"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="settings">アプリケーション設定です。</param>
    /// <param name="logger">ロガーです。</param>
    /// <param name="notifier">ユーザー通知手段です。</param>
    public PlaywrightInstagramUploader(AppSettings settings, IAppLogger logger, IUserNotifier notifier)
    {
        _settings = settings;
        _logger = logger;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var context = await playwright.Chromium.LaunchPersistentContextAsync(_settings.BrowserStateDirectory, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = new ViewportSize { Width = 1200, Height = 800 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36"
            });

            var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
            page.SetDefaultTimeout(300000);

            await page.GotoAsync("https://www.instagram.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (!await IsLoggedInAsync(page))
            {
                _notifier.ShowInfo(
                    "Instagram Uploader",
                    "ログインが必要です。初回のみ Facebook / Instagram 側で認証画面が表示された場合は、ブラウザ上で手動で突破してください。");

                await LoginWithFacebookAsync(page);
            }

            await DismissOptionalDialogsAsync(page);
            await OpenCreatePostDialogAsync(page);

            var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =>
            {
                await ClickFirstAvailableAsync(page, SelectFromComputerSelectors, required: true);
            });

            await fileChooser.SetFilesAsync(filePath);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            await ClickFirstAvailableAsync(page, NextSelectors, required: true);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            await ClickFirstAvailableAsync(page, NextSelectors, required: true);

            var captionBox = await FindFirstAvailableAsync(page, CaptionSelectors, 10000);
            if (captionBox is null)
            {
                return UploadResult.Failure("キャプション入力欄を検出できませんでした。");
            }

            await captionBox.FillAsync(caption);
            await ClickFirstAvailableAsync(page, ShareSelectors, required: true);

            var completion = await FindFirstAvailableAsync(page, CompletionSelectors, 60000);
            if (completion is null)
            {
                return UploadResult.Failure("投稿完了を確認できませんでした。");
            }

            try
            {
                await completion.ClickAsync();
            }
            catch (PlaywrightException)
            {
                // 完了メッセージのようにクリックできない要素もあるため、検出できれば成功とみなす。
            }

            return UploadResult.Success("投稿が完了しました。");
        }
        catch (TimeoutException ex)
        {
            return UploadResult.Failure($"Instagram 操作がタイムアウトしました。{ex.Message}");
        }
        catch (PlaywrightException ex)
        {
            _logger.Error("Instagram 操作に失敗しました。", ex);
            return UploadResult.Failure($"Instagram 操作に失敗しました。{ex.Message}");
        }
    }

    /// <summary>
    /// 現在のページがログイン済み状態かを判定します。
    /// </summary>
    /// <param name="page">対象ページです。</param>
    /// <returns>ログイン済みなら <see langword="true"/> です。</returns>
    private async Task<bool> IsLoggedInAsync(IPage page)
    {
        var createButton = await FindFirstAvailableAsync(page, CreateButtonSelectors, 5000);
        return createButton is not null;
    }

    /// <summary>
    /// Facebook ログイン経由で Instagram のセッションを確立します。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    private async Task LoginWithFacebookAsync(IPage page)
    {
        await ClickFirstAvailableAsync(page, FacebookLoginSelectors, required: true);
        await page.WaitForURLAsync(new Regex(".*facebook.com.*", RegexOptions.IgnoreCase));

        var emailInput = page.Locator("input[id='email'], input[name='email']").First;
        await emailInput.WaitForAsync();
        await emailInput.FillAsync(_settings.Username);

        var passwordInput = page.Locator("input[id='pass'], input[name='pass']").First;
        await passwordInput.WaitForAsync();
        await passwordInput.FillAsync(_settings.Password);
        await passwordInput.PressAsync("Enter");

        var createButton = await FindFirstAvailableAsync(page, CreateButtonSelectors, 300000);
        if (createButton is null)
        {
            throw new TimeoutException("ログイン後に Instagram の投稿画面へ戻れませんでした。");
        }
    }

    /// <summary>
    /// 任意表示のダイアログを閉じます。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    private async Task DismissOptionalDialogsAsync(IPage page)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var button = await FindFirstAvailableAsync(page, NotNowSelectors, 3000);
            if (button is null)
            {
                return;
            }

            await button.ClickAsync();
        }
    }

    /// <summary>
    /// 新規投稿ダイアログを開きます。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    private async Task OpenCreatePostDialogAsync(IPage page)
    {
        await ClickFirstAvailableAsync(page, CreateButtonSelectors, required: true);
    }

    /// <summary>
    /// 候補セレクターのうち最初に利用可能な要素をクリックします。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    /// <param name="selectors">候補セレクターです。</param>
    /// <param name="required">必須要素かどうかです。</param>
    private static async Task ClickFirstAvailableAsync(IPage page, IEnumerable<string> selectors, bool required)
    {
        var locator = await FindFirstAvailableAsync(page, selectors, required ? 10000 : 3000);
        if (locator is null)
        {
            throw new TimeoutException($"対象の UI 要素を検出できませんでした。候補: {string.Join(", ", selectors)}");
        }

        await locator.ClickAsync();
    }

    /// <summary>
    /// 候補セレクターのうち最初に利用可能な要素を返します。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    /// <param name="selectors">候補セレクターです。</param>
    /// <param name="timeoutMilliseconds">各候補に対する待機時間です。</param>
    /// <returns>見つかったロケーター、見つからない場合は <see langword="null"/> です。</returns>
    private static async Task<ILocator?> FindFirstAvailableAsync(IPage page, IEnumerable<string> selectors, float timeoutMilliseconds)
    {
        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;

            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    Timeout = timeoutMilliseconds
                });
                return locator;
            }
            catch (TimeoutException)
            {
            }
            catch (PlaywrightException)
            {
            }
        }

        return null;
    }
}
