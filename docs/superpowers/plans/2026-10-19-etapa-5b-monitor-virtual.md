# Etapa 5b — Monitor estendido (monitor virtual) + atalho — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Com o cabo plugado e o app aberto, o Windows ganha um monitor extra em Estender (Virtual Display Driver) com a resolução do iPhone e esse monitor aparece no iPhone; Win+Shift+] e o item "Modo monitor" da bandeja ligam/desligam; sem driver, duplica um monitor existente como na 5a.

**Architecture:** O driver fica sempre instalado com 1 monitor; o TelaDois só põe/tira esse monitor do desktop (`ChangeDisplaySettingsEx`) por trás de uma interface testável (`IDisplayApi`). Um `MonitorMode` (lógica pura) diz se o vídeo deve estar ligado; a `LinkSession` passa a ligar/desligar o vídeo no meio da conexão (`VIDEO_END`, tipo 8) e manda `CONFIG` só junto com a 1ª imagem. A `VideoFeedFactory` da 5a escolhe Estender (monitor virtual) ou Duplicar (monitor existente). No iPhone, `VIDEO_END` volta ao relógio sem desconectar.

**Tech Stack:** C# 14 / .NET 10 (SDK 10.0.402), WinForms (bandeja), P/Invoke `user32` (`EnumDisplayDevices`, `EnumDisplaySettings`, `ChangeDisplaySettingsEx`, `RegisterHotKey`, `VkKeyScanEx`), `System.IO.Pipes`, xUnit 2.9.3; Swift 6 (pacote `TelaDoisCore`) / Swift 5 + concorrência estrita (app), XCTest/XCUITest; FFmpeg 9.0.2; Virtual Display Driver 25.7.23; PowerShell + SetupAPI (instalador).

**Spec:** `docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md`. **Branch:** `etapa-5b-monitor-virtual` (saiu do `etapa-5a-video`).

## Global Constraints

- O usuário fala português: textos para ele (bandeja, registro, docs) em português; código e nomes em inglês.
- Não há Mac: nunca rodar `xcodebuild`/`xcodegen`/`swift` localmente. Código iOS é verificado **só** pelo GitHub Actions (`ios-test`, `ios-build`). Um push com mudança em `ios/**` cancela o `ios-test` que estiver rodando: para ver o "vermelho" do TDD, espere a execução anterior terminar antes do próximo push. Confirme o resultado com `gh run view <id> --json status,conclusion` (não confie no código de saída do `gh run watch`).
- PC: `dotnet test windows/TelaDois.sln` local; o `windows-test` do CI roda o mesmo (testes que dependem de FFmpeg ou driver retornam cedo quando eles não existem).
- Zero avisos: C# com `TreatWarningsAsErrors`; Swift com `SWIFT_STRICT_CONCURRENCY: complete`.
- Protocolo nos dois lados com bytes de referência nos dois testes: `HELLO(2556,1179)` = `010000000A0001000009FC0000049B`; `PING(1234567890123)` = `05000000080000011F71FB04CB`; `CONFIG(1366,768,60)` = `020000000A0000055600000300003C`; `VIDEO(completa, 1000 µs, 0000000109F0)` = `030000000F0100000000000003E80000000109F0`; **`VIDEO_END` = `0800000000`** (tipo 8, sem dados).
- Driver: Virtual Display Driver **25.7.23**, pacote `VirtualDisplayDriver-x86.Driver.Only.zip`, SHA-256 `e24210692b442b39af763536330ce78b423f19342b7a7792c26de3944e418b3a`, hardware ID `Root\MttVDD`, configuração `C:\VirtualDisplayDriver\vdd_settings.xml`, pipe `\\.\pipe\MTTVirtualDisplayPipe` (só `PING`; resposta ASCII `PONG`).
- Resoluções em paisagem no `vdd_settings.xml`: 2556×1179, 2796×1290, 2622×1206, 2868×1320, 2736×1260 (e 1920×1080), todas a 60 Hz. Vídeo a 60 fps.
- Atalho: **Win+Shift+]**, tecla achada pelo caractere `]` no layout atual (ABNT2 ≠ US).
- Tooltip da bandeja ≤ 63 caracteres (limite do Windows).
- Antes de baixar qualquer arquivo, pedir autorização ao usuário (nome, origem, tamanho). Instalar driver é com o usuário (administrador).
- No Windows, não editar arquivos com `Get-Content`/`Set-Content` (corrompe UTF-8); usar as ferramentas de edição.
- Commits: escrever a mensagem em `.git/TD_MSG`, `git commit -F .git/TD_MSG`, apagar o arquivo. Toda mensagem termina com a linha `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Nunca `git add -A`: adicionar os arquivos pelo nome.

## Ajustes em relação à spec (decididos ao planejar)

- **Instalador:** usa o pacote só do driver (132 KB) em vez do VDD Control (71 MB). Tirar o monitor do desktop logo após instalar fica com o TelaDois (`DetachLeftover` ao abrir), não com o script.
- **Estados:** o `MonitorMode` guarda ligado/desligado e se há iPhone; **Estender ou Duplicar** é decidido pela `VideoFeedFactory` ao abrir o vídeo (é ela que sabe se o driver respondeu e se o monitor ligou). Os três estados da spec continuam: desligado, ligado-estendido, ligado-duplicando.
- **iPhone:** `CONFIG`/`VIDEO` já são tratados no `UsbLink` (não no `LinkSessionState`); `VIDEO_END` vai para o mesmo lugar. O `LinkSessionState` só ganha um teste de que `VIDEO_END` não pede resposta.
- **Tooltip:** `TelaDois: estendido · 2556×1179 · 60 fps · 12 ms` (ou `duplicando`). O nome da compressão fica no registro (com ela o texto passaria de 63 caracteres).
- **Captura que falha / vídeo que não abre:** vira desligado + `VIDEO_END` (se o iPhone já tinha recebido `CONFIG`), e o túnel continua; o atalho tenta de novo. Na 5a a sessão fechava e o túnel abria outra.

## Estrutura de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `tools/vdd/vdd_settings.xml` (novo) | Configuração do driver: 1 monitor, resoluções dos iPhones a 60 Hz |
| `tools/vdd/instalar-vdd.ps1` (novo) | Baixa o driver (versão e SHA-256 fixos), confere a assinatura, copia o XML, cria o dispositivo `Root\MttVDD` |
| `windows/src/TelaDois.Core/Display/IDisplayApi.cs` (novo) | Interface das chamadas de monitor do Windows + registros `DisplayDeviceInfo`, `DisplayModeInfo`, `DisplayBounds` e `DisplayChangeException` |
| `windows/src/TelaDois.Core/Display/Win32DisplayApi.cs` (novo) | `IDisplayApi` com `user32` (base: `DisplayManager` do branch `etapa-5-segunda-tela`) |
| `windows/src/TelaDois.Core/Display/DriverPipe.cs` (novo) | `IDriverPipe`: `PING` no pipe do driver |
| `windows/src/TelaDois.Core/Display/IVirtualMonitor.cs` (novo) | O que a fábrica de vídeo precisa do monitor virtual |
| `windows/src/TelaDois.Core/Display/VirtualDisplay.cs` (novo) | Acha, liga (à direita, modo mais próximo) e desliga o monitor do driver |
| `windows/src/TelaDois.Core/Display/MonitorMode.cs` (novo) | Ligado/desligado + iPhone conectado; implementa `IVideoSwitch` |
| `windows/src/TelaDois.Core/Display/KeyboardLayout.cs` (novo) | Tecla virtual de um caractere no layout atual |
| `windows/src/TelaDois.Core/Link/IVideoSwitch.cs` (novo) | Contrato liga/desliga que a `LinkSession` consome |
| `windows/src/TelaDois.Core/Link/LinkFrame.cs`, `LinkMessages.cs` | `VideoEnd = 8` |
| `windows/src/TelaDois.Core/Link/LinkSession.cs` | Vídeo liga/desliga, `CONFIG` com a 1ª imagem, `VIDEO_END`, `Kind` no status |
| `windows/src/TelaDois.Core/Video/IVideoFeed.cs` | `VideoKind` (Duplicar/Estender) |
| `windows/src/TelaDois.Core/Video/H264.cs`, `AccessUnitReader.cs` | Reinsere SPS/PPS antes de IDR (I3) |
| `windows/src/TelaDois.Core/Video/VirtualMonitorFeed.cs` (novo) | Feed do monitor virtual: desliga o monitor ao descartar; para se o monitor sair do desktop |
| `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs` | Estender com o monitor virtual; senão Duplicar |
| `windows/src/TelaDois.Core/UsbTunnelService.cs`, `TunnelStatusText.cs` | `MonitorMode` acompanha a sessão; status "estendido"/"duplicando" |
| `windows/src/TelaDois.Tray/Hotkey.cs` (novo), `TrayContext.cs` | Atalho, item "Modo monitor", balões, `DetachLeftover`, desligar ao sair |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift` | `videoEnd` |
| `ios/TelaDois/UsbLink.swift`, `VideoRenderer.swift` | `VIDEO_END` → relógio; `flush(removingDisplayedImage:)` (I2) |
| `ios/TelaDoisUITests/RestScreenUITests.swift` | Teste e print `07-fim-do-video` |
| Testes novos em `windows/tests/TelaDois.Core.Tests/` | `Display/VirtualDisplayTests.cs`, `Display/DriverPipeTests.cs`, `Display/VirtualDisplayLocalTests.cs`, `Display/MonitorModeTests.cs`, `Display/KeyboardLayoutLocalTests.cs`, `Video/VirtualMonitorFeedTests.cs`, `Fakes/FakeDisplayApi.cs`, `Fakes/FakeVirtualMonitor.cs` |

---

### Task 1: Instalador do driver (script + configuração) e instalação no PC

**Files:**
- Create: `tools/vdd/vdd_settings.xml`
- Create: `tools/vdd/instalar-vdd.ps1`

**Interfaces:**
- Consumes: nada.
- Produces: o driver instalado neste PC (dispositivo `Root\MttVDD`, monitor "Virtual Display Driver", pipe `MTTVirtualDisplayPipe`), usado pelas Tasks 2 e 10.

- [ ] **Step 1: Pedir autorização para baixar o pacote para inspeção**

Perguntar ao usuário: "Posso baixar `VirtualDisplayDriver-x86.Driver.Only.zip` (132 KB) da release 25.7.23 de https://github.com/VirtualDrivers/Virtual-Display-Driver para conferir o conteúdo e a assinatura?" Só seguir com um "sim".

- [ ] **Step 2: Baixar numa pasta vazia do scratchpad e conferir**

```powershell
$d = Join-Path $env:TEMP "vdd-inspecao-$([guid]::NewGuid())"; New-Item -ItemType Directory $d | Out-Null
gh release download 25.7.23 --repo VirtualDrivers/Virtual-Display-Driver --pattern VirtualDisplayDriver-x86.Driver.Only.zip --dir $d
(Get-FileHash "$d\VirtualDisplayDriver-x86.Driver.Only.zip" -Algorithm SHA256).Hash
Expand-Archive "$d\VirtualDisplayDriver-x86.Driver.Only.zip" -DestinationPath "$d\x"
Get-ChildItem "$d\x" -Recurse | Select-Object FullName, Length
Get-ChildItem "$d\x" -Recurse -Include *.cat,*.dll | ForEach-Object { Get-AuthenticodeSignature $_.FullName } | Select-Object Path, Status, @{n='Signer';e={$_.SignerCertificate.Subject}}
```

Esperado: hash `E24210692B442B39AF763536330CE78B423F19342B7A7792C26DE3944E418B3A`; arquivos `MttVDD.inf`, `MttVDD.dll`, `MttVDD.cat` (numa subpasta); `Status = Valid` no `.cat` (assinado pela Microsoft — "Microsoft Windows Hardware Compatibility Publisher"). **Se o `.cat` não estiver `Valid`, pare e fale com o usuário**: driver sem assinatura exigiria modo de teste do Windows, fora do combinado.

- [ ] **Step 3: Escrever `tools/vdd/vdd_settings.xml`**

```xml
<?xml version='1.0' encoding='utf-8'?>
<!-- Configuração do Virtual Display Driver 25.7.23 para o TelaDois (instalada por tools/vdd/instalar-vdd.ps1). -->
<!-- 1 monitor sempre instalado: o TelaDois só o põe e tira do desktop. Resoluções em paisagem dos iPhones 15/16/17. -->
<vdd_settings>
    <monitors>
        <count>1</count>
    </monitors>
    <gpu>
        <friendlyname>default</friendlyname>
    </gpu>
    <global>
        <g_refresh_rate>60</g_refresh_rate>
    </global>
    <resolutions>
        <resolution>
            <width>2556</width>
            <height>1179</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
        <resolution>
            <width>2796</width>
            <height>1290</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
        <resolution>
            <width>2622</width>
            <height>1206</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
        <resolution>
            <width>2868</width>
            <height>1320</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
        <resolution>
            <width>2736</width>
            <height>1260</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
        <resolution>
            <width>1920</width>
            <height>1080</height>
            <refresh_rate>60</refresh_rate>
        </resolution>
    </resolutions>
    <logging>
        <SendLogsThroughPipe>false</SendLogsThroughPipe>
        <logging>false</logging>
        <debuglogging>false</debuglogging>
    </logging>
    <colour>
        <SDR10bit>false</SDR10bit>
        <HDRPlus>false</HDRPlus>
        <ColourFormat>RGB</ColourFormat>
    </colour>
    <cursor>
        <HardwareCursor>true</HardwareCursor>
        <CursorMaxY>128</CursorMaxY>
        <CursorMaxX>128</CursorMaxX>
        <AlphaCursorSupport>true</AlphaCursorSupport>
        <XorCursorSupportLevel>2</XorCursorSupportLevel>
    </cursor>
    <edid>
        <CustomEdid>false</CustomEdid>
        <PreventSpoof>false</PreventSpoof>
        <EdidCeaOverride>false</EdidCeaOverride>
    </edid>
</vdd_settings>
```

- [ ] **Step 4: Escrever `tools/vdd/instalar-vdd.ps1`**

