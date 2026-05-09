# Instagram Uploader ご利用マニュアル

## 1. アプリケーションの概要
`Instagram Uploader` は、指定したフォルダに画像ファイル(`.jpg`, `.jpeg`, `.png`)を配置するだけで、自動的にInstagramに投稿を行うWindows向けデスクトップアプリケーションです。
画像ファイルのEXIF情報を自動で読み取り、キャプション（投稿文）として付与したうえでバックグラウンド監視による自動アップロードを行います。

---

## 2. 初期設定

初めてご使用になる前に、以下のアカウント設定を行ってください。

### 1. 認証情報ファイルの設定
アプリケーションと同じフォルダ（または実行ファイルの同階層）に `credentials.json` というファイルを作成します。（`sample-credentials.json` がある場合は、それをコピーして名前を変更してください）

`credentials.json` をテキストエディタ（メモ帳など）で開き、以下のようにご自身のFacebook（Instagram連携済）のログイン情報を入力して保存します。

```json
{
  "Username": "あなたのFacebookのメールアドレスまたは電話番号",
  "Password": "パスワード",
  "UploadFolder": "（オプション）監視したいフォルダのフルパス"
}
```

* **Username / Password**: Instagramに連携しているFacebookアカウントの情報を入力します。（本システムはFacebookログインボタン経由でログイン処理を行います）
* **UploadFolder**: 任意のフォルダを監視したい場合にフルパス（例: `C:\\Users\\Name\\Pictures\\Instagram`）を入力します。空欄または項目ごと削除した場合は、アプリと同じフォルダ内に自動作成される `Uploads` フォルダが対象になります。

---

## 3. 事前準備

初回実行前に、Playwright が利用するブラウザをインストールしてください。

```powershell
pwsh bin\Debug\net10.0-windows\playwright.ps1 install chromium
```

発行済みアプリを配布する場合も、同等の Playwright Chromium セットアップが必要です。

---

## 4. 使い方（自動投稿の手順）

### STEP 1: アプリケーションの起動
1. `InstagramUploader.exe` をダブルクリックして起動します。
2. 画面の右下（タスクトレイ）にアイコンが表示され、バックグラウンドでの監視が始まります。
3. 起動直後にタスクトレイのバルーン通知が表示されます。

### STEP 2: 画像の配置（アップロード）
1. 監視対象となっているフォルダ（デフォルトではアプリ直下の `Uploads` フォルダ）を開きます。
2. 投稿したい画像（`.jpg`, `.jpeg`, `.png`）をこのフォルダにコピーまたは移動します。
3. フォルダにファイルが置かれるとアプリが自動検知し、Playwright 用の Chromium ブラウザが立ち上がって自動でアップロード処理を開始します。
4. 複数ファイルが置かれた場合も、アップロードは1件ずつ順番に処理されます。
5. 実行時に自動的に画像に記録されているEXIF情報（撮影設定など）が読み取られ、キャプションとして入力されます。

### STEP 3: 終了方法
* 常にフォルダを監視し続けるため、通常は右上の×ボタンなどで黒い画面を閉じないでください。
* 安全にアプリケーションを終了する場合は、画面右下の**タスクトレイのアイコンを右クリック**し、「**監視を終了する**」を選択してください。

---

## 4. このアプリの動作の仕組みと特徴
* **EXIF付与機能**: 写真ファイルに埋め込まれている撮影データ (EXIF情報) を読み取り、キャプションに自動入力します。
* **セッションの保持**: 初回アクセス時はログイン処理を行いますが、2回目以降は前回ログイン時のセッション(Cookie)を使用するためスムーズに投稿されます。
* **タスクトレイ常駐**: フォルダの監視はバックグラウンドで行われ、ユーザーの作業の邪魔になりません。

---

## 5. 簡易UML

```mermaid
classDiagram
    Program --> UploadMonitoringHostedService
    Program --> TrayApplicationContext
    UploadMonitoringHostedService --> FolderWatchService
    UploadMonitoringHostedService --> UploadQueueProcessor
    FolderWatchService --> UploadQueueProcessor : Enqueue(file)
    UploadQueueProcessor --> FileReadinessChecker
    UploadQueueProcessor --> ExifCaptionBuilder
    UploadQueueProcessor --> PlaywrightInstagramUploader
    PlaywrightInstagramUploader --> AppSettings
    PlaywrightInstagramUploader --> WindowsUserNotifier
    FileLogger <.. UploadMonitoringHostedService
    FileLogger <.. FolderWatchService
    FileLogger <.. UploadQueueProcessor
    FileLogger <.. PlaywrightInstagramUploader
```

画像ファイルを監視フォルダに置くと `FolderWatchService` が検知し、`UploadQueueProcessor` がキューに積んで順番に処理します。処理時に `FileReadinessChecker` で書き込み完了を待ち、`ExifCaptionBuilder` でキャプションを組み立て、`PlaywrightInstagramUploader` が Instagram 投稿を実行します。

---

## 6. 注意事項・トラブルシューティング

* **初回ログイン時の認証（ロボットチェック等）**
  初回のログイン時に「不審なログイン」とみなされたり、「ロボットではありません」などの認証画面が表示されたりする場合があります。その際はダイアログで案内が表示され、ブラウザのウィンドウも開きますので、**制限時間内（最大5分）に手動で突破（クリック・認証コード入力など）**を行ってください。認証を抜けると自動処理が再開されます。
* **対象外のファイルフォーマット**
  動画ファイル（mp4など）や、HEIC形式などの画像は対象外です。JPG/PNGなどに変換してから配置してください。
* **2段階認証について**
  Facebookアカウントで強力な2段階認証を設定している場合は手動で突破する必要があります。一度突破できれば `BrowserState` フォルダにログイン状態が保存されるため次回以降は不要になります。
* **ネットワークエラー**
  処理中に `app_log.txt` に「アップロードに失敗しました」と表示された場合は、手動でInstagramにアクセスできるか確認し、原因を取り除いたあとに再度ファイルをフォルダ内へ配置してください。失敗したファイルは自動では `Uploaded` フォルダへ移動されません。
* **ログの確認**
  動作がおかしい場合は、アプリと同じフォルダに生成される `app_log.txt` を確認してください。エラーの詳細が記載されています。
