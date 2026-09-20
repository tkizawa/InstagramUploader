using System.Globalization;

namespace InstagramUploader;

/// <summary>
/// アプリケーションの表示言語（日本語・英語）に対応するリソース文字列を提供します。
/// Windows の表示言語モード（<see cref="CultureInfo.CurrentUICulture"/>）に応じて自動的に切り替えます。
/// </summary>
public static class LocalizationResources
{
    /// <summary>
    /// 現在の表示言語が日本語であるかどうかを判定します。
    /// </summary>
    public static bool IsJapanese =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// アプリケーション名を取得します。
    /// </summary>
    public static string AppTitle => "Instagram Uploader";

    /// <summary>
    /// 設定ウィンドウのタイトルを取得します。
    /// </summary>
    public static string SettingsTitle =>
        IsJapanese ? "Instagram Uploader - 設定" : "Instagram Uploader - Settings";

    /// <summary>
    /// ユーザー名ラベルのテキストを取得します。
    /// </summary>
    public static string UsernameLabel =>
        IsJapanese ? "ユーザー名 (Username):" : "Username:";

    /// <summary>
    /// パスワードラベルのテキストを取得します。
    /// </summary>
    public static string PasswordLabel =>
        IsJapanese ? "パスワード (Password):" : "Password:";

    /// <summary>
    /// パスワード表示切替チェックボックスのテキストを取得します。
    /// </summary>
    public static string ShowPasswordLabel =>
        IsJapanese ? "パスワードを表示する" : "Show password";

    /// <summary>
    /// 監視フォルダラベルのテキストを取得します。
    /// </summary>
    public static string UploadFolderLabel =>
        IsJapanese ? "監視フォルダ (Upload Folder):" : "Upload Folder:";

    /// <summary>
    /// フォルダ参照ボタンのテキストを取得します。
    /// </summary>
    public static string BrowseButton =>
        IsJapanese ? "参照..." : "Browse...";

    /// <summary>
    /// Windows 起動時の自動起動チェックボックスのテキストを取得します。
    /// </summary>
    public static string AutoStartLabel =>
        IsJapanese ? "Windows 起動時に自動起動する" : "Start automatically when Windows starts";

    /// <summary>
    /// 保存ボタンのテキストを取得します。
    /// </summary>
    public static string SaveButton =>
        IsJapanese ? "保存" : "Save";

    /// <summary>
    /// キャンセルボタンのテキストを取得します。
    /// </summary>
    public static string CancelButton =>
        IsJapanese ? "キャンセル" : "Cancel";

    /// <summary>
    /// トレイメニューの「設定」項目のテキストを取得します。
    /// </summary>
    public static string TrayMenuSettings =>
        IsJapanese ? "設定(&S)..." : "&Settings...";

    /// <summary>
    /// トレイメニューの「監視を終了する」項目のテキストを取得します。
    /// </summary>
    public static string TrayMenuExit =>
        IsJapanese ? "監視を終了する(&X)" : "E&xit";

    /// <summary>
    /// トレイアイコンのツールチップテキスト（監視中）を取得します。
    /// </summary>
    public static string TrayTooltipWatching =>
        IsJapanese ? "Instagram Uploader 監視中" : "Instagram Uploader Watching";

    /// <summary>
    /// トレイアイコンのツールチップテキスト（未設定）を取得します。
    /// </summary>
    public static string TrayTooltipNotConfigured =>
        IsJapanese ? "Instagram Uploader (未設定)" : "Instagram Uploader (Not configured)";

    /// <summary>
    /// 起動通知バルーンのテキストを取得します。
    /// </summary>
    public static string TrayBalloonStarted =>
        IsJapanese
            ? "フォルダの監視を開始しました。終了や設定変更はタスクトレイのアイコンを右クリックしてください。"
            : "Folder monitoring has started. Right-click the system tray icon to configure or exit.";

    /// <summary>
    /// フォルダ選択ダイアログの説明文を取得します。
    /// </summary>
    public static string FolderBrowserDescription =>
        IsJapanese ? "監視するフォルダを選択してください" : "Select folder to monitor";

    /// <summary>
    /// 設定保存完了メッセージを取得します。
    /// </summary>
    public static string SettingsSavedSuccess =>
        IsJapanese
            ? "設定を保存しました。"
            : "Settings saved successfully.";

    /// <summary>
    /// ユーザー名未入力エラーメッセージを取得します。
    /// </summary>
    public static string ErrorUsernameRequired =>
        IsJapanese ? "ユーザー名を入力してください。" : "Username is required.";

    /// <summary>
    /// パスワード未入力エラーメッセージを取得します。
    /// </summary>
    public static string ErrorPasswordRequired =>
        IsJapanese ? "パスワードを入力してください。" : "Password is required.";

    /// <summary>
    /// 監視フォルダ未指定エラーメッセージを取得します。
    /// </summary>
    public static string ErrorUploadFolderRequired =>
        IsJapanese ? "監視フォルダを指定してください。" : "Upload folder is required.";

    /// <summary>
    /// 必須項目未入力エラーのダイアログタイトルを取得します。
    /// </summary>
    public static string ValidationDialogTitle =>
        IsJapanese ? "入力エラー" : "Validation Error";

    /// <summary>
    /// 情報ダイアログタイトルを取得します。
    /// </summary>
    public static string InfoDialogTitle =>
        IsJapanese ? "情報" : "Information";

    /// <summary>
    /// 起動失敗エラーダイアログのメッセージ書式を取得します。
    /// </summary>
    public static string StartupErrorMessageFormat =>
        IsJapanese
            ? "起動に失敗しました。{0}{1}{0}詳細は app_log.txt を確認してください。"
            : "Failed to start. {0}{1}{0}Please check app_log.txt for details.";
}