```powershell
<#
.SYNOPSIS
  Instala o Virtual Display Driver 25.7.23 (o monitor extra da Segunda Tela do TelaDois).
.DESCRIPTION
  Rode uma vez por PC, num PowerShell aberto como administrador:
    powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1
  Baixa só o pacote do driver (versão e SHA-256 fixos), confere a assinatura, grava
  C:\VirtualDisplayDriver\vdd_settings.xml e cria o dispositivo Root\MttVDD.
  Depois disso o TelaDois não precisa de administrador: ele só põe e tira o monitor do desktop.
#>
#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$Version = '25.7.23'
$Asset = 'VirtualDisplayDriver-x86.Driver.Only.zip'
$Sha256 = 'E24210692B442B39AF763536330CE78B423F19342B7A7792C26DE3944E418B3A'
$Url = "https://github.com/VirtualDrivers/Virtual-Display-Driver/releases/download/$Version/$Asset"
$HardwareId = 'Root\MttVDD'
$ConfigDir = 'C:\VirtualDisplayDriver'

# Cria o dispositivo raiz e instala o driver nele (o que o "devcon install" faz), pela SetupAPI.
$DeviceCode = @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

public static class TelaDoisVdd
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SpDevinfoData
    {
        public uint CbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    private const uint DicdGenerateId = 0x1;
    private const uint SpdrpHardwareId = 0x1;
    private const uint DifRegisterDevice = 0x19;
    private const uint InstallFlagForce = 0x1;

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid classGuid, IntPtr parent);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiCreateDeviceInfoW(IntPtr set, string name, ref Guid classGuid, string description, IntPtr parent, uint flags, ref SpDevinfoData data);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiSetDeviceRegistryPropertyW(IntPtr set, ref SpDevinfoData data, uint property, byte[] buffer, uint size);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiCallClassInstaller(uint function, IntPtr set, ref SpDevinfoData data);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("newdev.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool UpdateDriverForPlugAndPlayDevicesW(IntPtr parent, string hardwareId, string infPath, uint flags, out bool rebootRequired);

    /// <summary>Cria o dispositivo com o hardware ID dado e instala o .inf nele. Devolve true se o Windows pedir reinício.</summary>
    public static bool Install(string hardwareId, string infPath)
    {
        var displayClass = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318");
        var set = SetupDiCreateDeviceInfoList(ref displayClass, IntPtr.Zero);
        if (set == new IntPtr(-1)) throw new Win32Exception();
        try
        {
            var data = new SpDevinfoData { CbSize = (uint)Marshal.SizeOf<SpDevinfoData>() };
            if (!SetupDiCreateDeviceInfoW(set, "Display", ref displayClass, null, IntPtr.Zero, DicdGenerateId, ref data)) throw new Win32Exception();
            var ids = Encoding.Unicode.GetBytes(hardwareId + "\0\0");
            if (!SetupDiSetDeviceRegistryPropertyW(set, ref data, SpdrpHardwareId, ids, (uint)ids.Length)) throw new Win32Exception();
            if (!SetupDiCallClassInstaller(DifRegisterDevice, set, ref data)) throw new Win32Exception();
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }
        bool reboot;
        if (!UpdateDriverForPlugAndPlayDevicesW(IntPtr.Zero, hardwareId, infPath, InstallFlagForce, out reboot)) throw new Win32Exception();
        return reboot;
    }
}
'@

$work = Join-Path $env:TEMP "teladois-vdd-$([guid]::NewGuid())"
New-Item -ItemType Directory $work | Out-Null
try {
    $zip = Join-Path $work $Asset
    Write-Host "Baixando $Asset ($Version)..."
    Invoke-WebRequest -Uri $Url -OutFile $zip -UseBasicParsing
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash
    if ($hash -ne $Sha256) { throw "SHA-256 diferente do esperado ($hash): arquivo não confiável, nada foi instalado." }

    $files = Join-Path $work 'driver'
    Expand-Archive $zip -DestinationPath $files
    $inf = Get-ChildItem $files -Recurse -Filter 'MttVDD.inf' | Select-Object -First 1
    if (-not $inf) { throw 'MttVDD.inf não está no pacote.' }
    $cat = Get-ChildItem $inf.DirectoryName -Filter '*.cat' | Select-Object -First 1
    if (-not $cat) { throw 'O catálogo de assinatura (.cat) não está no pacote.' }
    $signature = Get-AuthenticodeSignature $cat.FullName
    if ($signature.Status -ne 'Valid') { throw "Assinatura do driver inválida ($($signature.Status)): nada foi instalado." }
    Write-Host "Assinado por: $($signature.SignerCertificate.Subject)"

    New-Item -ItemType Directory -Force $ConfigDir | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'vdd_settings.xml') (Join-Path $ConfigDir 'vdd_settings.xml') -Force
    Write-Host "Configuração gravada em $ConfigDir\vdd_settings.xml"

    $existing = Get-PnpDevice -Class Display -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $HardwareId }
    if ($existing) {
        Write-Host 'O monitor virtual já existe: atualizando o driver.'
        pnputil /add-driver $inf.FullName /install | Write-Host
        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) { throw "pnputil falhou (código $LASTEXITCODE)." }
    } else {
        Add-Type -TypeDefinition $DeviceCode
        if ([TelaDoisVdd]::Install($HardwareId, $inf.FullName)) { Write-Host 'O Windows pediu reinício para terminar a instalação.' }
    }
    Write-Host 'Monitor virtual instalado. Abra (ou reabra) o TelaDois: ele tira o monitor do desktop até o iPhone conectar.'
}
finally {
    Remove-Item -Recurse -Force $work
}
```

- [ ] **Step 5: Conferir a sintaxe do script (sem rodar)**

```powershell
$errors = $null; [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path tools\vdd\instalar-vdd.ps1), [ref]$null, [ref]$errors) | Out-Null; $errors.Count
[xml](Get-Content -Raw tools\vdd\vdd_settings.xml) | Out-Null; "xml ok"
```

Esperado: `0` e `xml ok`.

- [ ] **Step 6: Commit**

```bash
git add tools/vdd/vdd_settings.xml tools/vdd/instalar-vdd.ps1
```
Mensagem: `tools: instalador do Virtual Display Driver 25.7.23 (versão e SHA-256 fixos, só o pacote do driver)`.

- [ ] **Step 7: O usuário instala (administrador)**

Pedir ao usuário que abra o PowerShell **como administrador** na pasta do projeto e rode:

```powershell
powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1
```

Esperado: "Assinado por: …Microsoft…", "Monitor virtual instalado.". O Windows pode ligar o monitor novo no desktop na hora (normal; o TelaDois o desliga ao abrir).

- [ ] **Step 8: Conferir a instalação (sem administrador)**

```powershell
Get-PnpDevice -Class Display | Select-Object Status, FriendlyName, InstanceId
[System.IO.Directory]::GetFiles('\\.\pipe\') | Select-String MTTVirtualDisplayPipe
```

Esperado: uma linha `OK  Virtual Display Driver  ROOT\DISPLAY\0000` (ou parecido) e o pipe listado. Anotar o `InstanceId` (usado no teste sem driver da Task 10).

---

### Task 2: `VirtualDisplay` — achar, ligar e desligar o monitor do driver (+ validação no driver real)

**Files:**
- Create: `windows/src/TelaDois.Core/Display/IDisplayApi.cs`
- Create: `windows/src/TelaDois.Core/Display/Win32DisplayApi.cs`
- Create: `windows/src/TelaDois.Core/Display/DriverPipe.cs`
- Create: `windows/src/TelaDois.Core/Display/IVirtualMonitor.cs`
- Create: `windows/src/TelaDois.Core/Display/VirtualDisplay.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeDisplayApi.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayTests.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/DriverPipeTests.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayLocalTests.cs`

**Interfaces:**
- Consumes: `DisplayCatalog.Enumerate()`, `FfmpegLocator.Find()`, `SystemProcessRunner`, `EncoderProfiles.Candidates`, `FfmpegArguments.Probe` (5a, só no teste local).
- Produces:
  - `public interface IDisplayApi { IReadOnlyList<DisplayDeviceInfo> Devices(); IReadOnlyList<DisplayModeInfo> Modes(string deviceName); DisplayBounds? Bounds(string deviceName); int Apply(string deviceName, int x, int y, int width, int height, int frequency); }` (`Apply` devolve 0 em sucesso; largura 0 = tirar do desktop)
  - `public sealed record DisplayDeviceInfo(string DeviceName, string Description, bool AttachedToDesktop, bool IsPrimary)`
  - `public readonly record struct DisplayModeInfo(int Width, int Height, int Frequency)`
  - `public readonly record struct DisplayBounds(int X, int Y, int Width, int Height)`
  - `public sealed class DisplayChangeException(int code, string action) : Exception` com `int Code`
  - `public sealed class Win32DisplayApi(bool persist) : IDisplayApi`
  - `public interface IDriverPipe { Task<bool> PingAsync(CancellationToken ct); }` e `public sealed class DriverPipe(string name = "MTTVirtualDisplayPipe") : IDriverPipe` com `TimeSpan Timeout { get; init; }` (padrão 1 s)
  - `public interface IVirtualMonitor { Task<bool> IsInstalledAsync(CancellationToken ct); (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height); void Detach(string deviceName); bool IsAttached(string deviceName); }`
  - `public sealed class VirtualDisplay(IDisplayApi api, IDriverPipe pipe) : IVirtualMonitor` com `const string AdapterName = "Virtual Display Driver"`, `const int Fps = 60`, `string? Find()`, `string? DetachLeftover()` (devolve o `\\.\DISPLAYn` desligado ou `null`) e `static DisplayModeInfo? ChooseMode(IEnumerable<DisplayModeInfo> modes, int width, int height)`

- [ ] **Step 1: Escrever o falso `FakeDisplayApi`**

`windows/tests/TelaDois.Core.Tests/Fakes/FakeDisplayApi.cs`:

```csharp
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Windows de mentira: lista de monitores, modos e posições; <see cref="Apply"/> registra a chamada e liga/desliga.</summary>
internal sealed class FakeDisplayApi : IDisplayApi
{
    public List<DisplayDeviceInfo> DeviceList { get; } = [];
    public Dictionary<string, List<DisplayModeInfo>> ModeList { get; } = [];
    public Dictionary<string, DisplayBounds> BoundsList { get; } = [];
    public List<(string Device, int X, int Y, int Width, int Height, int Frequency)> Applied { get; } = [];

    /// <summary>Código devolvido pelo <see cref="Apply"/> (0 = sucesso).</summary>
    public int Result { get; set; }

    public IReadOnlyList<DisplayDeviceInfo> Devices() => DeviceList;

    public IReadOnlyList<DisplayModeInfo> Modes(string deviceName) =>
        ModeList.TryGetValue(deviceName, out var modes) ? modes : [];

    public DisplayBounds? Bounds(string deviceName) =>
        BoundsList.TryGetValue(deviceName, out var bounds) ? bounds : null;

    public int Apply(string deviceName, int x, int y, int width, int height, int frequency)
    {
        Applied.Add((deviceName, x, y, width, height, frequency));
        if (Result == 0)
        {
            var index = DeviceList.FindIndex(d => d.DeviceName == deviceName);
            DeviceList[index] = DeviceList[index] with { AttachedToDesktop = width > 0 };
        }
        return Result;
    }
}
```

- [ ] **Step 2: Escrever os testes do `VirtualDisplay`**

`windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayTests.cs`:

```csharp
using TelaDois.Core.Display;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests.Display;

public class VirtualDisplayTests
{
    private const string Virtual = @"\\.\DISPLAY3";

    private sealed class NoPipe : IDriverPipe
    {
        public Task<bool> PingAsync(CancellationToken ct) => Task.FromResult(false);
    }

    /// <summary>Principal 1600×900 em (0,0), outro 1920×1080 à direita, e o do driver desligado.</summary>
    private static FakeDisplayApi TwoMonitorsAndDriver()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY2", "AMD Radeon(TM) Graphics", true, false));
        api.DeviceList.Add(new DisplayDeviceInfo(Virtual, "Virtual Display Driver", false, false));
        api.BoundsList[@"\\.\DISPLAY1"] = new DisplayBounds(0, 0, 1600, 900);
        api.BoundsList[@"\\.\DISPLAY2"] = new DisplayBounds(1600, 0, 1920, 1080);
        api.ModeList[Virtual] = [new(1920, 1080, 60), new(2556, 1179, 60), new(2622, 1206, 60)];
        return api;
    }

    [Fact]
    public void FindsTheDriverMonitorByAdapterName() =>
        Assert.Equal(Virtual, new VirtualDisplay(TwoMonitorsAndDriver(), new NoPipe()).Find());

    [Fact]
    public void AttachPutsTheMonitorRightOfTheRightmostOneWithTheIphoneMode()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());

        var (device, mode) = display.Attach(2556, 1179);

        Assert.Equal(Virtual, device);
        Assert.Equal(new DisplayModeInfo(2556, 1179, 60), mode);
        Assert.Equal((Virtual, 3520, 0, 2556, 1179, 60), Assert.Single(api.Applied));
        Assert.True(display.IsAttached(Virtual));
    }

    [Fact]
    public void AttachFailureCarriesTheWindowsCode()
    {
        var api = TwoMonitorsAndDriver();
        api.Result = -2; // DISP_CHANGE_BADMODE
        var error = Assert.Throws<DisplayChangeException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
        Assert.Equal(-2, error.Code);
    }

    [Fact]
    public void AttachWithoutTheDriverMonitorThrows()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
        Assert.Throws<InvalidOperationException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
    }

    [Fact]
    public void DetachSendsSizeZero()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());
        display.Attach(2556, 1179);
        display.Detach(Virtual);
        Assert.Equal((Virtual, 0, 0, 0, 0, 0), api.Applied.Last());
        Assert.False(display.IsAttached(Virtual));
    }

    [Fact]
    public void DetachLeftoverOnlyActsWhenTheMonitorIsOnTheDesktop()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());
        Assert.Null(display.DetachLeftover());
        Assert.Empty(api.Applied);

        display.Attach(2556, 1179);
        Assert.Equal(Virtual, display.DetachLeftover());
        Assert.False(display.IsAttached(Virtual));
    }

    [Fact]
    public void ChooseModePrefersExactSizeAt60Hz() =>
        Assert.Equal(new DisplayModeInfo(2556, 1179, 60),
            VirtualDisplay.ChooseMode([new(2556, 1179, 30), new(2556, 1179, 60), new(1920, 1080, 60)], 2556, 1179));

    [Fact]
    public void ChooseModeFallsBackToTheClosestAspectRatio() =>
        Assert.Equal(new DisplayModeInfo(2622, 1206, 60),
            VirtualDisplay.ChooseMode([new(1920, 1080, 60), new(2622, 1206, 60), new(3840, 2160, 60)], 2556, 1179));

    [Fact]
    public void ChooseModeWithoutModesIsNull() => Assert.Null(VirtualDisplay.ChooseMode([], 2556, 1179));
}
```

- [ ] **Step 3: Escrever os testes do `DriverPipe`**

`windows/tests/TelaDois.Core.Tests/Display/DriverPipeTests.cs`:

```csharp
using System.IO.Pipes;
using System.Text;
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

public class DriverPipeTests
{
    private static string UniqueName() => $"teladois-teste-{Guid.NewGuid():N}";

    /// <summary>Faz o papel do driver: lê o comando (UTF-16) e responde <paramref name="reply"/> em ASCII.</summary>
    private static async Task<string> ServeOnceAsync(NamedPipeServerStream server, string reply)
    {
        await server.WaitForConnectionAsync();
        var buffer = new byte[256];
        var read = await server.ReadAsync(buffer);
        await server.WriteAsync(Encoding.ASCII.GetBytes(reply));
        await server.FlushAsync();
        return Encoding.Unicode.GetString(buffer, 0, read);
    }

    [Fact]
    public async Task TrueWhenTheDriverAnswersPong()
    {
        var name = UniqueName();
        await using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var serving = ServeOnceAsync(server, "PONG");
        Assert.True(await new DriverPipe(name).PingAsync(CancellationToken.None));
        Assert.Equal("PING", await serving.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task FalseWhenThePipeAnswersSomethingElse()
    {
        var name = UniqueName();
        var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var serving = Task.Run(async () =>
        {
            await ServeOnceAsync(server, "ERRO");
            await server.DisposeAsync(); // fecha sem PONG
        });
        Assert.False(await new DriverPipe(name).PingAsync(CancellationToken.None));
        await serving.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FalseWithoutTheDriver() =>
        Assert.False(await new DriverPipe(UniqueName()) { Timeout = TimeSpan.FromMilliseconds(200) }.PingAsync(CancellationToken.None));
}
```

