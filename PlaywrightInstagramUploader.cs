using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace InstagramUploader;

/// <summary>
/// Playwright を使って Instagram のブラウザー操作を行うアップローダーです。
/// </summary>
/// <remarks>
/// <see cref="PlaywrightInstagramUploader"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="settings">アプリケーション設定です。</param>
/// <param name="logger">ロガーです。</param>
/// <param name="notifier">ユーザー通知手段です。</param>
public sealed class PlaywrightInstagramUploader(AppSettings settings, IAppLogger logger, IUserNotifier notifier) : IInstagramUploader
{
    private static readonly string[] CreateButtonSelectors =
    {
        "div[role='button']:has(svg[aria-label*='新規投稿'])",
        "div[role='button']:has(svg[aria-label*='作成'])",
        "div[role='button']:has(svg[aria-label*='New post'])",
        "div[role='button']:has(svg[aria-label*='Create'])",
        "a:has(svg[aria-label*='新規投稿'])",
        "a:has(svg[aria-label*='作成'])",
        "a:has(svg[aria-label*='New post'])",
        "a:has(svg[aria-label*='Create'])",
        "svg[aria-label*='新規投稿']",
        "svg[aria-label*='作成']",
        "svg[aria-label*='New post']",
        "svg[aria-label*='Create']",
        "[aria-label*='新規投稿']",
        "[aria-label*='作成']",
        "[aria-label*='New post']",
        "[aria-label*='Create']",
        "div[role='button']:has-text('作成')",
        "div[role='button']:has-text('Create')",
        "a[href='#']:has-text('作成')",
        "a[href='#']:has-text('Create')",
        "span:text-is('作成')",
        "span:text-is('Create')"
    };

    private static readonly string[] FacebookLoginSelectors =
    {
        "button:has-text('Facebookでログイン')",
        "a:has-text('Facebookでログイン')",
        "span:has-text('Facebookでログイン')",
        "button:has-text('Log in with Facebook')",
        "a:has-text('Log in with Facebook')",
        "span:has-text('Log in with Facebook')"
    };

    private static readonly string[] NotNowSelectors =
    {
        "button:has-text('後で')",
        "button:has-text('Not Now')",
        "button:has-text('Not now')"
    };

    private static readonly string[] SelectFromComputerSelectors =
    {
        "button:has-text('コンピューターから選択')",
        "button:has-text('コンピュータから選択')",
        "button:has-text('ファイルを選択')",
        "button:has-text('Select from computer')",
        "button:has-text('Select From Computer')"
    };

    private static readonly string[] NextSelectors =
    {
        "div[role='dialog'] div[role='button']:has-text('次へ')",
        "div[role='dialog'] button:has-text('次へ')",
        "div[role='dialog'] div:text-is('次へ')",
        "div[role='dialog'] span:text-is('次へ')",
        "div[role='dialog'] div[role='button']:has-text('Next')",
        "div[role='dialog'] button:has-text('Next')",
        "div[role='dialog'] div:text-is('Next')",
        "div[role='dialog'] span:text-is('Next')",
        "div[role='dialog'] [tabindex='0']:has-text('次へ')",
        "div[role='dialog'] [tabindex='0']:has-text('Next')",
        "button:has-text('次へ')",
        "div[role='button']:has-text('次へ')",
        "span:text-is('次へ')",
        "button:has-text('Next')",
        "div[role='button']:has-text('Next')",
        "span:text-is('Next')"
    };

    private static readonly string[] CaptionSelectors =
    {
        // ダイアログ内の編集可能テキストエリア（Lexical / Draft.js / contenteditable 等）
        "div[role='dialog'] div[role='textbox'][contenteditable='true']",
        "div[role='dialog'] div[contenteditable='true']",
        "div[role='dialog'] div[role='textbox']",
        "div[role='dialog'] div[data-lexical-editor='true']",
        "div[role='dialog'] div[aria-label*='キャプション']",
        "div[role='dialog'] div[aria-label*='caption' i]",
        "div[role='dialog'] textarea",
        "div[aria-label*='キャプションを追加']",
        "div[aria-label*='キャプションを入力']",
        "div[aria-label*='キャプション']",
        "div[aria-label*='caption' i]",
        "div[aria-label='キャプションを追加...']",
        "div[aria-label='キャプションを入力…']",
        "div[aria-label='キャプションを入力...']",
        "div[aria-label='Write a caption...']",
        "div[aria-label='Write a caption…']",
        "div[contenteditable='true']",
        "div[role='textbox']"
    };

