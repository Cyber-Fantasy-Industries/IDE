; =========================
;  Portal Installer (Inno)
; =========================

#define AppName "Portal"
#define AppPublisher "Portal"
#define AppExeName "Portal.exe"

; --- HIER ANPASSEN ---
; Quelle: Ordner mit deinem gepublish-ten Output (Release)
; Beispiel aus deinem Script: GatewayIDE.App\bin\Release
#define SourceDir "C:\Users\aaron\Desktop\IDE\GatewayIDE.App\bin\Release"

; Falls deine EXE aktuell anders heißt, trägst du sie hier ein.
; (Wir kopieren sie nach Portal.exe um, ohne das Projekt schon umzubenennen.)
#define SourceExe "GatewayIDE.App.exe"
; ----------------------

[Setup]
AppId={{A6F6D2A7-7D76-4B6D-8B2C-PORTAL0000001}}
AppName={#AppName}
AppVersion=0.1.0
AppPublisher={#AppPublisher}
DefaultDirName={pf64}\Portal
DefaultGroupName={#AppName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
OutputDir=.
OutputBaseFilename=Portal-Setup
WizardStyle=modern

; Optional Icon:
; SetupIconFile=assets\portal.ico

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Files]
; alles kopieren, aber keine exe
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Excludes: "*.exe"

; genau eine GUI-exe als Portal.exe installieren
Source: "{#SourceDir}\{#SourceExe}"; DestDir: "{app}"; DestName: "{#AppExeName}"; Flags: ignoreversion

; Prereq Script mitinstallieren
Source: "tools\portal-prereq.ps1"; DestDir: "{app}\tools"; Flags: ignoreversion


[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; Flags: unchecked

[Run]
; Optional: direkt starten (wir machen das lieber kontrolliert über die letzte Seite)
; Filename: "{app}\{#AppExeName}"; Description: "Portal starten"; Flags: nowait postinstall skipifsilent

[Code]
var
  PrereqPage: TWizardPage;
  BtnCheck, BtnGit, BtnDocker: TNewButton;
  LblStatus: TNewStaticText;

function PowerShellPath(): string;
begin
  Result := ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
end;

procedure RunPS(const Args: string);
var
  ResultCode: Integer;
begin
  ShellExec('open', PowerShellPath(),
    '-NoProfile -ExecutionPolicy Bypass ' + Args,
    '', SW_SHOWNORMAL, ewNoWait, ResultCode);
end;

procedure CheckClick(Sender: TObject);
begin
  LblStatus.Caption := 'Prüfe... (Fenster kann kurz aufgehen)';
  RunPS('-File "' + ExpandConstant('{app}\tools\portal-prereq.ps1') + '" -Action check');
  LblStatus.Caption := 'Prüfung gestartet. Wenn etwas fehlt: Buttons benutzen.';
end;

procedure GitClick(Sender: TObject);
begin
  RunPS('-File "' + ExpandConstant('{app}\tools\portal-prereq.ps1') + '" -Action install-git');
end;

procedure DockerClick(Sender: TObject);
begin
  RunPS('-File "' + ExpandConstant('{app}\tools\portal-prereq.ps1') + '" -Action install-docker');
end;

procedure InitializeWizard;
begin
  PrereqPage := CreateCustomPage(wpFinished, 'System-Check (optional)', 'Git & Docker prüfen / installieren');

  LblStatus := TNewStaticText.Create(PrereqPage);
  LblStatus.Parent := PrereqPage.Surface;
  LblStatus.Left := 0;
  LblStatus.Top := 0;
  LblStatus.Width := PrereqPage.SurfaceWidth;
  LblStatus.Caption := 'Du kannst hier prüfen, ob Git und Docker vorhanden sind.';

  BtnCheck := TNewButton.Create(PrereqPage);
  BtnCheck.Parent := PrereqPage.Surface;
  BtnCheck.Left := 0;
  BtnCheck.Top := 40;
  BtnCheck.Width := 200;
  BtnCheck.Caption := 'Prüfen';
  BtnCheck.OnClick := @CheckClick;

  BtnGit := TNewButton.Create(PrereqPage);
  BtnGit.Parent := PrereqPage.Surface;
  BtnGit.Left := 0;
  BtnGit.Top := 80;
  BtnGit.Width := 200;
  BtnGit.Caption := 'Git installieren';
  BtnGit.OnClick := @GitClick;

  BtnDocker := TNewButton.Create(PrereqPage);
  BtnDocker.Parent := PrereqPage.Surface;
  BtnDocker.Left := 0;
  BtnDocker.Top := 120;
  BtnDocker.Width := 200;
  BtnDocker.Caption := 'Docker Desktop installieren';
  BtnDocker.OnClick := @DockerClick;
end;
end.