- [ ] **Step 4: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~TelaDois.Core.Tests.Display"`
Expected: FALHA de compilação — `IDisplayApi`, `VirtualDisplay`, `DriverPipe` não existem.

- [ ] **Step 5: Escrever `IDisplayApi.cs`**

`windows/src/TelaDois.Core/Display/IDisplayApi.cs`:

```csharp
namespace TelaDois.Core.Display;

/// <summary>Um monitor do Windows (<c>EnumDisplayDevices</c>): nome <c>\\.\DISPLAYn</c> e o adaptador que o fornece.</summary>
public sealed record DisplayDeviceInfo(string DeviceName, string Description, bool AttachedToDesktop, bool IsPrimary);

public readonly record struct DisplayModeInfo(int Width, int Height, int Frequency);

/// <summary>Posição e tamanho de um monitor no desktop.</summary>
public readonly record struct DisplayBounds(int X, int Y, int Width, int Height);

/// <summary>As chamadas de monitor do Windows que o TelaDois usa (atrás de uma interface para testar sem Windows).</summary>
public interface IDisplayApi
{
    IReadOnlyList<DisplayDeviceInfo> Devices();

    IReadOnlyList<DisplayModeInfo> Modes(string deviceName);

    /// <summary>Posição e tamanho atuais; <c>null</c> se o monitor não está no desktop.</summary>
    DisplayBounds? Bounds(string deviceName);

    /// <summary>Põe o monitor no desktop nessa posição e modo (largura 0 = tira do desktop). 0 = sucesso; senão o código <c>DISP_CHANGE_*</c>.</summary>
    int Apply(string deviceName, int x, int y, int width, int height, int frequency);
}

public sealed class DisplayChangeException(int code, string action)
    : Exception($"o Windows não deixou {action} o monitor virtual (código {code})")
{
    public int Code { get; } = code;
}
```

- [ ] **Step 6: Escrever `Win32DisplayApi.cs`**

`windows/src/TelaDois.Core/Display/Win32DisplayApi.cs`:

```csharp
using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

/// <summary>
/// <see cref="IDisplayApi"/> com o <c>user32</c>. Não precisa de administrador. Com <paramref name="persist"/>, ligar e
/// desligar ficam gravados no registro do Windows (valem depois de reiniciar); sem, valem só até reiniciar.
/// </summary>
public sealed class Win32DisplayApi(bool persist) : IDisplayApi
{
    private const int AttachedToDesktop = 0x1;
    private const int PrimaryDevice = 0x4;
    private const int DmPosition = 0x20;
    private const int DmPelsWidth = 0x80000;
    private const int DmPelsHeight = 0x100000;
    private const int DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1;
    private const uint CdsNoReset = 0x10000000;
    private const int EnumCurrentSettings = -1;

    public IReadOnlyList<DisplayDeviceInfo> Devices()
    {
        var devices = new List<DisplayDeviceInfo>();
        for (uint index = 0; ; index++)
        {
            var device = new DisplayDevice { cb = Marshal.SizeOf<DisplayDevice>() };
            if (!EnumDisplayDevices(null, index, ref device, 0))
            {
                return devices;
            }
            devices.Add(new DisplayDeviceInfo(device.DeviceName, device.DeviceString,
                (device.StateFlags & AttachedToDesktop) != 0, (device.StateFlags & PrimaryDevice) != 0));
        }
    }

    public IReadOnlyList<DisplayModeInfo> Modes(string deviceName)
    {
        var modes = new List<DisplayModeInfo>();
        for (var index = 0; ; index++)
        {
            var mode = NewMode();
            if (!EnumDisplaySettings(deviceName, index, ref mode))
            {
                return modes;
            }
            modes.Add(new DisplayModeInfo(mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency));
        }
    }

    public DisplayBounds? Bounds(string deviceName)
    {
        var mode = NewMode();
        return EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode) && mode.dmPelsWidth > 0
            ? new DisplayBounds(mode.dmPositionX, mode.dmPositionY, mode.dmPelsWidth, mode.dmPelsHeight)
            : null;
    }

    public int Apply(string deviceName, int x, int y, int width, int height, int frequency)
    {
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight | (frequency > 0 ? DmDisplayFrequency : 0);
        mode.dmPositionX = x;
        mode.dmPositionY = y;
        mode.dmPelsWidth = width;
        mode.dmPelsHeight = height;
        mode.dmDisplayFrequency = frequency;
        if (!persist)
        {
            return ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, 0, IntPtr.Zero);
        }
        var staged = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsUpdateRegistry | CdsNoReset, IntPtr.Zero);
        return staged != 0 ? staged : ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    }

    private static DevMode NewMode() => new() { dmSize = (short)Marshal.SizeOf<DevMode>() };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string? device, uint index, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode mode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref DevMode mode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, IntPtr mode, IntPtr hwnd, uint flags, IntPtr param);
}
```

- [ ] **Step 7: Escrever `DriverPipe.cs`**

`windows/src/TelaDois.Core/Display/DriverPipe.cs`:

```csharp
using System.IO.Pipes;
using System.Text;

namespace TelaDois.Core.Display;

public interface IDriverPipe
{
    /// <summary>true se o driver respondeu <c>PONG</c>; false se não está instalado, parado ou não respondeu a tempo.</summary>
    Task<bool> PingAsync(CancellationToken ct);
}

/// <summary>
/// Pipe do Virtual Display Driver (<c>\\.\pipe\MTTVirtualDisplayPipe</c>): comandos em UTF-16, respostas em ASCII.
/// O TelaDois só manda <c>PING</c> (o pipe aceita qualquer usuário do PC — ver revisão de segurança da Etapa 6).
/// </summary>
public sealed class DriverPipe(string name = "MTTVirtualDisplayPipe") : IDriverPipe
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(1);

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(Timeout);
        try
        {
            await using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(limit.Token);
            await pipe.WriteAsync(Encoding.Unicode.GetBytes("PING"), limit.Token);
            var buffer = new byte[256];
            var reply = new StringBuilder();
            int read;
            while ((read = await pipe.ReadAsync(buffer, limit.Token)) > 0)
            {
                reply.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (reply.ToString().Contains("PONG", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception e) when (e is OperationCanceledException && !ct.IsCancellationRequested || e is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
```

- [ ] **Step 8: Escrever `IVirtualMonitor.cs` e `VirtualDisplay.cs`**

`windows/src/TelaDois.Core/Display/IVirtualMonitor.cs`:

```csharp
namespace TelaDois.Core.Display;

/// <summary>O monitor extra da Segunda Tela, como a fábrica de vídeo o usa.</summary>
public interface IVirtualMonitor
{
    Task<bool> IsInstalledAsync(CancellationToken ct);

    /// <summary>Põe o monitor no desktop em Estender com o modo mais próximo de <paramref name="width"/>×<paramref name="height"/>.</summary>
    /// <exception cref="InvalidOperationException">O monitor do driver não aparece no Windows.</exception>
    /// <exception cref="DisplayChangeException">O Windows recusou a mudança.</exception>
    (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height);

    void Detach(string deviceName);

    bool IsAttached(string deviceName);
}
```

`windows/src/TelaDois.Core/Display/VirtualDisplay.cs`:

```csharp
namespace TelaDois.Core.Display;

/// <summary>
/// O monitor do Virtual Display Driver. O driver mantém 1 monitor sempre instalado (<c>SETDISPLAYCOUNT 0</c> vira 1);
/// ligar é pô-lo no desktop em Estender, à direita do monitor mais à direita; desligar é tirá-lo (tamanho 0), e o
/// Windows devolve as janelas aos outros monitores.
/// </summary>
public sealed class VirtualDisplay(IDisplayApi api, IDriverPipe pipe) : IVirtualMonitor
{
    /// <summary>Nome do adaptador no Windows (<c>DeviceName</c> do <c>MttVDD.inf</c>); conferido no driver real (Task 2).</summary>
    public const string AdapterName = "Virtual Display Driver";

    public const int Fps = 60;

    public Task<bool> IsInstalledAsync(CancellationToken ct) => pipe.PingAsync(ct);

    /// <summary><c>\\.\DISPLAYn</c> do monitor do driver, ligado ou não; <c>null</c> sem o driver.</summary>
    public string? Find() => api.Devices().FirstOrDefault(IsVirtual)?.DeviceName;

    public bool IsAttached(string deviceName) =>
        api.Devices().Any(d => d.DeviceName == deviceName && d.AttachedToDesktop);

    public (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height)
    {
        var device = Find() ?? throw new InvalidOperationException("o monitor do Virtual Display Driver não aparece no Windows");
        var mode = ChooseMode(api.Modes(device), width, height) ?? throw new InvalidOperationException("o monitor virtual não informou nenhuma resolução");
        var right = api.Devices()
            .Where(d => d.AttachedToDesktop && d.DeviceName != device)
            .Select(d => api.Bounds(d.DeviceName))
            .Where(b => b.HasValue)
            .Select(b => b!.Value.X + b.Value.Width)
            .DefaultIfEmpty(0)
            .Max();
        Check(api.Apply(device, right, 0, mode.Width, mode.Height, mode.Frequency), "ligar");
        return (device, mode);
    }

    public void Detach(string deviceName) => Check(api.Apply(deviceName, 0, 0, 0, 0, 0), "desligar");

    /// <summary>Ao abrir o TelaDois: tira do desktop o monitor virtual que sobrou ligado. Devolve qual, ou <c>null</c>.</summary>
    public string? DetachLeftover()
    {
        var leftover = api.Devices().FirstOrDefault(d => IsVirtual(d) && d.AttachedToDesktop);
        if (leftover is null)
        {
            return null;
        }
        Detach(leftover.DeviceName);
        return leftover.DeviceName;
    }

    /// <summary>O tamanho pedido (preferindo 60 Hz); senão a proporção mais próxima, o maior e o mais perto de 60 Hz.</summary>
    public static DisplayModeInfo? ChooseMode(IEnumerable<DisplayModeInfo> modes, int width, int height)
    {
        var candidates = modes.Where(m => m.Width > 0 && m.Height > 0).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }
        var target = (double)width / height;
        return candidates
            .OrderBy(m => m.Width == width && m.Height == height ? 0 : 1)
            .ThenBy(m => Math.Round(Math.Abs((double)m.Width / m.Height - target), 3))
            .ThenByDescending(m => (long)m.Width * m.Height)
            .ThenBy(m => Math.Abs(m.Frequency - Fps))
            .First();
    }

    private static bool IsVirtual(DisplayDeviceInfo device) =>
        device.Description.Contains(AdapterName, StringComparison.OrdinalIgnoreCase);

    private static void Check(int code, string action)
    {
        if (code != 0)
        {
            throw new DisplayChangeException(code, action);
        }
    }
}
```

- [ ] **Step 9: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~TelaDois.Core.Tests.Display"`
Expected: PASS (12 testes).

- [ ] **Step 10: Escrever o teste local com o driver real (validação dos riscos)**

`windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayLocalTests.cs`:

```csharp
using TelaDois.Core.Display;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Display;

/// <summary>Com o driver real (pisca o monitor extra por ~1 s). Retorna cedo onde o driver não está instalado (CI).</summary>
[Trait("Category", "Local")]
public class VirtualDisplayLocalTests
{
    [Fact]
    public async Task RealDriverMonitorAttachesShowsUpInDxgiAndDetaches()
    {
        var display = new VirtualDisplay(new Win32DisplayApi(persist: false), new DriverPipe());
        if (!await display.IsInstalledAsync(CancellationToken.None))
        {
            return; // sem o Virtual Display Driver nesta máquina
        }
        var device = display.Find();
        Assert.NotNull(device); // o nome do adaptador confere com VirtualDisplay.AdapterName
        try
        {
            var (name, mode) = display.Attach(2556, 1179);
            Assert.Equal(new DisplayModeInfo(2556, 1179, 60), mode);
            Assert.True(display.IsAttached(name));

            // A DXGI demora um pouco a mostrar a saída nova.
            DisplayOutput? output = null;
            for (var i = 0; i < 20 && output is null; i++)
            {
                output = DisplayCatalog.Enumerate().FirstOrDefault(o => o.DeviceName == name);
                if (output is null)
                {
                    await Task.Delay(100);
                }
            }
            Assert.NotNull(output); // está no adaptador 0, o único que o ddagrab enxerga
            Assert.Equal((2556, 1179), (output!.Width, output.Height));

            if (FfmpegLocator.Find() is { } ffmpeg)
            {
                var x264 = EncoderProfiles.Candidates.Single(p => p.Id == "x264");
                await using var probe = new SystemProcessRunner().Start(ffmpeg, FfmpegArguments.Probe(x264, output.OutputIndex));
                Assert.Equal(0, await probe.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15)));
            }
        }
        finally
        {
            if (display.Find() is { } attached && display.IsAttached(attached))
            {
                display.Detach(attached);
            }
        }
        Assert.False(display.IsAttached(device!));
    }
}
```

- [ ] **Step 11: Rodar o teste local e registrar o que o driver real faz**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~VirtualDisplayLocalTests" --logger "console;verbosity=detailed"`

Expected: PASS. Conferir também à mão: `Get-CimInstance Win32_VideoController | Select-Object Name` mostra "Virtual Display Driver". Decisões conforme o resultado:
- **`Find()` nulo** → o nome do adaptador é outro: ajustar `VirtualDisplay.AdapterName` ao `DeviceString` real (listar com um teste temporário que imprime `new Win32DisplayApi(false).Devices()`), rodar de novo.
- **`DisplayChangeException` ao ligar com `persist: false`** → trocar para `persist: true` no teste; se passar, a Task 9 usa `PersistVirtualMonitor = true`. Se passar com `false`, a Task 9 usa `false`.
- **`output` nulo** (o monitor não aparece no adaptador 0 da DXGI) → registrar no CLAUDE.md; o app vai cair em Duplicar (já previsto na Task 8). Tentar `<friendlyname>` com o nome da placa no `C:\VirtualDisplayDriver\vdd_settings.xml` + reiniciar o driver no Gerenciador de Dispositivos e rodar de novo.
- **FFmpeg falha** no monitor virtual → anotar a saída de erro; investigar antes da Task 8.

Anotar os resultados (nome do adaptador, `persist`, índice DXGI) para o CLAUDE.md da Task 10.

- [ ] **Step 12: Rodar todos os testes do PC**

Run: `dotnet test windows/TelaDois.sln`
Expected: PASS (97 da 5a + os novos).

- [ ] **Step 13: Commit**