    private static readonly string[] ShareSelectors =
    {
        // 完全一致（text-is）で「シェア」「Share」を指定。「シェア先」への誤爆を防止
        "div[role='dialog'] div[role='button']:text-is('シェア')",
        "div[role='dialog'] button:text-is('シェア')",
        "div[role='dialog'] div:text-is('シェア')",
        "div[role='dialog'] span:text-is('シェア')",
        "div[role='dialog'] [tabindex='0']:text-is('シェア')",
        "div[role='dialog'] div[role='button']:text-is('Share')",
        "div[role='dialog'] button:text-is('Share')",
        "div[role='dialog'] div:text-is('Share')",
        "div[role='dialog'] span:text-is('Share')",
        "div[role='dialog'] [tabindex='0']:text-is('Share')",
        "div[role='button']:text-is('シェア')",
        "button:text-is('シェア')",
        "span:text-is('シェア')",
        "div[role='button']:text-is('Share')",
        "button:text-is('Share')",
        "span:text-is('Share')"
    };

    private static readonly string[] CompletionSelectors =
    {
        "div[role='dialog'] :text('投稿をシェアしました')",
        "div[role='dialog'] :text('シェアしました')",
        "div[role='dialog'] :text('投稿がシェアされました')",
        "div[role='dialog'] :text('Your post has been shared')",
        "div[role='dialog'] :text('Post shared')",
        "div[role='dialog'] :text('完了')",
        "div[role='dialog'] :text('Done')",
        "span:has-text('投稿をシェアしました')",
        "span:has-text('シェアしました')",
        "span:has-text('投稿がシェアされました')",
        "span:has-text('Your post has been shared')",
        "span:has-text('Post shared')",
        "span:has-text('完了')",
        "span:has-text('Done')",
        "img[alt*='Animated checkmark']",
        "img[alt*='チェックマーク']",
        "svg[aria-label*='完了']",
        "svg[aria-label*='Done']"
    };

    private readonly AppSettings _settings = settings;
    private readonly IAppLogger _logger = logger;
    private readonly IUserNotifier _notifier = notifier;

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(string filePath, string caption, CancellationToken cancellationToken = default)
    {
        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Info($"Instagram アップロードを開始します: {filePath}");

            using var playwright = await Playwright.CreateAsync();
            context = await playwright.Chromium.LaunchPersistentContextAsync(_settings.BrowserStateDirectory, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = new ViewportSize { Width = 1200, Height = 800 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36"
            });

            page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
            page.SetDefaultTimeout(300000);

            cancellationToken.ThrowIfCancellationRequested();
            _logger.Info("Instagram を開いています。");
            await page.GotoAsync("https://www.instagram.com/");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            _logger.Info("Instagram の初期画面を読み込みました。");

            var isLoggedIn = await IsLoggedInAsync(page);
            _logger.Info(isLoggedIn ? "ログイン済みセッションを検出しました。" : "ログインが必要です。Facebook ログインを開始します。");

            if (!isLoggedIn)
            {
                _notifier.ShowInfo(
                    "Instagram Uploader",
                    "ログインが必要です。初回のみ Facebook / Instagram 側で認証画面が表示された場合は、ブラウザ上で手動で突破してください。");

                await LoginWithFacebookAsync(page);
                _logger.Info("Facebook ログイン後の Instagram 画面を確認しました。");
            }

            await DismissOptionalDialogsAsync(page);
            _logger.Info("投稿ダイアログを開きます。");
            await OpenCreatePostDialogAsync(page);

            var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =>
            {
                await ClickFirstAvailableAsync(page, SelectFromComputerSelectors, required: true);
            });

            _logger.Info("画像ファイルを選択します。");
            await fileChooser.SetFilesAsync(filePath);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            _logger.Info("1回目の「次へ」（フィルター画面へ）をクリックします。");
            await ClickFirstAvailableAsync(page, NextSelectors, required: true, timeoutMilliseconds: 20000);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            _logger.Info("2回目の「次へ」（キャプション・シェア画面へ）をクリックします。");
            await ClickFirstAvailableAsync(page, NextSelectors, required: true, timeoutMilliseconds: 20000);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            // シェア画面への到達を確認（「シェア」ボタンまたはキャプション欄の表示を待機）
            _logger.Info("シェア画面の読み込みを待機しています。");
            var shareButton = await FindFirstAvailableAsync(page, ShareSelectors, 15000);

