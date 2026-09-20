<#
.SYNOPSIS
    Instagram Uploader のスタンドアロンインストーラ (.exe) をビルドします。

.DESCRIPTION
    プロジェクトルールに準拠し、実行環境のアーキテクチャ（x64 / Arm64 等）に合わせた
    自己完結型バイナリを発行し、Inno Setup を用いて .\Installer フォルダに
    バージョン番号付きの exe インストーラを生成します。
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Architecture = "",
    [switch]$SkipTest
)

$ErrorActionPreference = "Stop"

# 1. アーキテクチャの判定 (未指定時は実行環境のアーキテクチャに準拠)
if ([string]::IsNullOrWhiteSpace($Architecture)) {
    $osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
    $Architecture = switch ($osArch) {
        "x64"   { "x64" }
        "arm64" { "arm64" }
        "x86"   { "x86" }
        default { "x64" }
    }
}
$rid = "win-$Architecture"
Write-Host "ターゲットアーキテクチャ: $Architecture (RID: $rid)" -ForegroundColor Cyan

# 2. プロジェクトバージョン情報の取得
$projectFile = Join-Path $PSScriptRoot "InstagramUploader.csproj"
[xml]$projXml = Get-Content $projectFile
$versionNode = $projXml.SelectSingleNode("//Version")
$version = if ($versionNode -and -not [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    $versionNode.InnerText.Trim()
} else {
    "1.0.0.0"
}
Write-Host "アプリケーションバージョン: $version" -ForegroundColor Cyan

# 3. テストの実行 (スキップフラグがない場合)
if (-not $SkipTest) {
    Write-Host "単体テストを実行中..." -ForegroundColor Cyan
    dotnet test $PSScriptRoot -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "単体テストに失敗しました。"
    }
}

# 4. 発行 (Publish) ディレクトリの準備と発行実行
$publishDir = Join-Path $PSScriptRoot "publish"
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

Write-Host "自己完結型バイナリを発行中 ($Configuration, $rid)..." -ForegroundColor Cyan
dotnet publish $projectFile `
    -c $Configuration `
    -r $rid `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish に失敗しました。"
}

# 5. Inno Setup コンパイラ (iscc.exe) の探索
$isccCandidates = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if ($candidate -and (Test-Path $candidate)) {
        $isccPath = $candidate
        break
    }
}

if (-not $isccPath) {
    throw "Inno Setup コンパイラ (ISCC.exe) が見つかりませんでした。Inno Setup 6 がインストールされているか確認してください。"
}
Write-Host "Inno Setup コンパイラ: $isccPath" -ForegroundColor Cyan

# 6. 出力先 .\Installer フォルダの準備
$installerOutputDir = Join-Path $PSScriptRoot "Installer"
if (-not (Test-Path $installerOutputDir)) {
    New-Item -ItemType Directory -Path $installerOutputDir | Out-Null
}

# 7. インストーラのコンパイル実行
$issFile = Join-Path $PSScriptRoot "setup\installer.iss"
Write-Host "インストーラ (.exe) をコンパイル中..." -ForegroundColor Cyan

& "$isccPath" `
    "/DMyAppVersion=$version" `
    "/DMyAppArch=$Architecture" `
    "/DSourceDir=$publishDir" `
    "$issFile"

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup によるインストーラ生成に失敗しました。"
}

# 8. 生成された成果物の確認
$expectedExe = Join-Path $installerOutputDir "InstagramUploader-$version-$Architecture-Setup.exe"
if (Test-Path $expectedExe) {
    $item = Get-Item $expectedExe
    $sizeMB = [math]::Round($item.Length / 1MB, 2)
    Write-Host "`n=======================================================" -ForegroundColor Green
    Write-Host "スタンドアロンインストーラが正常に作成されました！" -ForegroundColor Green
    Write-Host "ファイル: $($item.FullName)" -ForegroundColor Green
    Write-Host "サイズ  : $sizeMB MB" -ForegroundColor Green
    Write-Host "バージョン: $version" -ForegroundColor Green
    Write-Host "アーキテクチャ: $Architecture" -ForegroundColor Green
    Write-Host "=======================================================`n" -ForegroundColor Green
} else {
    throw "期待されるインストーラファイルが見つかりません: $expectedExe"
}