```bash
git add windows/src/TelaDois.Core/Display/IDisplayApi.cs windows/src/TelaDois.Core/Display/Win32DisplayApi.cs windows/src/TelaDois.Core/Display/DriverPipe.cs windows/src/TelaDois.Core/Display/IVirtualMonitor.cs windows/src/TelaDois.Core/Display/VirtualDisplay.cs windows/tests/TelaDois.Core.Tests/Fakes/FakeDisplayApi.cs windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayTests.cs windows/tests/TelaDois.Core.Tests/Display/DriverPipeTests.cs windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayLocalTests.cs
```
Mensagem: `feat(windows): VirtualDisplay liga e desliga o monitor do Virtual Display Driver (testado no driver real)`.

---

### Task 3: Protocolo — `VIDEO_END` (tipo 8) nos dois lados

**Files:**
- Modify: `windows/src/TelaDois.Core/Link/LinkFrame.cs` (enum)
- Modify: `windows/src/TelaDois.Core/Link/LinkMessages.cs`
- Modify: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift`
- Test: `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift`

**Interfaces:**
- Produces: C# `LinkMessageType.VideoEnd = 8`, `LinkMessages.VideoEnd()`; Swift `LinkMessageType.videoEnd` (= 8), `LinkProtocol.videoEnd() -> LinkFrame`.

- [ ] **Step 1: Testes em C#**

Em `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs`, depois de `VideoMatchesGoldenBytes`:

```csharp
    [Fact]
    public void VideoEndMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("0800000000"), LinkMessages.VideoEnd().Encode());

    [Fact]
    public void VideoEndIsAKnownType() =>
        Assert.Equal(LinkMessageType.VideoEnd, new LinkFrame(8, []).MessageType);
```

- [ ] **Step 2: Testes em Swift**

Em `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`, depois de `testVideoMatchesGoldenBytes`:

```swift
    func testVideoEndMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.videoEnd().encoded(), hex("0800000000"))
        XCTAssertEqual(LinkFrame(type: 8).messageType, .videoEnd)
    }
```

Em `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift`, depois de `testOtherFramesNeedNoReply`:

```swift
    func testVideoEndNeedsNoReplyAndKeepsTheSessionAlive() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.videoEnd(), now: t0.addingTimeInterval(2)), [])
        XCTAssertFalse(state.isTimedOut(now: t0.addingTimeInterval(4.5)))
    }
```

- [ ] **Step 3: Ver o C# falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~LinkProtocolTests"`
Expected: FALHA de compilação — `LinkMessages.VideoEnd` não existe.

- [ ] **Step 4: Implementar em C#**

Em `LinkFrame.cs`, no enum, depois de `Bye = 7,`:

```csharp
    VideoEnd = 8,
```

Em `LinkMessages.cs`, depois de `public static LinkFrame Bye() => new(LinkMessageType.Bye);`:

```csharp
    /// <summary>O PC parou a Segunda Tela sem desconectar: o iPhone volta ao relógio.</summary>
    public static LinkFrame VideoEnd() => new(LinkMessageType.VideoEnd);
```

- [ ] **Step 5: Ver o C# passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~LinkProtocolTests"`
Expected: PASS.

- [ ] **Step 6: Implementar em Swift**

Em `LinkProtocol.swift`, trocar a linha do enum por:

```swift
    case hello = 1, config, video, keyframeRequest, ping, pong, bye, videoEnd
```

E depois de `public static func bye() -> LinkFrame { LinkFrame(.bye) }`:

```swift
    /// O PC parou a Segunda Tela sem desconectar: volta ao relógio.
    public static func videoEnd() -> LinkFrame { LinkFrame(.videoEnd) }
```

- [ ] **Step 7: Commit e CI**

```bash
git add windows/src/TelaDois.Core/Link/LinkFrame.cs windows/src/TelaDois.Core/Link/LinkMessages.cs windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift
```
Mensagem: `feat(protocolo): VIDEO_END (tipo 8) para parar a Segunda Tela sem desconectar`. Depois `git push` e conferir `ios-test` (passo `swift test`) e `windows-test` verdes com `gh run list --branch etapa-5b-monitor-virtual --limit 4` + `gh run view <id> --json status,conclusion`.

---

### Task 4: iPhone — `VIDEO_END` volta ao relógio e não mostra imagem velha (I2)

**Files:**
- Modify: `ios/TelaDois/UsbLink.swift:115-126`
- Modify: `ios/TelaDois/VideoRenderer.swift:23-29`
- Modify: `ios/project.yml:11` (versão)
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift`

**Interfaces:**
- Consumes: `LinkProtocol.videoEnd()` (Task 3).
- Produces: comportamento — `VIDEO_END` zera `videoConfig` (a tela raiz mostra o relógio) e mantém `isConnected`.

- [ ] **Step 1: Escrever o teste de UI**

Em `ios/TelaDoisUITests/RestScreenUITests.swift`, depois de `testVideoFromPcShowsSecondScreenAndByeReturnsToClock`:

```swift
    func testVideoEndReturnsToClockButKeepsTheConnection() throws {
        let app = launch()
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))

        let url = try XCTUnwrap(Bundle(for: Self.self).url(forResource: "bars-640x360", withExtension: "h264"))
        let units = H264Parser.accessUnits(in: [UInt8](try Data(contentsOf: url)))
        pc.send(LinkProtocol.config(width: 640, height: 360, fps: 30))
        for (index, unit) in units.enumerated() {
            pc.send(LinkProtocol.video(keyframe: index == 0, timestamp: UInt64(index) * 33_333, accessUnit: unit))
        }
        XCTAssertTrue(element(app, "second-screen").waitForExistence(timeout: 2), "deveria mostrar a Segunda Tela")

        pc.send(LinkProtocol.videoEnd())
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 2), "VIDEO_END deveria voltar ao relógio")
        XCTAssertFalse(element(app, "second-screen").exists)
        XCTAssertTrue(element(app, "link-status").exists, "a conexão continua depois do VIDEO_END")

        // Ainda conectado: o PING é respondido.
        pc.send(LinkProtocol.ping(7))
        var reply = pc.readFrame()
        while let frame = reply, frame.messageType != .pong { reply = pc.readFrame() }
        XCTAssertEqual(reply.flatMap(LinkProtocol.timestamp(of:)), 7)
        attachScreenshot("07-fim-do-video")
        pc.send(LinkProtocol.bye())
    }
```

- [ ] **Step 2: Enviar só o teste e ver falhar no CI**

Esperar o `ios-test` da Task 3 terminar (regra do `cancel-in-progress`). Depois:

```bash
git add ios/TelaDoisUITests/RestScreenUITests.swift
```
Mensagem: `test(ui): VIDEO_END volta ao relógio e mantém a conexão (ainda sem implementação)`. `git push`; esperar o `ios-test`.
Expected: FALHA em `testVideoEndReturnsToClockButKeepsTheConnection` com "VIDEO_END deveria voltar ao relógio".

- [ ] **Step 3: Implementar no `UsbLink`**

Em `ios/TelaDois/UsbLink.swift`, no `switch frame.messageType` de `handle(_:)`, depois do `case .video:`:

```swift
            case .videoEnd:
                // O PC desligou a Segunda Tela (atalho ou bandeja): volta ao relógio, conexão continua.
                if videoConfig != nil {
                    videoConfig = nil
                    renderer.reset()
                }
```

- [ ] **Step 4: Implementar a correção I2 no `VideoRenderer`**

Em `ios/TelaDois/VideoRenderer.swift`, trocar o `reset()` por:

```swift
    func reset() {
        queue.async {
            self.format = nil
            self.waitingForKeyframe = true
            // Tira também a imagem na tela: ao religar, não aparece a última imagem da vez anterior.
            self.layer.sampleBufferRenderer.flush(removingDisplayedImage: true, completionHandler: nil)
        }
    }
```

- [ ] **Step 5: Subir a versão do app**

Em `ios/project.yml`, trocar `MARKETING_VERSION: "0.3.0"` por:

```yaml
    MARKETING_VERSION: "0.4.0"
```

- [ ] **Step 6: Commit, CI e prints**

```bash
git add ios/TelaDois/UsbLink.swift ios/TelaDois/VideoRenderer.swift ios/project.yml
```
Mensagem: `feat(ios): VIDEO_END volta ao relógio sem desconectar; flush tira a imagem velha (I2)`. `git push`; esperar `ios-test` e `ios-build` verdes. Baixar os prints (`gh run download <id> -n screenshots -D <pasta vazia do scratchpad>`) e **revisar** `07-fim-do-video.png` (relógio + "PC conectado", sem resto de vídeo) e `05-segunda-tela.png` (barras em tela cheia).

---

### Task 5: PC — `AccessUnitReader` reinsere SPS/PPS antes de IDR (I3)

**Files:**
- Modify: `windows/src/TelaDois.Core/Video/H264.cs`
- Modify: `windows/src/TelaDois.Core/Video/AccessUnitReader.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/AccessUnitReaderTests.cs`

**Interfaces:**
- Produces: `H264.Sps = 7`, `H264.Pps = 8`; cada `AccessUnit` com IDR sai com SPS e PPS logo depois do AUD (os últimos vistos, se a imagem veio sem eles).

- [ ] **Step 1: Escrever os testes**

Em `AccessUnitReaderTests.cs`, depois de `ScanFindsThreeAndFourByteStartCodes`:

```csharp
    private static readonly byte[] Aud = Convert.FromHexString("0000000109F0");
    private static readonly byte[] Sps = Convert.FromHexString("000000016742001E");
    private static readonly byte[] Pps = Convert.FromHexString("0000000168CE3880");
    private static readonly byte[] Idr = Convert.FromHexString("0000000165888480");
    private static readonly byte[] Slice = Convert.FromHexString("00000001419A80");

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    [Fact]
    public async Task ReinsertsLastParameterSetsBeforeAnIdrThatCameWithout()
    {
        // Como o h264_amf: SPS/PPS só na primeira imagem completa (medido no PC AMD em 2026-10-19).
        var units = await ReadAll(new MemoryStream(Concat(Aud, Sps, Pps, Idr, Aud, Slice, Aud, Idr)));
        Assert.Equal(3, units.Count);
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), units[0].Data);
        Assert.Equal(Concat(Aud, Slice), units[1].Data);
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), units[2].Data);
        Assert.True(units[2].IsKeyframe);
    }

    [Fact]
    public async Task OnlyAddsTheMissingParameterSet()
    {
        // Veio um SPS novo sem PPS: só o PPS guardado entra, logo depois do AUD (antes do SPS que veio).
        var newSps = Convert.FromHexString("000000016742001F");
        var units = await ReadAll(new MemoryStream(Concat(Aud, Sps, Pps, Idr, Aud, newSps, Idr)));
        Assert.Equal(Concat(Aud, Pps, newSps, Idr), units[1].Data);
    }

    [Fact]
    public async Task LeavesAnIdrAloneWhenNoParameterSetWasSeenYet()
    {
        var units = await ReadAll(new MemoryStream(Concat(Aud, Idr, Aud, Slice)));
        Assert.Equal(Concat(Aud, Idr), units[0].Data);
    }
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~AccessUnitReaderTests"`
Expected: FALHA em `ReinsertsLastParameterSetsBeforeAnIdrThatCameWithout` (a 3ª imagem sai sem SPS/PPS) e em `OnlyAddsTheMissingParameterSet`.

- [ ] **Step 3: Implementar**

Em `H264.cs`, depois de `public const int Aud = 9;`:

```csharp
    public const int Sps = 7;
    public const int Pps = 8;
```

Em `AccessUnitReader.cs`: acrescentar à documentação da classe a frase "Toda imagem com IDR sai com SPS e PPS (os últimos vistos são reinseridos logo depois do AUD: o <c>h264_amf</c> só os manda no começo)."; acrescentar os campos depois de `private int _count;`:

```csharp
    private byte[]? _sps;
    private byte[]? _pps;
```

trocar a linha `units.Add(new AccessUnit(_buffer.AsSpan(from, to - from).ToArray(), types.Contains(H264.Idr)));` por:

```csharp
                units.Add(Complete(_buffer.AsSpan(from, to - from).ToArray()));
```

e acrescentar o método antes do fim da classe:

```csharp
    /// <summary>Guarda o SPS/PPS vistos e reinsere, logo depois do AUD, os que faltarem numa imagem com IDR.</summary>
    private AccessUnit Complete(byte[] data)
    {
        var nals = H264.ScanNals(data, data.Length);
        bool hasSps = false, hasPps = false, idr = false;
        for (var i = 0; i < nals.Count; i++)
        {
            var end = i + 1 < nals.Count ? nals[i + 1].Start : data.Length;
            switch (nals[i].Type)
            {
                case H264.Sps:
                    _sps = data[nals[i].Start..end];
                    hasSps = true;
                    break;
                case H264.Pps:
                    _pps = data[nals[i].Start..end];
                    hasPps = true;
                    break;
                case H264.Idr:
                    idr = true;
                    break;
            }
        }
        if (idr && (!hasSps || !hasPps) && _sps is not null && _pps is not null)
        {
            var at = nals.Count > 1 ? nals[1].Start : data.Length;
            data = [.. data[..at], .. hasSps ? Array.Empty<byte>() : _sps, .. hasPps ? Array.Empty<byte>() : _pps, .. data[at..]];
        }
        return new AccessUnit(data, idr);
    }
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~AccessUnitReaderTests"`
Expected: PASS (6 testes).

- [ ] **Step 5: Commit**

```bash
git add windows/src/TelaDois.Core/Video/H264.cs windows/src/TelaDois.Core/Video/AccessUnitReader.cs windows/tests/TelaDois.Core.Tests/Video/AccessUnitReaderTests.cs
```
Mensagem: `fix(video): toda imagem completa sai com SPS/PPS (o AMF só manda no começo) — I3`.

---

### Task 6: PC — `MonitorMode` (liga/desliga) e o contrato `IVideoSwitch`

**Files:**
- Create: `windows/src/TelaDois.Core/Link/IVideoSwitch.cs`
- Create: `windows/src/TelaDois.Core/Display/MonitorMode.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/MonitorModeTests.cs`

**Interfaces:**
- Produces:
  - `public interface IVideoSwitch { bool WantsVideo { get; } Task WaitForChangeAsync(bool seen, CancellationToken ct); void VideoEnded(); }` (namespace `TelaDois.Core.Link`)
  - `public enum ToggleResult { TurnedOn, TurnedOff, NoPhone }`
  - `public sealed class MonitorMode : IVideoSwitch` com `bool IsConnected`, `void SessionStarted()`, `void SessionEnded()`, `ToggleResult Toggle()`, `event Action<bool, bool>? StateChanged` (conectado, ligado; só em mudanças reais, fora da trava) (namespace `TelaDois.Core.Display`)

- [ ] **Step 1: Escrever os testes**

`windows/tests/TelaDois.Core.Tests/Display/MonitorModeTests.cs`:

