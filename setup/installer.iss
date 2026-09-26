; Inno Setup Script for Instagram Uploader
#define MyAppName "Instagram Uploader"
#define MyAppPublisher "tkizawa"
#define MyAppExeName "InstagramUploader.exe"
#define MyAppURL "https://github.com/tkizawa/InstagramUploader"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.1"
#endif

#ifndef MyAppArch
  #define MyAppArch "x64"
#endif

#ifndef SourceDir
  #define SourceDir "..\publish"
#endif

[Setup]
AppId={{D814144B-A57E-4DEE-88D8-80F9AD8D6DC7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\Installer
OutputBaseFilename=InstagramUploader-{#MyAppVersion}-{#MyAppArch}-Setup
SetupIconFile=..\assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog commandline
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
japanese.LaunchProgram=Instagram Uploader を起動する
japanese.InstallPlaywright=Playwright ブラウザコンポーネント (Chromium) を準備する
japanese.DesktopShortcut=デスクトップにショートカットを作成する
english.LaunchProgram=Launch Instagram Uploader
english.InstallPlaywright=Prepare Playwright browser components (Chromium)
english.DesktopShortcut=Create a desktop shortcut

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "playwright"; Description: "{cm:InstallPlaywright}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\playwright.ps1"" install chromium"; StatusMsg: "{cm:InstallPlaywright}"; Flags: runhidden; Tasks: playwright
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent
