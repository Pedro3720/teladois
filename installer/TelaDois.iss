; Instalador do TelaDois (Etapa 6a). Monte com installer\montar.ps1: ele publica o app, baixa o driver e o FFmpeg
; conferidos e chama o ISCC com /DAppVersion e /DStage.
#ifndef AppVersion
  #define AppVersion "0.0.0-local"
#endif
#ifndef Stage
  #define Stage "out\stage"
#endif

[Setup]
; AppId fixo: instalar uma versão nova por cima substitui a anterior.
AppId={{6F1C2A0E-4B7D-4E57-9B0A-7D2C5E8A1F30}
AppName=TelaDois
AppVersion={#AppVersion}
AppVerName=TelaDois {#AppVersion}
AppPublisher=TelaDois
AppPublisherURL=https://github.com/Pedro3720/teladois
AppSupportURL=https://github.com/Pedro3720/teladois/blob/main/docs/INSTALACAO.md
DefaultDirName={autopf}\TelaDois
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Windows 10 2004 ou mais novo (exigido pelo Virtual Display Driver).
MinVersion=10.0.19041
OutputDir=out
OutputBaseFilename=TelaDois-Setup-{#AppVersion}
SetupIconFile=..\windows\src\TelaDois.Tray\TelaDois.ico
UninstallDisplayIcon={app}\TelaDois.exe
UninstallDisplayName=TelaDois
Compression=lzma2/ultra64
SolidCompression=yes
; Segue o tema claro/escuro do Windows, no estilo do Windows 11.
WizardStyle=modern dynamic windows11
WizardImageFile=imagens\assistente-100.png,imagens\assistente-150.png,imagens\assistente-200.png
WizardSmallImageFile=imagens\icone-100.png,imagens\icone-150.png,imagens\icone-200.png
; O TelaDois é fechado no PrepareToInstall (com o FFmpeg que ele abriu).
CloseApplications=no
SetupLogging=yes

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Messages]
ptbr.SelectTasksLabel2=Confira as opções e clique em Instalar.
ptbr.FinishedLabel=O TelaDois está na bandeja, perto do relógio. Conecte o iPhone pelo cabo e abra o app TelaDois nele.

[Tasks]
Name: "monitor"; Description: "Instalar o monitor virtual (o iPhone vira uma tela a mais, não uma cópia)"
Name: "autostart"; Description: "Abrir o TelaDois junto com o Windows"
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"

[Files]
Source: "{#Stage}\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#Stage}\ffmpeg\*"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion
Source: "{#Stage}\driver\*"; DestDir: "{app}\driver"; Flags: ignoreversion
Source: "{#Stage}\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion

[Dirs]
Name: "{app}\logs"

[Icons]
Name: "{autoprograms}\TelaDois"; Filename: "{app}\TelaDois.exe"
Name: "{autodesktop}\TelaDois"; Filename: "{app}\TelaDois.exe"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TelaDois"; ValueData: """{app}\TelaDois.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\TelaDois.exe"; Description: "Abrir o TelaDois agora"; Flags: nowait postinstall runasoriginaluser skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
const
  RebootCode = 3010;
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  AppleUrl = 'https://www.apple.com/itunes/download/win64';

var
  NeedsReboot: Boolean;
  Warnings: String;

function RunScript(const Script, Args, Log: String; var Code: Integer): Boolean;
begin
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + Script + '" ' + Args + ' -LogPath "' + Log + '"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

procedure StopTelaDois;
var
  Code: Integer;
  FfmpegDir: String;
begin
  { /T leva junto o ffmpeg.exe que o TelaDois abriu. Sem TelaDois aberto o taskkill só devolve 128. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM TelaDois.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
  { Se o TelaDois caiu, o ffmpeg.exe da pasta do app pode ter ficado órfão e travaria a troca dos arquivos. }
  FfmpegDir := ExpandConstant('{app}\ffmpeg\');
  StringChangeEx(FfmpegDir, '''', '''''', True);
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -NonInteractive -Command "Get-Process ffmpeg -ErrorAction SilentlyContinue | ' +
    'Where-Object { $_.Path -and $_.Path.StartsWith(''' + FfmpegDir + ''', ''OrdinalIgnoreCase'') } | Stop-Process -Force"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

procedure AddWarning(const Text: String);
begin
  if Warnings <> '' then
    Warnings := Warnings + #13#10#13#10;
  Warnings := Warnings + Text;
end;

procedure ShowStep(const Text: String);
begin
  WizardForm.StatusLabel.Caption := Text;
  WizardForm.ProgressGauge.Style := npbstMarquee;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopTelaDois;
  Result := '';
end;

procedure InstallMonitor;
var
  Code: Integer;
  Log: String;
begin
  ShowStep('Instalando o monitor virtual...');
  Log := ExpandConstant('{app}\logs\instalar-vdd.log');
  if RunScript(ExpandConstant('{app}\driver\instalar-vdd.ps1'),
       ExpandConstant('-DriverZip "{app}\driver\VirtualDisplayDriver-x86.Driver.Only.zip"'), Log, Code)
     and ((Code = 0) or (Code = RebootCode)) then
    NeedsReboot := NeedsReboot or (Code = RebootCode)
  else
    { A linha não pode começar com #13#10: o pré-processador (ISPP) a leria como diretiva. }
    AddWarning('O monitor virtual não foi instalado. O TelaDois funciona assim mesmo, mostrando no iPhone uma cópia de um monitor.' + #13#10 +
      'Para tentar de novo, rode este instalador outra vez. Detalhes: ' + Log);
end;

procedure CheckApple;
var
  Code: Integer;
  ErrorCode: Integer;
begin
  ShowStep('Conferindo o driver USB da Apple...');
  if not RunScript(ExpandConstant('{app}\tools\verificar-apple.ps1'), '', ExpandConstant('{app}\logs\verificar-apple.log'), Code)
     or (Code <> 0) then
  begin
    if not WizardSilent then
      ShellExecAsOriginalUser('open', AppleUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    AddWarning('Falta o driver USB da Apple, que vem com o iTunes. Abrimos a página de download: instale o iTunes para o PC reconhecer o iPhone.');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('monitor') then
      InstallMonitor;
    if not WizardIsTaskSelected('autostart') then
      RegDeleteValue(HKLM, RunKey, 'TelaDois');
    CheckApple;
    if Warnings <> '' then
      Log('Avisos: ' + Warnings);
    if (Warnings <> '') and not WizardSilent then
      MsgBox(Warnings, mbInformation, MB_OK);
    if NeedsReboot and WizardSilent then
      Log('Reinício pendente: o monitor virtual só funciona depois de reiniciar o Windows (não reiniciamos sozinhos).');
  end;
end;

{ Nunca reinicia sozinho: no modo silencioso só registra no log; no interativo a página final pergunta. }
function NeedRestart: Boolean;
begin
  Result := NeedsReboot and not WizardSilent;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  { Já deixa marcado "reiniciar mais tarde" na página final. }
  if (CurPageID = wpFinished) and WizardForm.NoRadio.Visible then
    WizardForm.NoRadio.Checked := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Code: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    StopTelaDois;
    RunScript(ExpandConstant('{app}\driver\desinstalar-vdd.ps1'), '', ExpandConstant('{%TEMP}\TelaDois-desinstalar-vdd.log'), Code);
  end;
end;