```csharp
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

public class MonitorModeTests
{
    [Fact]
    public void StartsDisconnectedAndOff()
    {
        var mode = new MonitorMode();
        Assert.False(mode.IsConnected);
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void SessionTurnsOnByItselfAndEndTurnsOff()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        Assert.True(mode.IsConnected);
        Assert.True(mode.WantsVideo);
        mode.SessionEnded();
        Assert.False(mode.IsConnected);
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void ToggleWithoutPhoneChangesNothing()
    {
        var mode = new MonitorMode();
        Assert.Equal(ToggleResult.NoPhone, mode.Toggle());
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void ToggleAlternatesWhileConnected()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        Assert.False(mode.WantsVideo);
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        Assert.True(mode.WantsVideo);
    }

    [Fact]
    public void VideoEndedTurnsOffButKeepsThePhone()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        mode.VideoEnded();
        Assert.True(mode.IsConnected);
        Assert.False(mode.WantsVideo);
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
    }

    [Fact]
    public void StateChangedFiresOnlyOnRealChanges()
    {
        var mode = new MonitorMode();
        var events = new List<(bool, bool)>();
        mode.StateChanged += (connected, on) => events.Add((connected, on));
        mode.SessionStarted();
        mode.SessionStarted();
        mode.VideoEnded();
        mode.VideoEnded();
        mode.SessionEnded();
        Assert.Equal(new List<(bool, bool)> { (true, true), (true, false), (false, false) }, events);
    }

    [Fact]
    public async Task WaitForChangeReturnsAtOnceWhenTheStateIsAlreadyDifferent()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        await mode.WaitForChangeAsync(seen: false, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForChangeCompletesOnToggle()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        var wait = mode.WaitForChangeAsync(seen: true, CancellationToken.None);
        Assert.False(wait.IsCompleted);
        mode.Toggle();
        await wait.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForChangeHonoursCancellation()
    {
        var mode = new MonitorMode();
        using var cts = new CancellationTokenSource();
        var wait = mode.WaitForChangeAsync(seen: false, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~MonitorModeTests"`
Expected: FALHA de compilação — `MonitorMode` não existe.

- [ ] **Step 3: Implementar**

`windows/src/TelaDois.Core/Link/IVideoSwitch.cs`:

```csharp
namespace TelaDois.Core.Link;

/// <summary>Diz à sessão se o vídeo deve estar ligado; a sessão avisa quando o vídeo parou sozinho.</summary>
public interface IVideoSwitch
{
    bool WantsVideo { get; }

    /// <summary>Completa quando o estado deixar de ser <paramref name="seen"/> (na hora, se já for diferente).</summary>
    Task WaitForChangeAsync(bool seen, CancellationToken ct);

    /// <summary>O vídeo parou ou não abriu: passa a desligado (o atalho pode ligar de novo).</summary>
    void VideoEnded();
}
```

`windows/src/TelaDois.Core/Display/MonitorMode.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Display;

public enum ToggleResult
{
    TurnedOn,
    TurnedOff,
    NoPhone,
}

/// <summary>
/// Modo monitor: liga sozinho quando o iPhone conecta, alterna no atalho/bandeja enquanto conectado e desliga quando
/// o iPhone sai ou o vídeo para. Se estende ou duplica é decidido por quem abre o vídeo.
/// </summary>
public sealed class MonitorMode : IVideoSwitch
{
    private readonly object _lock = new();
    private bool _connected;
    private bool _on;
    private TaskCompletionSource _changed = NewSignal();

    /// <summary>(conectado, ligado) a cada mudança real; chamado fora da trava, na thread de quem mudou.</summary>
    public event Action<bool, bool>? StateChanged;

    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _connected;
            }
        }
    }

    public bool WantsVideo
    {
        get
        {
            lock (_lock)
            {
                return _on;
            }
        }
    }

    public void SessionStarted() => Change((_, _) => (true, true));

    public void SessionEnded() => Change((_, _) => (false, false));

    public void VideoEnded() => Change((connected, _) => (connected, false));

    public ToggleResult Toggle()
    {
        var noPhone = false;
        var (_, on) = Change((connected, current) =>
        {
            noPhone = !connected;
            return connected ? (connected, !current) : (connected, current);
        });
        return noPhone ? ToggleResult.NoPhone : on ? ToggleResult.TurnedOn : ToggleResult.TurnedOff;
    }

    public Task WaitForChangeAsync(bool seen, CancellationToken ct)
    {
        Task wait;
        lock (_lock)
        {
            if (_on != seen)
            {
                return Task.CompletedTask;
            }
            wait = _changed.Task;
        }
        return wait.WaitAsync(ct);
    }

    private (bool Connected, bool On) Change(Func<bool, bool, (bool, bool)> next)
    {
        TaskCompletionSource? signal = null;
        bool connected, on;
        lock (_lock)
        {
            (connected, on) = next(_connected, _on);
            if (connected != _connected || on != _on)
            {
                _connected = connected;
                _on = on;
                signal = _changed;
                _changed = NewSignal();
            }
        }
        if (signal is not null)
        {
            signal.TrySetResult();
            StateChanged?.Invoke(connected, on);
        }
        return (connected, on);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~MonitorModeTests"`
Expected: PASS (9 testes).

- [ ] **Step 5: Commit**

```bash
git add windows/src/TelaDois.Core/Link/IVideoSwitch.cs windows/src/TelaDois.Core/Display/MonitorMode.cs windows/tests/TelaDois.Core.Tests/Display/MonitorModeTests.cs
```
Mensagem: `feat(windows): MonitorMode liga/desliga a Segunda Tela (liga ao conectar, alterna no atalho)`.

---

### Task 7: PC — `LinkSession` liga/desliga o vídeo, `CONFIG` com a 1ª imagem (I1), `VIDEO_END`

**Files:**
- Modify: `windows/src/TelaDois.Core/Video/IVideoFeed.cs`
- Modify: `windows/src/TelaDois.Core/Link/LinkSession.cs` (arquivo inteiro)
- Test: `windows/tests/TelaDois.Core.Tests/LinkSessionTests.cs`

**Interfaces:**
- Consumes: `IVideoSwitch`, `MonitorMode`, `ToggleResult` (Task 6); `LinkMessages.VideoEnd()` (Task 3).
- Produces:
  - `public enum VideoKind { Mirror, Extend }` e `IVideoFeed.Kind` (membro padrão `=> VideoKind.Mirror`)
  - `public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null, VideoKind? Kind = null)`
  - `LinkSession.RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null, IVideoSwitch? videoSwitch = null)` — sem `videoSwitch`, o vídeo fica ligado (como a 5a); um vídeo que para sozinho ou não abre vira `VIDEO_END` (se houve `CONFIG`) e a sessão continua.

- [ ] **Step 1: Ajustar o teste existente que esperava `CONFIG` sem imagem**

Em `LinkSessionTests.cs`, no teste `DisposesFeedAndSaysByeWhenPhoneSendsInvalidData`, trocar
`var feed = new FixedFeed(new VideoConfig(1366, 768, 60), []);`
por:

```csharp
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), [new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true)]);
```

(com I1, o `CONFIG` só sai junto com a 1ª imagem).

- [ ] **Step 2: Escrever os testes novos**

Em `LinkSessionTests.cs`, acrescentar `using TelaDois.Core.Display;` no topo e, antes do fim da classe:

```csharp
    /// <summary>Feed cuja 1ª imagem só sai quando <see cref="Release"/> completar; depois manda imagens a cada 20 ms.</summary>
    private sealed class GatedFeed(VideoConfig config, VideoKind kind = VideoKind.Mirror) : IVideoFeed
    {
        private int _sent;
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }
        public VideoConfig Config => config;
        public VideoKind Kind => kind;

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            await Release.Task.WaitAsync(ct);
            if (Interlocked.Increment(ref _sent) == 1)
            {
                return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true);
            }
            await Task.Delay(20, ct);
            return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x41], false);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Uma imagem e depois para sozinho (como o monitor virtual que saiu do desktop).</summary>
    private sealed class OneFrameThenStopsFeed : IVideoFeed
    {
        private int _calls;
        public bool Disposed { get; private set; }
        public VideoConfig Config => new(1366, 768, 60);

        public Task<AccessUnit> NextAsync(CancellationToken ct) =>
            Interlocked.Increment(ref _calls) == 1
                ? Task.FromResult(new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true))
                : Task.FromException<AccessUnit>(new OperationCanceledException("o monitor virtual saiu do desktop"));

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ConfigWaitsForTheFirstFrame()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var feed = new GatedFeed(new VideoConfig(1366, 768, 60));
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        // Feed aberto mas sem imagem: nada de CONFIG (o iPhone ficaria preto esperando).
        await AnswerUntilAsync(phone, reader, () => seen.Count(t => t == LinkMessageType.Ping) >= 6, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Config, seen);

        feed.Release.SetResult();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video), seen, cts.Token);
        var config = seen.IndexOf(LinkMessageType.Config);
        Assert.True(config >= 0);
        Assert.Equal(LinkMessageType.Video, seen.Skip(config + 1).First(t => t != LinkMessageType.Ping));

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task SwitchOffSendsVideoEndAndSwitchOnOpensANewFeed()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feeds = new ConcurrentQueue<GatedFeed>();
        var statuses = new ConcurrentQueue<LinkStatus>();
        var run = LinkSession.RunAsync(pc, statuses.Enqueue, Fast, cts.Token, (_, _) =>
        {
            var feed = new GatedFeed(new VideoConfig(2556, 1179, 60), VideoKind.Extend);
            feed.Release.SetResult();
            feeds.Enqueue(feed);
            return Task.FromResult<IVideoFeed?>(feed);
        }, mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.Contains(statuses, s => s.Kind == VideoKind.Extend && s.Video == new VideoConfig(2556, 1179, 60));

        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.VideoEnd), seen, cts.Token);
        Assert.True(Assert.Single(feeds).Disposed);
        var afterEnd = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Count(t => t == LinkMessageType.Ping) >= 4, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Video, seen.Skip(afterEnd));
        Assert.False(run.IsCompleted);
        Assert.Null(statuses.Last().Video);

        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.Equal(LinkMessageType.Config, seen.Skip(afterEnd).First(t => t != LinkMessageType.Ping));
        Assert.Equal(2, feeds.Count);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.All(feeds, f => Assert.True(f.Disposed));
    }

    [Fact]
    public async Task FeedThatStopsByItselfSendsVideoEndAndTurnsTheSwitchOff()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feed = new OneFrameThenStopsFeed();
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.VideoEnd), seen, cts.Token);
        Assert.True(feed.Disposed);
        Assert.False(mode.WantsVideo);
        Assert.True(mode.IsConnected);

        var afterEnd = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.False(run.IsCompleted);
        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task SwitchOffBeforeTheFirstFrameSendsNoVideoEnd()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feed = new GatedFeed(new VideoConfig(1366, 768, 60)); // nunca libera a 1ª imagem
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Count(t => t == LinkMessageType.Ping) >= 2, seen, cts.Token);
        mode.Toggle();
        await AnswerUntilAsync(phone, reader, () => feed.Disposed && seen.Count(t => t == LinkMessageType.Ping) >= 6, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Config, seen);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task VideoThatDoesNotOpenTurnsTheSwitchOff()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(null), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, new LinkFrameReader(phone), () => !mode.WantsVideo && seen.Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.True(mode.IsConnected);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~LinkSessionTests"`
Expected: FALHA de compilação — `VideoKind` não existe e `RunAsync` não aceita `videoSwitch`.

- [ ] **Step 4: `VideoKind` no `IVideoFeed`**

`windows/src/TelaDois.Core/Video/IVideoFeed.cs` inteiro:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Duplicar um monitor existente (5a) ou Estender com o monitor virtual (5b).</summary>
public enum VideoKind
{
    Mirror,
    Extend,
}

/// <summary>
/// Fonte de vídeo de uma sessão: formato fixo e imagens em sequência. Descartar encerra a captura (pode ser chamado
/// mais de uma vez) e faz <see cref="NextAsync"/> — pendente ou futuro — terminar com <see cref="OperationCanceledException"/>.
/// <see cref="NextAsync"/> também termina assim quando a fonte para sozinha (ex.: o monitor virtual saiu do desktop).
/// </summary>
public interface IVideoFeed : IAsyncDisposable
{
    VideoConfig Config { get; }

    VideoKind Kind => VideoKind.Mirror;

    Task<AccessUnit> NextAsync(CancellationToken ct);
}
```

- [ ] **Step 5: Reescrever `LinkSession.cs`**

`windows/src/TelaDois.Core/Link/LinkSession.cs` inteiro:

```csharp
using System.Diagnostics;
using TelaDois.Core.Video;

namespace TelaDois.Core.Link;

public sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout)
{
    public static LinkSessionOptions Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
}

/// <summary>Estado da conexão: resolução do iPhone, a última latência medida e, com o vídeo ligado, formato e tipo.</summary>
public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null, VideoKind? Kind = null);

public enum LinkEndReason
{
    Bye,
    Timeout,
    Closed,
    Cancelled,
}

