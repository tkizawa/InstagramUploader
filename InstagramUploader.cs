using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace InstagramUploader
{
    class Program
    {
        // 監視するフォルダパス
        static readonly string WatchFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
        
        // ★Instagramのログイン情報 (credentials.jsonから読み込みます)
        static string Username = "";
        static string Password = "";

        private static void LoadCredentials()
        {
            string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credentials.json");
            if (!File.Exists(credPath))
            {
                Console.WriteLine("エラー: credentials.json が見つかりません。プロジェクトルートと同じ場所に作成してください。");
                Environment.Exit(1);
            }

            try
            {
                string json = File.ReadAllText(credPath);
                using var document = JsonDocument.Parse(json);
                Username = document.RootElement.GetProperty("Username").GetString() ?? "";
                Password = document.RootElement.GetProperty("Password").GetString() ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: credentials.json の読み込みに失敗しました。フォーマット等を確認してください。{ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task Main(string[] args)
        {
            LoadCredentials();

            if (!System.IO.Directory.Exists(WatchFolder))
            {
                System.IO.Directory.CreateDirectory(WatchFolder);
            }

            Console.WriteLine($"フォルダの監視を開始します: {WatchFolder}");
            Console.WriteLine("対象フォルダに .jpg, .jpeg, .png ファイルを配置してください。");
            Console.WriteLine("※実行前にソースコード内の(YOUR_USERNAME / YOUR_PASSWORD)を設定してください。");

            // 起動時にすでに存在するファイルも処理する
            ProcessExistingFiles(WatchFolder);

            using var watcher = new FileSystemWatcher(WatchFolder);
            
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("終了するにはEnterキーを押してください...");
            Console.ReadLine();
        }

        private static void ProcessExistingFiles(string folderPath)
        {
            var files = System.IO.Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    Console.WriteLine($"既存のファイルを検知しました: {file}");
                    // 非同期でアップロード処理を開始（待機しない）
                    _ = UploadToInstagramWrapper(file);
                }
            }
        }

        private static async Task UploadToInstagramWrapper(string file)
        {
            try
            {
                await UploadToInstagram(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"アップロードエラー: {ex.Message}");
            }
        }

        private static async void OnCreated(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath).ToLower();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                Console.WriteLine($"\n新しい画像が検知されました: {e.FullPath}");
                // ファイルが完全に書き込まれるまで少し待機
                await Task.Delay(1000);
                
                try
                {
                    await UploadToInstagram(e.FullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"アップロードエラー: {ex.Message}");
                }
            }
        }

        private static async Task UploadToInstagram(string filePath)
        {
            using var playwright = await Playwright.CreateAsync();
            
            string userDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserState");

            // ブラウザのCookieやログイン状態を保持するPersistentContextを使用する
            await using var context = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = new ViewportSize { Width = 1200, Height = 800 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36"
            });
            
            var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
            page.SetDefaultTimeout(300000); // すべての待機処理のデフォルトタイムアウトを5分に設定

            Console.WriteLine("Instagramを開いています...");
            await page.GotoAsync("https://www.instagram.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle); // 描画完了を待機
            
            // すでにログイン済みか（作成ボタンが存在するか）判定
            var createBtnLocator = page.Locator("[aria-label='新規投稿を作成'], a[href='#']:has-text('作成')");
            bool isLoggedIn = false;
            try
            {
                // 5秒だけ待って、作成ボタンがあればログイン済みとみなす
                await createBtnLocator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                isLoggedIn = true;
            }
            catch { }

            if (!isLoggedIn)
            {
                // 「Facebookでログイン」ボタンをクリック
                Console.WriteLine("ログインしていません。Facebookログイン画面へ遷移します...");
                Console.WriteLine("初回のみ: ロボット認証やセキュリティチェックが表示された場合、ブラウザ上で手動で突破してください（最大5分待機します）");
                var fbLoginBtn = page.Locator("button:has-text('Facebookでログイン'), a:has-text('Facebookでログイン'), span:has-text('Facebookでログイン')").First;
                await fbLoginBtn.WaitForAsync();
                await fbLoginBtn.ClickAsync();

                // Facebookのログイン画面に遷移するのを待機
                await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*facebook.com.*"));

                // Facebook側のログインフォーム入力
                Console.WriteLine("Facebookの認証情報を入力しています...");
                var emailInput = page.Locator("input[id='email'], input[name='email']").First;
                await emailInput.WaitForAsync();
                await emailInput.FillAsync(Username);
                
                var fbPasswordInput = page.Locator("input[id='pass'], input[name='pass']").First;
                await fbPasswordInput.FillAsync(Password);

                // パスワード入力欄でEnterキーを押すことでFacebookログインを実行
                await fbPasswordInput.PressAsync("Enter");
                
                Console.WriteLine("Instagramの画面へ戻るのを待機しています...(最大5分)");

                // ログイン後画面(Instagram)に遷移するか待機
                await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("^(?!.*accounts/login).*$"));
                Console.WriteLine("✓ ログイン成功");
            }
            else
            {
                Console.WriteLine("✓ すでにログイン済みです（状態を再利用）");
            }

            // 通知ダイアログ等（「後で」など）が出る可能性があるためタイムアウト短めで待って押す
            try
            {
                var notNowBtn = await page.WaitForSelectorAsync("button:has-text('後で')", new PageWaitForSelectorOptions { Timeout = 3000 });
                if (notNowBtn != null) await notNowBtn.ClickAsync();
            }
            catch { }
            try
            {
                var notNowBtn2 = await page.WaitForSelectorAsync("button:has-text('後で')", new PageWaitForSelectorOptions { Timeout = 3000 });
                if (notNowBtn2 != null) await notNowBtn2.ClickAsync();
            }
            catch { }

            // 新規作成ボタン (UIによって要素が変わるため複数候補を試すかaria-labelを使う)
            Console.WriteLine("作成ボタンをクリック...");
            await page.ClickAsync("[aria-label='新規投稿を作成'], a[href='#']:has-text('作成')");
            
            // ファイル選択
            Console.WriteLine("ファイルを選択...");
            var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =>
            {
                await page.ClickAsync("button:has-text('コンピューターから選択')");
            });
            await fileChooser.SetFilesAsync(filePath);

            await Task.Delay(2000); // プレビュー画面の表示アニメーションを待機

            // 切り抜き → 「次へ」 (button要素ではなくdiv要素などの場合もあるためtextで検索)
            Console.WriteLine("次へ (1/2)...");
            var nextBtn1 = page.Locator("text=次へ").First;
            await nextBtn1.WaitForAsync();
            await nextBtn1.ClickAsync();
            
            // フィルター → 「次へ」
            Console.WriteLine("次へ (2/2)...");
            await Task.Delay(2000); // 画面遷移アニメーション待ち
            var nextBtn2 = page.Locator("text=次へ").First;
            await nextBtn2.WaitForAsync();
            await nextBtn2.ClickAsync();

            // キャプション入力画面
            Console.WriteLine("キャプションを入力...");
            await Task.Delay(1000);
            await page.WaitForSelectorAsync("div[aria-label='キャプションを入力…']");
            
            string caption = $"アップロードテスト - {Path.GetFileName(filePath)}\n\n(自動投稿システムからの投稿)\n{GetExifCaption(filePath)}";
            await page.FillAsync("div[aria-label='キャプションを入力…']", caption);

            // シェア
            Console.WriteLine("シェアボタンをクリック...");
            await Task.Delay(2000); // 画面が完全に切り替わるのを待つ
            var shareBtn = page.Locator("div[role='dialog']").Locator("text=シェア").First;
            await shareBtn.ClickAsync();

            // 完了待機
            Console.WriteLine("アップロード処理の完了を待機しています...(最大1分)");
            try
            {
                // シェア実行後、同じダイアログ内に「完了」が現れるのを直接待つ
                var doneBtn = page.Locator("div[role='dialog']").Locator("text=完了").First;
                await doneBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 60000 });
                
                Console.WriteLine("「完了」リンクを検知したためクリックします...");
                await doneBtn.ClickAsync();
                await Task.Delay(2000); // ダイアログが閉じるアニメーションを待機

                Console.WriteLine("✓ 投稿完了！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"※完了画面の検知でタイムアウトしましたが、裏でアップロード自体は完了している可能性があります: {ex.Message}");
            }

            // 念のためアップロードの裏側処理が終わるように少し待機
            await Task.Delay(3000);

            await context.CloseAsync();

            // 処理が終わったらファイルをリネームか移動して連続投稿を防ぐ
            string uploadedFolder = Path.Combine(Path.GetDirectoryName(filePath)!, "Uploaded");
            if (!System.IO.Directory.Exists(uploadedFolder))
            {
                System.IO.Directory.CreateDirectory(uploadedFolder);
            }
            string destPath = Path.Combine(uploadedFolder, Path.GetFileName(filePath));
            if (File.Exists(destPath)) File.Delete(destPath);
            File.Move(filePath, destPath);
            Console.WriteLine($"ファイルを移動しました: {destPath}");
        }

        private static string GetExifCaption(string filePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

                if (subIfd == null && ifd0 == null) return string.Empty;

                var sb = new StringBuilder();

                // 撮影日時
                var dateTime = subIfd?.GetString(ExifDirectoryBase.TagDateTimeOriginal);
                if (!string.IsNullOrEmpty(dateTime)) sb.AppendLine($"撮影日時: {dateTime}");

                // カメラ名
                var make = ifd0?.GetString(ExifDirectoryBase.TagMake);
                var model = ifd0?.GetString(ExifDirectoryBase.TagModel);
                if (!string.IsNullOrEmpty(make) || !string.IsNullOrEmpty(model))
                {
                    sb.AppendLine($"カメラ名: {make} {model}".Trim());
                }

                // レンズ名
                var lensMake = subIfd?.GetString(ExifDirectoryBase.TagLensMake);
                var lensModel = subIfd?.GetString(ExifDirectoryBase.TagLensModel);
                if (!string.IsNullOrEmpty(lensMake) || !string.IsNullOrEmpty(lensModel))
                {
                    sb.AppendLine($"レンズ名: {lensMake} {lensModel}".Trim());
                }

                // 焦点距離
                var focalLength = subIfd?.GetString(ExifDirectoryBase.TagFocalLength);
                if (!string.IsNullOrEmpty(focalLength)) sb.AppendLine($"焦点距離: {focalLength}mm");

                // 絞り
                var fNumber = subIfd?.GetString(ExifDirectoryBase.TagFNumber);
                if (!string.IsNullOrEmpty(fNumber)) sb.AppendLine($"絞り: f/{fNumber}");

                // シャッタースピード
                var exposureTime = subIfd?.GetString(ExifDirectoryBase.TagExposureTime);
                if (!string.IsNullOrEmpty(exposureTime)) sb.AppendLine($"シャッタースピード: {exposureTime}秒");

                // ISO感度
                var iso = subIfd?.GetString(ExifDirectoryBase.TagIsoEquivalent);
                if (!string.IsNullOrEmpty(iso)) sb.AppendLine($"ISO感度: {iso}");

                // 露出補正
                var exposureBias = subIfd?.GetString(ExifDirectoryBase.TagExposureBias);
                if (!string.IsNullOrEmpty(exposureBias)) sb.AppendLine($"露出補正: {exposureBias} EV");

                // 測光方式
                var meteringMode = subIfd?.GetDescription(ExifDirectoryBase.TagMeteringMode);
                if (!string.IsNullOrEmpty(meteringMode)) sb.AppendLine($"測光方式: {meteringMode}");

                return sb.ToString().TrimEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