            // キャプション入力処理
            if (!string.IsNullOrWhiteSpace(caption))
            {
                var captionBox = await FindFirstAvailableAsync(page, CaptionSelectors, 10000);
                if (captionBox is not null)
                {
                    _logger.Info("キャプションを入力します。");
                    await captionBox.ClickAsync();
                    await Task.Delay(300, cancellationToken);
                    try
                    {
                        await captionBox.FillAsync(caption);
                    }
                    catch
                    {
                        // contenteditable / Lexical 等で FillAsync が効かない場合はキー入力で挿入
                        await page.Keyboard.InsertTextAsync(caption);
                    }
                    await Task.Delay(500, cancellationToken);
                }
                else
                {
                    _logger.Info("キャプション入力欄を検出できませんでしたが、処理を継続します。");
                }
            }
            else
            {
                _logger.Info("キャプションは指定されていないため、入力をスキップします。");
            }

            _logger.Info("「シェア」ボタンをクリックします。");
            await ClickFirstAvailableAsync(page, ShareSelectors, required: true, timeoutMilliseconds: 15000);

            // クリック後、2秒待機して画面遷移（投稿処理開始）を確認
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var remainingShareButton = await FindFirstAvailableAsync(page, ShareSelectors, 1000);
            if (remainingShareButton is not null && await remainingShareButton.IsVisibleAsync())
            {
                _logger.Info("シェアボタンが残っているため、強制クリックで再試行します。");
                try
                {
                    await remainingShareButton.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 3000 });
                }
                catch
                {
                }
            }

            _logger.Info("投稿完了を待機しています。");

            // 投稿完了（完了メッセージまたはダイアログ終了）を待機
            var completion = await FindFirstAvailableAsync(page, CompletionSelectors, 60000);
            if (completion is null)
            {
                // ダイアログが自動的に閉じて投稿完了しているか確認
                var dialogCount = await page.Locator("div[role='dialog']").CountAsync();
                if (dialogCount == 0)
                {
                    _logger.Info("投稿ダイアログが閉じたことを確認しました。");
                }
                else
                {
                    // アップロード通信に時間がかかっている可能性を考慮し、追加で30秒待機
                    completion = await FindFirstAvailableAsync(page, CompletionSelectors, 30000);
                    dialogCount = await page.Locator("div[role='dialog']").CountAsync();
                    if (completion is null && dialogCount > 0)
                    {
                        await SaveErrorScreenshotAsync(page);
                        return UploadResult.Failure("投稿完了を確認できませんでした。");
                    }
                }
            }

            if (completion is not null)
            {
                try
                {
                    await completion.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                }
                catch (PlaywrightException)
                {
                    // 完了メッセージのようにクリックできない要素もあるため、検出できれば成功とみなす。
                }
            }

            // ダイアログがまだ残っている場合は閉じるボタンを試行
            try
            {
                var closeButton = page.Locator("div[role='dialog'] [aria-label='閉じる'], div[role='dialog'] [aria-label='Close'], div[role='dialog'] svg[aria-label='閉じる'], div[role='dialog'] svg[aria-label='Close']").First;
                if (await closeButton.IsVisibleAsync())
                {
                    await closeButton.ClickAsync();
                }
            }
            catch
            {
                // 閉じる操作の失敗は問題ないため無視
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            _logger.Info("Instagram への投稿が完了しました。");
            return UploadResult.Success("投稿が完了しました。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PlaywrightException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("アプリケーションの終了によりアップロードを中止しました。", ex, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            if (page is not null)
            {
                await SaveErrorScreenshotAsync(page);
            }
            return UploadResult.Failure($"Instagram 操作がタイムアウトしました。{ex.Message}");
        }
        catch (PlaywrightException ex)
        {
            if (page is not null)
            {
                await SaveErrorScreenshotAsync(page);
            }
            _logger.Error("Instagram 操作に失敗しました。", ex);
            return UploadResult.Failure($"Instagram 操作に失敗しました。{ex.Message}");
        }
        finally
        {
            if (context is not null)
            {
                try
                {
                    await context.DisposeAsync();
                }
                catch
                {
                    // 切断時の例外は無視
                }
            }
        }
    }

    /// <summary>
    /// エラー調査用に現在のページスクリーンショットを保存します。
    /// </summary>
    /// <param name="page">対象ページです。</param>
    private async Task SaveErrorScreenshotAsync(IPage page)
    {
        try
        {
            if (page.IsClosed)
            {
                return;
            }

            var logDir = Path.GetDirectoryName(_settings.LogFilePath) ?? _settings.BrowserStateDirectory;
            var screenshotPath = Path.Combine(logDir, "last_upload_error.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            _logger.Info($"エラー時の画面キャプチャを保存しました: {screenshotPath}");
        }
        catch (PlaywrightException)
        {
            // ページが既に閉じられている場合などは無視
        }
        catch (Exception ex)
        {
            _logger.Error("エラー画面キャプチャの保存に失敗しました。", ex);
        }
    }

    /// <summary>
    /// 現在のページがログイン済み状態かを判定します。
    /// </summary>
    /// <param name="page">対象ページです。</param>
    /// <returns>ログイン済みなら <see langword="true"/> です。</returns>
    private async Task<bool> IsLoggedInAsync(IPage page)
    {
        // 1. 保存済み Cookie からセッション情報を確認
        try
        {
            var cookies = await page.Context.CookiesAsync("https://www.instagram.com");
            var hasSession = cookies.Any(c =>
                (c.Name.Equals("sessionid", StringComparison.OrdinalIgnoreCase) ||
                 c.Name.Equals("ds_user_id", StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrWhiteSpace(c.Value));

            if (hasSession)
            {
                _logger.Info("Cookie からログインセッション（sessionid / ds_user_id）を検出しました。");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Info($"Cookie 確認で例外が発生しました（DOM判定を継続）: {ex.Message}");
        }

        // 2. ログイン入力フォームの表示確認（表示されていれば未ログイン）
        try
        {
            var loginInput = page.Locator("input[name='username'], input[name='password']").First;
            if (await loginInput.IsVisibleAsync())
            {
                _logger.Info("ログイン入力フォームが検出されたため、未ログイン状態と判定しました。");
                return false;
            }
        }
        catch
        {
        }

        // 3. 作成ボタンやナビゲーション要素の存在確認
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
        // 作成ボタンクリック後、ダイアログ表示を少し待機（出ない場合は再試行）
        var selectButton = await FindFirstAvailableAsync(page, SelectFromComputerSelectors, 3000);
        if (selectButton is null)
        {
            _logger.Info("投稿ダイアログが未展開の可能性があるため、作成ボタンを再クリックします。");
            var retryButton = await FindFirstAvailableAsync(page, CreateButtonSelectors, 3000);
            if (retryButton is not null)
            {
                await retryButton.ClickAsync();
            }
        }
    }

    /// <summary>
    /// 候補セレクターのうち最初に利用可能な要素をクリックします。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    /// <param name="selectors">候補セレクターです。</param>
    /// <param name="required">必須要素かどうかです。</param>
    /// <param name="timeoutMilliseconds">待機時間（ミリ秒）です。</param>
    private static async Task ClickFirstAvailableAsync(IPage page, IEnumerable<string> selectors, bool required, float timeoutMilliseconds = 10000)
    {
        var locator = await FindFirstAvailableAsync(page, selectors, timeoutMilliseconds);
        if (locator is null)
        {
            if (required)
            {
                throw new TimeoutException($"対象の UI 要素を検出できませんでした。候補: {string.Join(", ", selectors)}");
            }
            return;
        }

        try
        {
            await locator.ClickAsync();
        }
        catch (PlaywrightException)
        {
            // アニメーション中や透明要素の重なりで通常クリックが遮られた場合は強制クリックを試行
            await locator.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
        }
    }

    /// <summary>
    /// 候補セレクターのうち最初に利用可能な要素を返します（複数セレクターを同時に監視）。
    /// </summary>
    /// <param name="page">操作対象ページです。</param>
    /// <param name="selectors">候補セレクターです。</param>
    /// <param name="timeoutMilliseconds">待機時間です。</param>
    /// <returns>見つかったロケーター、見つからない場合は <see langword="null"/> です。</returns>
    private static async Task<ILocator?> FindFirstAvailableAsync(IPage page, IEnumerable<string> selectors, float timeoutMilliseconds)
    {
        var selectorList = selectors.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (selectorList.Count == 0)
        {
            return null;
        }

        ILocator? combined = null;
        foreach (var selector in selectorList)
        {
            var loc = page.Locator(selector);
            combined = combined is null ? loc : combined.Or(loc);
        }

        if (combined is null)
        {
            return null;
        }

        var first = combined.First;
        try
        {
            await first.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMilliseconds
            });
            return first;
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch (PlaywrightException)
        {
            return null;
        }
    }
}