/// <summary>
/// Lado do PC: espera o HELLO, manda PING logo em seguida e a cada intervalo, mede o PONG e encerra no BYE,
/// quando o iPhone fecha o socket ou após o tempo-limite sem receber nada (ou com uma escrita travada).
/// O vídeo liga e desliga pelo <see cref="IVideoSwitch"/> (sem ele, fica ligado): ligar abre o feed em paralelo e
/// manda CONFIG junto com a 1ª imagem; desligar — ou o feed parar sozinho, ou não abrir — descarta o feed e, se o
/// iPhone tinha recebido CONFIG, manda VIDEO_END; a sessão continua. Ao sair, descarta o vídeo e manda BYE.
/// </summary>
public static class LinkSession
{
    public static async Task<LinkEndReason> RunAsync(
        Stream stream,
        Action<LinkStatus> onStatus,
        LinkSessionOptions options,
        CancellationToken ct,
        Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null,
        IVideoSwitch? videoSwitch = null)
    {
        var reader = new LinkFrameReader(stream);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clock = Stopwatch.StartNew();
        long lastHeard = 0;
        var statusLock = new object();
        LinkStatus? status = null;
        Task<LinkEndReason>? receiving = null;
        CancellationTokenSource? videoCts = null;
        Task<IVideoFeed?>? opening = null;
        IVideoFeed? feed = null;
        Task? sending = null;
        var configSent = 0;

        // Toda escrita, inclusive a espera pela vez, tem tempo-limite: um iPhone que parou de ler encerra a sessão.
        async Task SendAsync(LinkFrame frame, CancellationToken token)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
            limit.CancelAfter(options.Timeout);
            try
            {
                await writeLock.WaitAsync(limit.Token);
                try
                {
                    await stream.WriteAsync(frame.Encode(), limit.Token);
                }
                finally
                {
                    writeLock.Release();
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException("o iPhone parou de receber");
            }
        }

        // Serializa as atualizações para uma medição de latência nunca apagar o vídeo do status (e vice-versa).
        void Update(Func<LinkStatus, LinkStatus> change)
        {
            lock (statusLock)
            {
                status = change(status!);
                onStatus(status);
            }
        }

        void StartVideo(HelloInfo hello)
        {
            if (openVideo is null)
            {
                return;
            }
            videoCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            var token = videoCts.Token;
            // Task.Run: uma abertura que trava ou lança sem await também fica numa tarefa observada.
            opening = Task.Run(() => openVideo(hello, token), CancellationToken.None);
        }

        void StartSending(IVideoFeed activeFeed)
        {
            var token = videoCts!.Token;
            sending = Task.Run(async () =>
            {
                // CONFIG só com a 1ª imagem pronta (I1): uma captura que falha logo não deixa o iPhone preto.
                var unit = await activeFeed.NextAsync(token);
                await SendAsync(LinkMessages.Config(activeFeed.Config), cts.Token);
                Interlocked.Exchange(ref configSent, 1);
                Update(s => s with { Video = activeFeed.Config, Kind = activeFeed.Kind });
                var started = Stopwatch.StartNew();
                while (true)
                {
                    // Escritas com o token da sessão: cancelar o vídeo no meio de uma escrita cortaria a mensagem.
                    await SendAsync(LinkMessages.Video(unit.IsKeyframe, (ulong)(started.Elapsed.Ticks / 10), unit.Data), cts.Token);
                    unit = await activeFeed.NextAsync(token);
                }
            }, CancellationToken.None);
        }

        // Para a abertura, o envio e o feed, sem mandar nada. Engole qualquer falha deles.
        async Task DisposeVideoAsync()
        {
            videoCts?.Cancel();
            await ObserveAsync(sending);
            sending = null;
            if (opening is not null)
            {
                try
                {
                    feed ??= await opening;
                }
                catch (Exception)
                {
                }
                opening = null;
            }
            if (feed is not null)
            {
                try
                {
                    await feed.DisposeAsync();
                }
                catch (Exception)
                {
                }
                feed = null;
            }
            videoCts?.Dispose();
            videoCts = null;
        }

        // Desliga o vídeo com a sessão viva: o iPhone só recebe VIDEO_END se chegou a receber CONFIG.
        async Task StopVideoAsync()
        {
            await DisposeVideoAsync();
            if (Interlocked.Exchange(ref configSent, 0) == 1)
            {
                Update(s => s with { Video = null, Kind = null });
                await SendAsync(LinkMessages.VideoEnd(), cts.Token);
            }
        }

        try
        {
            var hello = await ReadHelloAsync(reader, options.Timeout, cts.Token);
            if (hello is null)
            {
                return LinkEndReason.Closed;
            }
            var helloInfo = hello.Value;
            status = new LinkStatus(helloInfo, null);
            onStatus(status);
            Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);

            receiving = Task.Run(async () =>
            {
                while (await reader.NextAsync(cts.Token) is { } frame)
                {
                    Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);
                    switch (frame.MessageType)
                    {
                        case LinkMessageType.Pong when LinkMessages.TryReadTimestamp(frame, out var sent):
                            var roundTrip = TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds - (long)sent);
                            Update(s => s with { RoundTrip = roundTrip });
                            break;
                        case LinkMessageType.Ping when LinkMessages.TryReadTimestamp(frame, out var theirs):
                            await SendAsync(LinkMessages.Pong(theirs), cts.Token);
                            break;
                        case LinkMessageType.Bye:
                            return LinkEndReason.Bye;
                    }
                }
                return LinkEndReason.Closed;
            }, CancellationToken.None);

            var wantVideo = videoSwitch?.WantsVideo ?? true;
            if (wantVideo)
            {
                StartVideo(helloInfo);
            }
            var switchChanged = videoSwitch?.WaitForChangeAsync(wantVideo, cts.Token) ?? Task.Delay(Timeout.Infinite, cts.Token);

            await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
            var tick = Task.Delay(options.PingInterval, cts.Token);
            while (true)
            {
                var pending = new List<Task>(5) { receiving, tick, switchChanged };
                if (sending is not null)
                {
                    pending.Add(sending);
                }
                if (opening is not null)
                {
                    pending.Add(opening);
                }
                var done = await Task.WhenAny(pending);
                if (done == receiving)
                {
                    return await receiving;
                }
                if (done == switchChanged)
                {
                    ct.ThrowIfCancellationRequested();
                    var wanted = videoSwitch!.WantsVideo;
                    switchChanged = videoSwitch.WaitForChangeAsync(wanted, cts.Token);
                    if (wanted != wantVideo)
                    {
                        wantVideo = wanted;
                        if (wanted)
                        {
                            StartVideo(helloInfo);
                        }
                        else
                        {
                            await StopVideoAsync();
                        }
                    }
                    continue;
                }
                if (done == sending)
                {
                    try
                    {
                        await sending; // relança a falha de escrita (IOException → Closed, TimeoutException → Timeout)
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        // O feed parou sozinho (ex.: o monitor virtual saiu do desktop): vídeo desligado, a sessão segue.
                        await StopVideoAsync();
                        wantVideo = false;
                        videoSwitch?.VideoEnded();
                        continue;
                    }
                    return LinkEndReason.Closed;
                }
                if (done == opening)
                {
                    var opened = opening;
                    opening = null;
                    try
                    {
                        feed = await opened;
                    }
                    catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
                    {
                        // Quem abre registra o motivo.
                    }
                    if (feed is not null)
                    {
                        StartSending(feed);
                    }
                    else
                    {
                        // Sem Segunda Tela agora: o túnel continua e o atalho pode tentar de novo.
                        await DisposeVideoAsync();
                        wantVideo = false;
                        videoSwitch?.VideoEnded();
                    }
                    continue;
                }
                ct.ThrowIfCancellationRequested();
                if (clock.ElapsedMilliseconds - Interlocked.Read(ref lastHeard) > options.Timeout.TotalMilliseconds)
                {
                    return LinkEndReason.Timeout;
                }
                await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
                tick = Task.Delay(options.PingInterval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return LinkEndReason.Cancelled;
        }
        catch (TimeoutException)
        {
            return LinkEndReason.Timeout;
        }
        catch (IOException)
        {
            return LinkEndReason.Closed;
        }
        finally
        {
            // A limpeza engole qualquer falha das tarefas: a exceção que sai é a original da sessão.
            cts.Cancel();
            await ObserveAsync(receiving);
            try
            {
                await DisposeVideoAsync();
            }
            finally
            {
                using var byeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                try
                {
                    await SendAsync(LinkMessages.Bye(), byeTimeout.Token);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private static async Task ObserveAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }
        try
        {
            await task;
        }
        catch (Exception)
        {
        }
    }

    private static async Task<HelloInfo?> ReadHelloAsync(LinkFrameReader reader, TimeSpan timeout, CancellationToken ct)
    {
        using var helloCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        helloCts.CancelAfter(timeout);
        LinkFrame? first;
        try
        {
            first = await reader.NextAsync(helloCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("o iPhone não mandou HELLO a tempo");
        }
        if (first is null)
        {
            return null;
        }
        return LinkMessages.TryParseHello(first, out var hello)
            ? hello
            : throw new InvalidDataException("a primeira mensagem do iPhone não é HELLO");
    }
}
```

- [ ] **Step 6: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~LinkSessionTests"`
Expected: PASS (todos os antigos + 5 novos). Rodar 5 vezes seguidas para pegar instabilidade de tempo: `for ($i=0; $i -lt 5; $i++) { dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~LinkSessionTests" --no-build | Select-Object -Last 1 }` — Expected: 5× "Aprovado".

- [ ] **Step 7: Rodar todos os testes do PC**

Run: `dotnet test windows/TelaDois.sln`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add windows/src/TelaDois.Core/Video/IVideoFeed.cs windows/src/TelaDois.Core/Link/LinkSession.cs windows/tests/TelaDois.Core.Tests/LinkSessionTests.cs
```
Mensagem: `feat(windows): sessão liga/desliga a Segunda Tela (VIDEO_END) e manda CONFIG com a 1ª imagem — I1`.

---

### Task 8: PC — `VideoFeedFactory` estende com o monitor virtual (`VirtualMonitorFeed`)

**Files:**
- Create: `windows/src/TelaDois.Core/Video/VirtualMonitorFeed.cs`
- Modify: `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs` (arquivo inteiro)
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeVirtualMonitor.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/VirtualMonitorFeedTests.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/VideoFeedFactoryTests.cs`

**Interfaces:**
- Consumes: `IVirtualMonitor`, `DisplayModeInfo`, `DisplayChangeException` (Task 2); `VideoKind` (Task 7).
- Produces:
  - `public sealed class VirtualMonitorFeed(IVideoFeed capture, IVirtualMonitor monitor, string deviceName, Action<string> log, TimeSpan? checkInterval = null) : IVideoFeed` — `Kind = Extend`; descartar desliga o monitor; `NextAsync` termina com `OperationCanceledException` se o monitor sair do desktop.
  - `VideoFeedFactory(string? ffmpegPath, IProcessRunner runner, Func<IReadOnlyList<DisplayOutput>> displays, Func<string?> preferredMonitor, Action<string> log, IVirtualMonitor? virtualMonitor = null)` com `event Action? DriverMissing` e `TimeSpan OutputWait { get; init; }` (padrão 2 s).

- [ ] **Step 1: Escrever o falso `FakeVirtualMonitor`**

`windows/tests/TelaDois.Core.Tests/Fakes/FakeVirtualMonitor.cs`:

```csharp
using System.Collections.Concurrent;
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Monitor virtual de mentira: liga/desliga um booleano e anota cada desligamento.</summary>
internal sealed class FakeVirtualMonitor : IVirtualMonitor
{
    private int _attached;

    public bool Installed { get; set; } = true;
    public string Device { get; set; } = @"\\.\DISPLAY3";
    public DisplayModeInfo Mode { get; set; } = new(2556, 1179, 60);
    public Exception? AttachError { get; set; }
    public ConcurrentQueue<string> Detached { get; } = new();

    public bool Attached
    {
        get => Volatile.Read(ref _attached) == 1;
        set => Volatile.Write(ref _attached, value ? 1 : 0);
    }

    public Task<bool> IsInstalledAsync(CancellationToken ct) => Task.FromResult(Installed);

    public (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height)
    {
        if (AttachError is not null)
        {
            throw AttachError;
        }
        Attached = true;
        return (Device, Mode);
    }

    public void Detach(string deviceName)
    {
        Attached = false;
        Detached.Enqueue(deviceName);
    }

    public bool IsAttached(string deviceName) => Attached && deviceName == Device;
}
```

- [ ] **Step 2: Escrever os testes do `VirtualMonitorFeed`**

`windows/tests/TelaDois.Core.Tests/Video/VirtualMonitorFeedTests.cs`:

```csharp
using System.Collections.Concurrent;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VirtualMonitorFeedTests
{
    /// <summary>Captura que nunca entrega imagem (como o FFmpeg reiniciando sem o monitor).</summary>
    private sealed class WaitingFeed : IVideoFeed
    {
        public bool Disposed { get; private set; }
        public VideoConfig Config => new(2556, 1179, 60);

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new OperationCanceledException(ct);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task StopsWhenTheMonitorLeavesTheDesktop()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var log = new ConcurrentQueue<string>();
        await using var feed = new VirtualMonitorFeed(new WaitingFeed(), monitor, monitor.Device, log.Enqueue, TimeSpan.FromMilliseconds(50));
        Assert.Equal(VideoKind.Extend, ((IVideoFeed)feed).Kind);
        Assert.Equal(new VideoConfig(2556, 1179, 60), feed.Config);

        var next = feed.NextAsync(CancellationToken.None);
        monitor.Attached = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains("o monitor virtual saiu do desktop: Segunda Tela desligada", log);
    }

    [Fact]
    public async Task DisposeStopsTheCaptureAndDetachesOnce()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var capture = new WaitingFeed();
        var feed = new VirtualMonitorFeed(capture, monitor, monitor.Device, _ => { });
        await feed.DisposeAsync();
        await feed.DisposeAsync();
        Assert.True(capture.Disposed);
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
    }
}
```

- [ ] **Step 3: Escrever os testes novos da fábrica**

Em `windows/tests/TelaDois.Core.Tests/Video/VideoFeedFactoryTests.cs`, acrescentar `using TelaDois.Core.Display;` no topo e, antes do fim da classe:

```csharp
    private static readonly DisplayOutput Virtual = new(2, @"\\.\DISPLAY3", 2556, 1179, false);

    /// <summary>Sondagem: só o h264_mf funciona; captura: entrega o vídeo de teste.</summary>
    private static FakeProcessRunner WorkingRunner() =>
        new(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, AccessUnitReaderTests.Fixture()));

    [Fact]
    public async Task ExtendsWithTheVirtualMonitorAndDetachesOnDispose()
    {
        var monitor = new FakeVirtualMonitor();
        var runner = WorkingRunner();
        var factory = new VideoFeedFactory("ffmpeg", runner, () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);

        var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.NotNull(feed);
        Assert.Equal(VideoKind.Extend, feed!.Kind);
        Assert.Equal(new VideoConfig(2556, 1179, 60), feed.Config);
        await feed.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        Assert.Contains("ddagrab=output_idx=2:framerate=60:draw_mouse=1", runner.Started.Last(a => !a.Contains("-frames:v")));
        await feed.DisposeAsync();
        Assert.Equal(new[] { Virtual.DeviceName }, monitor.Detached);
    }

    [Fact]
    public async Task WithoutTheDriverMirrorsAndWarns()
    {
        var monitor = new FakeVirtualMonitor { Installed = false };
        var warned = 0;
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, _ => { }, monitor);
        factory.DriverMissing += () => warned++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed.Config);
        Assert.Equal(1, warned);
        Assert.Empty(monitor.Detached);
    }

    [Fact]
    public async Task AttachFailureFallsBackToMirror()
    {
        var monitor = new FakeVirtualMonitor { AttachError = new DisplayChangeException(-2, "ligar") };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains(log, line => line.StartsWith("não deu para ligar o monitor virtual", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VirtualOutputMissingFromDxgiDetachesAndMirrors()
    {
        var monitor = new FakeVirtualMonitor();
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(200),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
        Assert.Contains(log, line => line.Contains("fora do adaptador 0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LogsWhenTheDriverHasNoExactMode()
    {
        var monitor = new FakeVirtualMonitor { Mode = new DisplayModeInfo(2622, 1206, 60) };
        var wide = Virtual with { Width = 2622, Height = 1206 };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, wide } : new[] { Main }, () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(new VideoConfig(2622, 1206, 60), feed!.Config);
        Assert.Contains("o monitor virtual não tem 2556×1179: usando 2622×1206", log);
    }

    [Fact]
    public async Task ProbeFailureDetachesTheVirtualMonitor()
    {
        var monitor = new FakeVirtualMonitor();
        var factory = new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(1)),
            () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);

        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
    }
```

- [ ] **Step 4: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~VideoFeedFactoryTests|FullyQualifiedName~VirtualMonitorFeedTests"`
Expected: FALHA de compilação — `VirtualMonitorFeed` não existe e a fábrica não aceita `IVirtualMonitor`.

- [ ] **Step 5: Escrever `VirtualMonitorFeed.cs`**

`windows/src/TelaDois.Core/Video/VirtualMonitorFeed.cs`:

```csharp
using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Captura do monitor virtual (Estender). Confere a cada <c>checkInterval</c> se o monitor continua no desktop: se o
/// usuário o desligou nas Configurações, <see cref="NextAsync"/> termina com <see cref="OperationCanceledException"/>
/// (a sessão manda VIDEO_END). Descartar encerra a captura e tira o monitor do desktop.
/// </summary>
public sealed class VirtualMonitorFeed : IVideoFeed
{
    private readonly IVideoFeed _capture;
    private readonly IVirtualMonitor _monitor;
    private readonly string _deviceName;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _gone = new();
    private readonly Task _watch;
    private int _disposed;

    public VirtualMonitorFeed(IVideoFeed capture, IVirtualMonitor monitor, string deviceName, Action<string> log, TimeSpan? checkInterval = null)
    {
        _capture = capture;
        _monitor = monitor;
        _deviceName = deviceName;
        _log = log;
        _watch = WatchAsync(checkInterval ?? TimeSpan.FromSeconds(1));
    }

    public VideoConfig Config => _capture.Config;

    public VideoKind Kind => VideoKind.Extend;

    public async Task<AccessUnit> NextAsync(CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _gone.Token);
        return await _capture.NextAsync(linked.Token);
    }

    private async Task WatchAsync(TimeSpan interval)
    {
        try
        {
            while (true)
            {
                await Task.Delay(interval, _gone.Token);
                if (!_monitor.IsAttached(_deviceName))
                {
                    _log("o monitor virtual saiu do desktop: Segunda Tela desligada");
                    _gone.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }
        _gone.Cancel();
        await _watch;
        await _capture.DisposeAsync();
        try
        {
            if (_monitor.IsAttached(_deviceName))
            {
                _monitor.Detach(_deviceName);
            }
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            _log($"não deu para desligar o monitor virtual: {e.Message}");
        }
        _gone.Dispose();
    }
}
```

- [ ] **Step 6: Reescrever `VideoFeedFactory.cs`**

`windows/src/TelaDois.Core/Video/VideoFeedFactory.cs` inteiro:

```csharp
using System.Diagnostics;
using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Ao ligar o vídeo de uma sessão: acha o FFmpeg; com o monitor virtual instalado, liga-o com a resolução do iPhone
/// e captura ele (Estender); sem driver, ou se ligar/achar o monitor falhar, captura um monitor existente (Duplicar).
/// </summary>
public sealed class VideoFeedFactory(
    string? ffmpegPath,
    IProcessRunner runner,
    Func<IReadOnlyList<DisplayOutput>> displays,
    Func<string?> preferredMonitor,
    Action<string> log,
    IVirtualMonitor? virtualMonitor = null)
{
    public const ushort Fps = 60;

    private readonly EncoderProbe? _probe = ffmpegPath is null ? null : new EncoderProbe(runner, ffmpegPath, log);

    public event Action? FfmpegMissing;

    /// <summary>O monitor virtual não respondeu: a Segunda Tela duplica um monitor existente.</summary>
    public event Action? DriverMissing;

    /// <summary>Quanto esperar o monitor recém-ligado aparecer na DXGI.</summary>
    public TimeSpan OutputWait { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// A captura, ou <c>null</c> sem FFmpeg, sem monitor ou com qualquer falha (registrada no log): o vídeo nunca
    /// derruba o túnel. Só o cancelamento de <c>ct</c> sai como exceção.
    /// </summary>
    public async Task<IVideoFeed?> OpenAsync(HelloInfo hello, CancellationToken ct)
    {
        try
        {
            return await TryOpenAsync(hello, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            log($"Segunda Tela indisponível: {e.Message}");
            return null;
        }
    }

    private async Task<IVideoFeed?> TryOpenAsync(HelloInfo hello, CancellationToken ct)
    {
        if (ffmpegPath is null || _probe is null)
        {
            log("FFmpeg não encontrado: instale com 'winget install Gyan.FFmpeg'");
            FfmpegMissing?.Invoke();
            return null;
        }
        if (virtualMonitor is not null)
        {
            if (await virtualMonitor.IsInstalledAsync(ct))
            {
                if (await TryExtendAsync(virtualMonitor, _probe, ffmpegPath, hello, ct) is { } extended)
                {
                    return extended;
                }
            }
            else
            {
                log(@"monitor virtual não instalado: duplicando um monitor (para estender, rode tools\vdd\instalar-vdd.ps1 como administrador)");
                DriverMissing?.Invoke();
            }
        }
        var output = DisplayCatalog.Choose(displays(), preferredMonitor());
        if (output is null)
        {
            log("nenhum monitor para transmitir");
            return null;
        }
        var profile = await _probe.ChooseAsync(output.OutputIndex, ct);
        log($"duplicando {output.DeviceName} ({output.Width}×{output.Height}) com {profile.Id} para o iPhone {hello.Width}×{hello.Height}");
        return new FfmpegCapture(runner, ffmpegPath, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
            new VideoConfig((uint)output.Width, (uint)output.Height, Fps), log);
    }

    /// <summary>Liga o monitor virtual e captura ele; <c>null</c> para cair em Duplicar (motivo no log).</summary>
    private async Task<IVideoFeed?> TryExtendAsync(IVirtualMonitor monitor, EncoderProbe probe, string ffmpeg, HelloInfo hello, CancellationToken ct)
    {
        string device;
        DisplayModeInfo mode;
        try
        {
            (device, mode) = monitor.Attach((int)hello.Width, (int)hello.Height);
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            log($"não deu para ligar o monitor virtual: {e.Message}; duplicando um monitor");
            return null;
        }
        try
        {
            if (mode.Width != hello.Width || mode.Height != hello.Height)
            {
                log($"o monitor virtual não tem {hello.Width}×{hello.Height}: usando {mode.Width}×{mode.Height}");
            }
            var output = await WaitForOutputAsync(device, ct);
            if (output is null)
            {
                log($"monitor virtual {device} fora do adaptador 0 da DXGI (o ddagrab não o enxerga): duplicando um monitor");
                Detach(monitor, device);
                return null;
            }
            var profile = await probe.ChooseAsync(output.OutputIndex, ct);
            log($"estendendo para o iPhone: {device} ({output.Width}×{output.Height}) com {profile.Id}");
            var capture = new FfmpegCapture(runner, ffmpeg, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
                new VideoConfig((uint)output.Width, (uint)output.Height, Fps), log);
            return new VirtualMonitorFeed(capture, monitor, device, log);
        }
        catch
        {
            Detach(monitor, device);
            throw;
        }
    }

    private async Task<DisplayOutput?> WaitForOutputAsync(string device, CancellationToken ct)
    {
        var waited = Stopwatch.StartNew();
        while (true)
        {
            if (displays().FirstOrDefault(o => o.DeviceName == device) is { } output)
            {
                return output;
            }
            if (waited.Elapsed >= OutputWait)
            {
                return null;
            }
            await Task.Delay(100, ct);
        }
    }

    private void Detach(IVirtualMonitor monitor, string device)
    {
        try
        {
            monitor.Detach(device);
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            log($"não deu para desligar o monitor virtual: {e.Message}");
        }
    }
}
```

- [ ] **Step 7: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~VideoFeedFactoryTests|FullyQualifiedName~VirtualMonitorFeedTests"`
Expected: PASS (6 antigos + 6 novos na fábrica, 2 no feed). O teste antigo `CapturesChosenMonitorWithProbedEncoder` continua passando (sem `IVirtualMonitor`, Duplicar como na 5a).

- [ ] **Step 8: Rodar todos os testes do PC**

Run: `dotnet test windows/TelaDois.sln`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add windows/src/TelaDois.Core/Video/VirtualMonitorFeed.cs windows/src/TelaDois.Core/Video/VideoFeedFactory.cs windows/tests/TelaDois.Core.Tests/Fakes/FakeVirtualMonitor.cs windows/tests/TelaDois.Core.Tests/Video/VirtualMonitorFeedTests.cs windows/tests/TelaDois.Core.Tests/Video/VideoFeedFactoryTests.cs
```
Mensagem: `feat(windows): Segunda Tela estende com o monitor virtual; sem driver, duplica como na 5a`.

---

### Task 9: PC — túnel e bandeja: `MonitorMode`, atalho Win+Shift+], "Modo monitor", status

**Files:**
- Create: `windows/src/TelaDois.Core/Display/KeyboardLayout.cs`
- Create: `windows/src/TelaDois.Tray/Hotkey.cs`
- Modify: `windows/src/TelaDois.Core/UsbTunnelService.cs`
- Modify: `windows/src/TelaDois.Core/TunnelStatusText.cs`
- Modify: `windows/src/TelaDois.Tray/TrayContext.cs` (arquivo inteiro)
- Test: `windows/tests/TelaDois.Core.Tests/UsbTunnelServiceTests.cs`
- Test: `windows/tests/TelaDois.Core.Tests/TunnelStatusTextTests.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/KeyboardLayoutLocalTests.cs`

**Interfaces:**
- Consumes: `MonitorMode`, `ToggleResult` (Task 6); `LinkStatus.Kind`, `VideoKind` (Task 7); `VirtualDisplay`, `Win32DisplayApi`, `DriverPipe` (Task 2); `VideoFeedFactory.DriverMissing` (Task 8).
- Produces:
  - `TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null, VideoKind? Kind = null)`
  - `UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null, MonitorMode? monitor = null)`
  - `KeyboardLayout.VirtualKeyFor(char key) -> int?`
  - Tray: `Hotkey(char key, Action pressed)` com `bool Registered`.

- [ ] **Step 1: Testes do túnel e do texto da bandeja**

Em `UsbTunnelServiceTests.cs`, acrescentar `using TelaDois.Core.Display;` no topo e, antes do fim da classe:

```csharp
    [Fact]
    public async Task MonitorModeFollowsTheSession()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var monitor = new MonitorMode();
        var service = new UsbTunnelService(new UsbmuxClient(usbmuxd.Endpoint), Fast, _ => { }, monitor: monitor) { Retry = TimeSpan.FromMilliseconds(100) };
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await connected.WaitAsync(cts.Token);
        Assert.True(monitor.IsConnected);
        Assert.True(monitor.WantsVideo); // liga sozinho ao conectar

        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Detached(1));
        await waiting.WaitAsync(cts.Token);
        Assert.False(monitor.IsConnected);
        Assert.False(monitor.WantsVideo);
        cts.Cancel();
        await run;
    }
```

`TunnelStatusTextTests.cs` inteiro:

```csharp
using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class TunnelStatusTextTests
{
    [Fact]
    public void ShowsMirroringWithMonitorSizeAndLatency()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4), new VideoConfig(1366, 768, 60), VideoKind.Mirror));
        Assert.Equal("TelaDois: duplicando · 1366×768 · 60 fps · 4 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ShowsExtendedWithinTheTooltipLimit()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2868, 1320), TimeSpan.FromMilliseconds(123), new VideoConfig(2868, 1320, 60), VideoKind.Extend));
        Assert.Equal("TelaDois: estendido · 2868×1320 · 60 fps · 123 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ConnectedWithoutVideoKeepsOldText() =>
        Assert.Equal("TelaDois: conectado · 2556×1179 · 4 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4))));
}
```

`windows/tests/TelaDois.Core.Tests/Display/KeyboardLayoutLocalTests.cs`:

```csharp
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

[Trait("Category", "Local")]
public class KeyboardLayoutLocalTests
{
    [Fact]
    public void ClosingBracketHasAKeyWithoutModifiersOnThisKeyboard() =>
        // ABNT2 e US têm "]" numa tecla própria (em posições diferentes); o atalho usa a tecla certa de cada um.
        Assert.NotNull(KeyboardLayout.VirtualKeyFor(']'));
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~UsbTunnelServiceTests|FullyQualifiedName~TunnelStatusTextTests|FullyQualifiedName~KeyboardLayoutLocalTests"`
Expected: FALHA de compilação — `TunnelStatus` sem `Kind`, `UsbTunnelService` sem `monitor`, `KeyboardLayout` não existe.

- [ ] **Step 3: `UsbTunnelService`**

Em `UsbTunnelService.cs`: acrescentar `using TelaDois.Core.Display;`; trocar o record por

```csharp
public sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null, VideoKind? Kind = null);
```

trocar a assinatura da classe por

```csharp
public sealed class UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null, MonitorMode? monitor = null)
```

e, em `RunDeviceAsync`, trocar o bloco de `log("túnel aberto até o app do iPhone");` até `log($"sessão encerrada: {reason}");` por:

```csharp
                log("túnel aberto até o app do iPhone");
                // Ao conectar, a Segunda Tela liga sozinha; ao sair, desliga (o atalho só vale com o iPhone conectado).
                monitor?.SessionStarted();
                try
                {
                    var reason = await LinkSession.RunAsync(
                        tunnel,
                        status => Report(new TunnelStatus(TunnelState.Connected, status.Hello, status.RoundTrip, status.Video, status.Kind)),
                        options,
                        ct,
                        openVideo,
                        monitor);
                    log($"sessão encerrada: {reason}");
                }
                finally
                {
                    monitor?.SessionEnded();
                }
```

- [ ] **Step 4: `TunnelStatusText`**

`windows/src/TelaDois.Core/TunnelStatusText.cs` inteiro:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core;

/// <summary>Texto curto da bandeja (o Windows corta o tooltip do ícone em 63 caracteres; a compressão fica no registro).</summary>
public static class TunnelStatusText
{
    public static string For(TunnelStatus status) => status.State switch
    {
        TunnelState.ServiceMissing => "TelaDois: falta o Apple Mobile Device Service",
        TunnelState.WaitingForIPhone => "TelaDois: conecte o iPhone no cabo",
        TunnelState.WaitingForApp => "TelaDois: abra o app no iPhone",
        _ when status is { Video: { } video, RoundTrip: { } rtt } =>
            $"TelaDois: {Mode(status)} · {video.Width}×{video.Height} · {video.Fps} fps · {Math.Round(rtt.TotalMilliseconds):0} ms",
        _ when status.Video is { } video => $"TelaDois: {Mode(status)} · {video.Width}×{video.Height} · {video.Fps} fps",
        _ when status is { Hello: { } hello, RoundTrip: { } rtt } =>
            $"TelaDois: conectado · {hello.Width}×{hello.Height} · {Math.Round(rtt.TotalMilliseconds):0} ms",
        _ when status.Hello is { } hello => $"TelaDois: conectado · {hello.Width}×{hello.Height}",
        _ => "TelaDois: conectado",
    };

    private static string Mode(TunnelStatus status) => status.Kind == VideoKind.Extend ? "estendido" : "duplicando";
}
```

- [ ] **Step 5: `KeyboardLayout`**

`windows/src/TelaDois.Core/Display/KeyboardLayout.cs`:

```csharp
using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

public static class KeyboardLayout
{
    /// <summary>
    /// Tecla virtual que digita <paramref name="key"/> no layout de teclado atual sem Shift/Ctrl/Alt
    /// (no ABNT2 o "]" fica numa tecla diferente da do US); <c>null</c> se não houver.
    /// </summary>
    public static int? VirtualKeyFor(char key)
    {
        var result = VkKeyScanEx(key, GetKeyboardLayout(0));
        if (result == -1)
        {
            return null;
        }
        var modifiers = (result >> 8) & 0xFF;
        return modifiers == 0 ? result & 0xFF : null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "VkKeyScanExW")]
    private static extern short VkKeyScanEx(char ch, IntPtr layout);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint threadId);
}
```

- [ ] **Step 6: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~UsbTunnelServiceTests|FullyQualifiedName~TunnelStatusTextTests|FullyQualifiedName~KeyboardLayoutLocalTests"`
Expected: PASS.

- [ ] **Step 7: `Hotkey` da bandeja**

`windows/src/TelaDois.Tray/Hotkey.cs`:

```csharp
using System.Runtime.InteropServices;
using TelaDois.Core.Display;

namespace TelaDois.Tray;

/// <summary>Atalho global Win+Shift+&lt;tecla&gt;; a tecla é achada pelo caractere no layout atual.</summary>
internal sealed class Hotkey : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModShift = 0x4;
    private const uint ModWin = 0x8;
    private const uint ModNoRepeat = 0x4000;
    private const int Id = 1;

    private readonly Action _pressed;

    public Hotkey(char key, Action pressed)
    {
        _pressed = pressed;
        CreateHandle(new CreateParams());
        Registered = KeyboardLayout.VirtualKeyFor(key) is { } vk && RegisterHotKey(Handle, Id, ModWin | ModShift | ModNoRepeat, (uint)vk);
    }

    /// <summary>false se outro programa já usa o atalho ou a tecla não existe neste teclado.</summary>
    public bool Registered { get; }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == Id)
        {
            _pressed();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Registered)
        {
            UnregisterHotKey(Handle, Id);
        }
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
```

- [ ] **Step 8: `TrayContext`**

`windows/src/TelaDois.Tray/TrayContext.cs` inteiro (em `PersistVirtualMonitor`, usar o valor que a Task 2, Step 11 registrou):

```csharp
using TelaDois.Core;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja: estado do túnel, Modo monitor (Win+Shift+]) e o monitor duplicado sem driver.</summary>
internal sealed class TrayContext : ApplicationContext
{
    /// <summary>Ligar o monitor virtual gravando no registro do Windows (resultado da Task 2 da Etapa 5b).</summary>
    private const bool PersistVirtualMonitor = false;

    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private readonly TraySettings _settings = TraySettings.Load();
    private readonly MonitorMode _monitor = new();
    private readonly VirtualDisplay _virtualDisplay = new(new Win32DisplayApi(PersistVirtualMonitor), new DriverPipe());
    private readonly ToolStripMenuItem _monitorItem;
    private readonly Hotkey _hotkey;
    private readonly Task _running;
    private TunnelState? _lastState;
    private int _ffmpegWarned;
    private int _driverWarned;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        _monitorItem = new ToolStripMenuItem("Modo monitor (Win+Shift+])") { Enabled = false };
        _monitorItem.Click += (_, _) => ToggleMonitor();
        menu.Items.Add(_monitorItem);
        var monitors = new ToolStripMenuItem("Monitor transmitido");
        monitors.DropDownItems.Add("(carregando)");
        monitors.DropDownOpening += (_, _) => FillMonitors(monitors);
        menu.Items.Add(monitors);
        menu.Items.Add("Abrir registro", null, (_, _) => _log.Open());
        menu.Items.Add("Sair", null, (_, _) => ExitThread());
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "TelaDois: iniciando",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _log.Write("TelaDois iniciado");
        DetachLeftover();

        var ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        var video = new VideoFeedFactory(FfmpegLocator.Find(), new SystemProcessRunner(), DisplayCatalog.Enumerate, () => _settings.Monitor, _log.Write, _virtualDisplay);
        video.FfmpegMissing += () =>
        {
            // Uma vez por execução: a fábrica avisa a cada conexão, o balão não precisa repetir.
            if (Interlocked.Exchange(ref _ffmpegWarned, 1) == 0)
            {
                ui.Post(_ => _icon.ShowBalloonTip(10000, "TelaDois",
                    "Para a Segunda Tela, instale o FFmpeg (winget install Gyan.FFmpeg) e reinicie o TelaDois", ToolTipIcon.Warning), null);
            }
        };
        video.DriverMissing += () =>
        {
            if (Interlocked.Exchange(ref _driverWarned, 1) == 0)
            {
                ui.Post(_ => _icon.ShowBalloonTip(10000, "TelaDois",
                    @"Monitor virtual não instalado: duplicando um monitor. Para estender, rode tools\vdd\instalar-vdd.ps1 como administrador.", ToolTipIcon.Info), null);
            }
        };
        _monitor.StateChanged += (connected, on) => ui.Post(_ =>
        {
            _monitorItem.Enabled = connected;
            _monitorItem.Checked = on;
        }, null);

        _hotkey = new Hotkey(']', ToggleMonitor);
        if (!_hotkey.Registered)
        {
            _log.Write("atalho Win+Shift+] indisponível (outro programa já usa ou a tecla não existe neste teclado)");
            _icon.ShowBalloonTip(10000, "TelaDois", "O atalho Win+Shift+] está em uso por outro programa. Use o item \"Modo monitor\" da bandeja.", ToolTipIcon.Warning);
        }

        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write, video.OpenAsync, _monitor);
        service.StatusChanged += status => ui.Post(_ => Show(status), null);
        _running = Task.Run(() => service.RunAsync(_cts.Token));
    }

    private void ToggleMonitor()
    {
        var result = _monitor.Toggle();
        _log.Write(result switch
        {
            ToggleResult.TurnedOn => "Modo monitor: ligado",
            ToggleResult.TurnedOff => "Modo monitor: desligado",
            _ => "Modo monitor: nenhum iPhone conectado",
        });
        if (result == ToggleResult.NoPhone)
        {
            _icon.ShowBalloonTip(3000, "TelaDois", "Conecte o iPhone pelo cabo com o TelaDois aberto", ToolTipIcon.Info);
        }
    }

    /// <summary>Tira do desktop o monitor virtual que ficou ligado (queda anterior ou recém-instalado).</summary>
    private void DetachLeftover()
    {
        try
        {
            if (_virtualDisplay.DetachLeftover() is { } leftover)
            {
                _log.Write($"monitor virtual que sobrou ligado foi desligado ({leftover})");
            }
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            _log.Write($"não deu para desligar o monitor virtual que sobrou: {e.Message}");
        }
    }

    private void Show(TunnelStatus status)
    {
        var text = TunnelStatusText.For(status);
        _icon.Text = text;
        if (status.State == _lastState)
        {
            return;
        }
        _lastState = status.State;
        _log.Write(text);
        if (status.State == TunnelState.Connected)
        {
            _icon.ShowBalloonTip(3000, "TelaDois", "iPhone conectado pelo cabo", ToolTipIcon.Info);
        }
        else if (status.State == TunnelState.ServiceMissing)
        {
            _icon.ShowBalloonTip(10000, "TelaDois",
                "Instale o iTunes baixado do site da Apple (não o da Microsoft Store) para o PC enxergar o iPhone.",
                ToolTipIcon.Warning);
        }
    }

    private void FillMonitors(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        var outputs = DisplayCatalog.Enumerate();
        var current = DisplayCatalog.Choose(outputs, _settings.Monitor);
        foreach (var output in outputs)
        {
            var label = $"{output.DeviceName.Replace(@"\\.\", "")} · {output.Width}×{output.Height}{(output.IsPrimary ? " (principal)" : "")}";
            var item = new ToolStripMenuItem(label) { Checked = output == current };
            item.Click += (_, _) =>
            {
                _settings.Monitor = output.DeviceName;
                try
                {
                    _settings.Save();
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    _log.Write($"não deu para salvar a escolha do monitor: {e.Message}");
                }
                _log.Write($"monitor transmitido: {output.DeviceName} (vale na próxima conexão)");
            };
            menu.DropDownItems.Add(item);
        }
        if (outputs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("nenhum monitor encontrado") { Enabled = false });
        }
    }

    protected override void ExitThreadCore()
    {
        // Ao sair, a sessão descarta o vídeo e o monitor virtual sai do desktop: espera um pouco por isso.
        _cts.Cancel();
        try
        {
            _running.Wait(TimeSpan.FromSeconds(3));
        }
        catch (AggregateException)
        {
        }
        DetachLeftover();
        _hotkey.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
        _log.Write("TelaDois encerrado");
        base.ExitThreadCore();
    }
}
```

- [ ] **Step 9: Compilar e rodar todos os testes**

Run: `dotnet build windows/TelaDois.sln -c Release` e `dotnet test windows/TelaDois.sln`
Expected: build sem avisos; todos os testes PASS.

- [ ] **Step 10: Conferir o atalho à mão (sem iPhone)**

```powershell
dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows-5b
Start-Process builds/windows-5b/TelaDois.exe
```

Apertar Win+Shift+]. Expected: balão "Conecte o iPhone pelo cabo com o TelaDois aberto"; no registro (`%LOCALAPPDATA%\TelaDois\tela-dois.log`) "Modo monitor: nenhum iPhone conectado". Se o monitor virtual estava ligado antes de abrir, o registro mostra "monitor virtual que sobrou ligado foi desligado". Fechar pelo menu "Sair".

- [ ] **Step 11: Commit**

```bash
git add windows/src/TelaDois.Core/Display/KeyboardLayout.cs windows/src/TelaDois.Tray/Hotkey.cs windows/src/TelaDois.Core/UsbTunnelService.cs windows/src/TelaDois.Core/TunnelStatusText.cs windows/src/TelaDois.Tray/TrayContext.cs windows/tests/TelaDois.Core.Tests/UsbTunnelServiceTests.cs windows/tests/TelaDois.Core.Tests/TunnelStatusTextTests.cs windows/tests/TelaDois.Core.Tests/Display/KeyboardLayoutLocalTests.cs
```
Mensagem: `feat(windows): Modo monitor na bandeja e atalho Win+Shift+]; liga ao conectar, desliga ao sair`. `git push` e conferir `windows-test` verde.

---

### Task 10: Teste no iPhone (PC AMD) e documentação

**Files:**
- Modify: `docs/INSTALACAO.md`
- Modify: `CLAUDE.md`
- Modify: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8)

**Interfaces:**
- Consumes: tudo acima; `.ipa` de um SHA com `ios-test` e `ios-build` verdes; `TelaDois.exe` publicado.

- [ ] **Step 1: `.ipa` do SHA final**

Conferir os 3 workflows verdes no último SHA (`gh run list --branch etapa-5b-monitor-virtual --limit 6`). Baixar o artefato `TelaDois-ipa` do `ios-build` desse SHA numa pasta nova `builds/5b-<sha7>`, e inspecionar com `py -3 -I <scratchpad>\inspect_ipa.py <ipa> <pasta nova do scratchpad>`. Expected: `CFBundleShortVersionString: 0.4.0`, arm64, `LC_CODE_SIGNATURE presente: False`.

- [ ] **Step 2: Instalar pelo xtool**

Com o iPhone no cabo e desbloqueado:

```powershell
wsl -d Ubuntu -- bash -lc 'xtool install --usb /mnt/d/Downloads/Teladois/builds/5b-<sha7>/TelaDois.ipa'
```

O xtool vai pedir para revogar o certificado criado no PC do trabalho: o usuário responde `yes` numa aba do Terminal. Expected: instalação concluída; abrir o app no iPhone mostra o relógio.

- [ ] **Step 3: Publicar o `TelaDois.exe` e testar Duplicar (sem driver)**

```powershell
dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows-5b
```

O usuário desativa o monitor virtual (PowerShell administrador: `pnputil /disable-device "<InstanceId da Task 1, Step 8>"`). Abrir `builds/windows-5b/TelaDois.exe`, app aberto no iPhone, cabo plugado. Expected: balão "Monitor virtual não instalado…"; o iPhone mostra o monitor existente; o registro mostra `compressão escolhida: amf-download (h264_amf)` (ou `amf-d3d11`) — **este é o teste da 5a no PC AMD**. Win+Shift+] → o iPhone volta ao relógio com "PC conectado"; de novo → volta a imagem. Fechar o TelaDois. O usuário reativa: `pnputil /enable-device "<InstanceId>"`.

- [ ] **Step 4: Testar Estender (com driver)**

Abrir o TelaDois de novo, app aberto no iPhone, cabo plugado. Conferir, anotando no registro/fotos:
1. Em ≤ 3 s aparece um monitor extra em Configurações → Tela, à direita, 2556×1179; o iPhone mostra a área de trabalho vazia desse monitor. Tooltip: `TelaDois: estendido · 2556×1179 · 60 fps · N ms`.
2. Arrastar uma janela para a direita até ela aparecer no iPhone; o cursor do mouse aparece no iPhone.
3. Win+Shift+] → a janela volta para os monitores do PC; o iPhone mostra o relógio com "PC conectado". De novo → o monitor e a imagem voltam (sem aparecer a imagem antiga por um instante — I2).
4. Tirar o cabo → o monitor some de Configurações → Tela; nenhum `ffmpeg.exe` no Gerenciador de Tarefas (`Get-Process ffmpeg -ErrorAction SilentlyContinue` vazio). Pôr o cabo → liga de novo.
5. Fechar o app no iPhone (deslizar para cima) → o monitor some. Sair do TelaDois pelo menu com o monitor ligado → o monitor some.
6. Latência: abrir um cronômetro com milissegundos (ex.: https://www.online-stopwatch.com/ ou `Get-Date -Format HH:mm:ss.fff` num loop no terminal) na tela estendida, fotografar o PC e o iPhone juntos; diferença < 100 ms.

Se algo falhar: `superpowers:systematic-debugging` com o registro `%LOCALAPPDATA%\TelaDois\tela-dois.log` antes de mexer no código.

- [ ] **Step 5: Documentação**

- `docs/INSTALACAO.md`: seção nova "Monitor virtual (Segunda Tela estendida)" — o que é, `powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1` como administrador, uma vez por PC; o que esperar (monitor some até o iPhone conectar); Win+Shift+] e o item "Modo monitor"; sem driver o TelaDois duplica um monitor; como desinstalar (Gerenciador de Dispositivos → Adaptadores de vídeo → Virtual Display Driver → Desinstalar dispositivo, marcando "remover o driver").
- `CLAUDE.md`: estrutura (`tools/vdd/`, `Display/`), decisões novas (monitor sempre instalado + `ChangeDisplaySettingsEx`, por que não `SETDISPLAYCOUNT 0`; `persist` decidido na Task 2; `VIDEO_END`; `CONFIG` com a 1ª imagem; SPS/PPS reinseridos), bytes de referência com `VIDEO_END`, "Estado atual" (5a + 5b testadas no AMD, resultados das Tasks 2 e 10: nome do adaptador, índice DXGI, latência), tabela de cada PC (Virtual Display Driver ✅ neste PC).
- Spec geral §8: linha da Etapa 5 → "concluída (5a duplicar + 5b estender), testada no iPhone em <data>".

- [ ] **Step 6: Commit e CI**

```bash
git add docs/INSTALACAO.md CLAUDE.md docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md
```
Mensagem: `docs: Etapa 5b testada no iPhone (monitor virtual estendido + atalho)`. `git push`.

- [ ] **Step 7: Juntar ao `main` — decisão do usuário**

Com `ios-test`, `ios-build` e `windows-test` verdes no SHA final, perguntar ao usuário se pode juntar `etapa-5b-monitor-virtual` (que contém a 5a) ao `main` por fast-forward (`git switch main`, `git merge --ff-only etapa-5b-monitor-virtual`, `git push`). Não juntar sem o "sim".
