# Etapa 6a — Instalador do Windows — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Um único `TelaDois-Setup-<versão>.exe` (Inno Setup) que instala o app do PC, o FFmpeg, o monitor virtual e confere o driver da Apple, publicado pelo GitHub Actions numa Release.

**Architecture:** `installer/montar.ps1` publica o `TelaDois.exe` (arquivo único, self-contained), baixa driver e FFmpeg com SHA-256 fixo e compila `installer/TelaDois.iss`. O instalador copia tudo para `C:\Program Files\TelaDois` e, no fim, roda `instalar-vdd.ps1 -DriverZip` (sem internet) e `verificar-apple.ps1`. O workflow `windows-installer.yml` faz o mesmo no CI e cria a Release numa tag `v*`.

**Tech Stack:** C# / .NET 10 (WinForms), xUnit, Windows PowerShell 5.1, Inno Setup 6.7.3, Python 3 + Pillow (só para gerar imagens), GitHub Actions `windows-2025`.

**Spec:** `docs/superpowers/specs/2026-10-30-etapa-6a-instalador-windows-design.md`

## Global Constraints

- Branch: `etapa-6a-instalador` (a partir de `etapa-5b-monitor-virtual`). Nada vai para o `main` sem `ios-test`, `ios-build`, `windows-test` verdes **e** aprovação do usuário.
- Antes de ler código, `uv tool run --from graphifyy python -m graphify query "<pergunta>"`; depois de mudar código, `uv tool run --from graphifyy python -m graphify update .`.
- Textos para o usuário em português; identificadores de código em inglês; comentários em português (como o resto do projeto).
- `TreatWarningsAsErrors=true`: zero avisos.
- **Scripts `.ps1` com acento ficam em UTF-8 com BOM** (o Windows PowerShell 5.1 lê sem BOM como ANSI). Nunca edite arquivos com `Get-Content`/`Set-Content`: use as ferramentas de edição; para criar um `.ps1` novo, grave e depois rode `tools/instalador/testar-scripts.ps1`, que reclama de BOM faltando. Para pôr o BOM: `py -3 -c "import sys,pathlib;p=pathlib.Path(sys.argv[1]);b=p.read_bytes();p.write_bytes(b if b.startswith(b'\xef\xbb\xbf') else b'\xef\xbb\xbf'+b)" <arquivo>`.
- Versões e SHA-256 fixos:
  - Virtual Display Driver `25.7.23`, `VirtualDisplayDriver-x86.Driver.Only.zip`, `E24210692B442B39AF763536330CE78B423F19342B7A7792C26DE3944E418B3A`.
  - FFmpeg Gyan `9.0.1` essentials, `https://github.com/GyanD/codexffmpeg/releases/download/9.0.1/ffmpeg-9.0.1-essentials_build.zip`, `FEC81AE03971D9DD4BE3EBE02E263BD2EC1D789483F931BDBA5F5715E65DA2E9` (tem `h264_amf`, `h264_qsv`, `h264_nvenc`, `h264_mf`, `libx264`, `ddagrab`; conferido em 2026-10-30).
  - Inno Setup `6.7.3`, `https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe`, `9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732`.
  - iTunes pelo winget: `--id Apple.iTunes -e --source winget` (instalador `iTunes64Setup.exe` do site da Apple; a página reserva `https://www.apple.com/itunes/download/win64` redireciona para o mesmo arquivo).
- Versão base do app: `0.6.0`.
- Neste PC: `dotnet` em `C:\Program Files\dotnet` (SDK 10.0.403), ISCC em `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`, `gh` em `C:\Program Files\GitHub CLI`. Abra um terminal novo se algum não estiver no PATH.
- Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Estrutura de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `windows/src/TelaDois.Core/Video/FfmpegLocator.cs` (mudar) | acha o FFmpeg: pasta do app primeiro, depois PATH |
| `windows/tests/TelaDois.Core.Tests/Video/FfmpegArgumentsTests.cs` (mudar) | testes do `FfmpegLocator` |
| `installer/gerar-imagens.py` (novo) | gera `.ico` e imagens do assistente a partir de `docs/design/icone-1024.png` |
| `windows/src/TelaDois.Tray/TelaDois.ico` (gerado) | ícone do `.exe`, da bandeja, do atalho, do instalador |
| `installer/imagens/*.png` (gerados) | imagens do assistente |
| `windows/src/TelaDois.Tray/AppIcon.cs` (novo) | carrega o ícone embutido no tamanho da bandeja |
| `windows/src/TelaDois.Tray/TelaDois.Tray.csproj`, `TrayContext.cs`, `windows/Directory.Build.props` (mudar) | ícone, versão, linha de versão no registro |
| `tools/vdd/instalar-vdd.ps1` (mudar) | `-DriverZip`, `-LogPath`, sai 3010 se pedir reinício |
| `tools/vdd/desinstalar-vdd.ps1` (novo) | desfaz o `instalar-vdd.ps1` |
| `tools/instalador/verificar-apple.ps1` (novo) | serviço da Apple; instala o iTunes pelo winget |
| `tools/instalador/testar-scripts.ps1` (novo) | sintaxe + BOM de todos os `.ps1` do instalador |
| `installer/TelaDois.iss` (novo) | o assistente (Inno Setup) |
| `installer/montar.ps1` (novo) | publica, baixa, confere, compila |
| `installer/NOTAS-DA-VERSAO.md` (novo) | texto da Release |
| `.github/workflows/windows-installer.yml` (novo) | CI do instalador e Release |
| `.gitignore`, `README.md`, `docs/INSTALACAO.md`, `CLAUDE.md` (mudar) | ignorar saídas, guias, estado |

---

### Task 1: FFmpeg ao lado do app

**Files:**
- Modify: `windows/src/TelaDois.Core/Video/FfmpegLocator.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/FfmpegArgumentsTests.cs` (fim do arquivo, junto do `FindsFfmpegInPath`)

**Interfaces:**
- Produces: `public static string? FfmpegLocator.Find(string? path = null, string? appDirectory = null)` — `appDirectory` padrão `AppContext.BaseDirectory`; procura `<appDirectory>\ffmpeg\ffmpeg.exe` antes do PATH. Chamadas existentes (`Find()`, `Find(path)`) continuam compilando.

- [ ] **Step 1: Escrever os testes que falham** — depois do `FindsFfmpegInPath`:

```csharp
    [Fact]
    public void PrefersFfmpegNextToTheApp()
    {
        var app = Directory.CreateTempSubdirectory();
        var bundled = Directory.CreateDirectory(Path.Combine(app.FullName, "ffmpeg"));
        File.WriteAllText(Path.Combine(bundled.FullName, "ffmpeg.exe"), "");
        var inPath = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(inPath.FullName, "ffmpeg.exe"), "");
        Assert.Equal(Path.Combine(bundled.FullName, "ffmpeg.exe"), FfmpegLocator.Find(inPath.FullName, app.FullName));
    }

    [Fact]
    public void FallsBackToPathWithoutBundledFfmpeg()
    {
        var app = Directory.CreateTempSubdirectory();
        var inPath = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(inPath.FullName, "ffmpeg.exe"), "");
        Assert.Equal(Path.Combine(inPath.FullName, "ffmpeg.exe"), FfmpegLocator.Find(inPath.FullName, app.FullName));
        Assert.Null(FfmpegLocator.Find(@"C:\nao-existe", app.FullName));
    }
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test windows/TelaDois.sln -c Release --filter "FullyQualifiedName~FfmpegArgumentsTests"`
Expected: erro de compilação `CS1501` (nenhuma sobrecarga de `Find` aceita 2 argumentos).

- [ ] **Step 3: Implementar** — `FfmpegLocator.cs` inteiro:

```csharp
namespace TelaDois.Core.Video;

public static class FfmpegLocator
{
    /// <summary>
    /// Procura <c>ffmpeg.exe</c> primeiro em <c>ffmpeg\</c> ao lado do TelaDois.exe (o instalador põe ali) e depois nas
    /// pastas do PATH (o winget põe um atalho em WinGet\Links).
    /// </summary>
    public static string? Find(string? path = null, string? appDirectory = null)
    {
        var bundled = Path.Combine(appDirectory ?? AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(bundled))
        {
            return bundled;
        }
        path ??= Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test windows/TelaDois.sln -c Release`
Expected: `Com falha: 0` nos dois projetos de teste (386 + 142).

- [ ] **Step 5: Commit**

```bash
git add windows/src/TelaDois.Core/Video/FfmpegLocator.cs windows/tests/TelaDois.Core.Tests/Video/FfmpegArgumentsTests.cs
git commit -m "feat(windows): FFmpeg ao lado do TelaDois.exe tem preferência sobre o PATH" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Ícone de verdade e versão

**Files:**
- Create: `installer/gerar-imagens.py`, `windows/src/TelaDois.Tray/AppIcon.cs`
- Create (gerados): `windows/src/TelaDois.Tray/TelaDois.ico`, `installer/imagens/assistente-100.png`, `assistente-150.png`, `assistente-200.png`, `icone-100.png`, `icone-150.png`, `icone-200.png`
- Modify: `windows/src/TelaDois.Tray/TelaDois.Tray.csproj`, `windows/src/TelaDois.Tray/TrayContext.cs:55-62`, `windows/Directory.Build.props`

**Interfaces:**
- Produces: `TelaDois.ico` (usado pelo `.iss` como `SetupIconFile`), imagens `installer/imagens/assistente-{100,150,200}.png` (164×314, 246×471, 328×628) e `icone-{100,150,200}.png` (55, 83, 110 px); `<VersionPrefix>0.6.0</VersionPrefix>`; linha de registro `TelaDois <versão> iniciado`.

- [ ] **Step 1: Gerador de imagens** — `installer/gerar-imagens.py`:

```python
"""Gera o ícone do Windows e as imagens do instalador a partir de docs/design/icone-1024.png.

Uso (na raiz do repositório): py -3 installer/gerar-imagens.py
Os arquivos gerados são versionados; rode de novo só se o ícone mudar. Precisa do Pillow (py -3 -m pip install pillow).
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs" / "design" / "icone-1024.png"
ICO = ROOT / "windows" / "src" / "TelaDois.Tray" / "TelaDois.ico"
IMAGES = ROOT / "installer" / "imagens"
BLACK = (0, 0, 0, 255)
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def artwork() -> Image.Image:
    """O desenho (monitor + iPhone) recortado do fundo preto, com 4% de folga."""
    img = Image.open(SOURCE).convert("RGBA")
    box = img.convert("L").point(lambda v: 255 if v > 40 else 0).getbbox()
    pad = round(img.width * 0.04)
    return img.crop((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad))


def fit(art: Image.Image, width: int, height: int) -> Image.Image:
    ratio = min(width / art.width, height / art.height)
    return art.resize((max(1, round(art.width * ratio)), max(1, round(art.height * ratio))), Image.LANCZOS)


def tile(art: Image.Image, size: int) -> Image.Image:
    """Desenho centralizado num quadrado preto de cantos arredondados, fora dele transparente."""
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=round(size * 0.2), fill=255)
    canvas.paste(Image.new("RGBA", (size, size), BLACK), (0, 0), mask)
    # Nos tamanhos da bandeja (até 32 px) o desenho ocupa quase tudo para os traços não sumirem.
    inner = size - 2 * round(size * (0.04 if size <= 32 else 0.1))
    scaled = fit(art, inner, inner)
    canvas.alpha_composite(scaled, ((size - scaled.width) // 2, (size - scaled.height) // 2))
    return canvas


def wizard(art: Image.Image, width: int, height: int) -> Image.Image:
    """Faixa da esquerda do assistente: fundo preto e o desenho um pouco acima do meio."""
    canvas = Image.new("RGBA", (width, height), BLACK)
    scaled = fit(art, round(width * 0.76), height)
    canvas.alpha_composite(scaled, ((width - scaled.width) // 2, round(height * 0.42) - scaled.height // 2))
    return canvas


def main() -> None:
    art = artwork()
    tiles = [tile(art, size) for size in ICO_SIZES]
    tiles[-1].save(ICO, format="ICO", sizes=[(s, s) for s in ICO_SIZES], append_images=tiles[:-1])
    IMAGES.mkdir(parents=True, exist_ok=True)
    for scale in (100, 150, 200):
        factor = scale / 100
        wizard(art, round(164 * factor), round(314 * factor)).save(IMAGES / f"assistente-{scale}.png")
        tile(art, round(55 * factor)).save(IMAGES / f"icone-{scale}.png")
    print(f"Gerados: {ICO.relative_to(ROOT)} e {len(list(IMAGES.glob('*.png')))} imagens em {IMAGES.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Gerar e olhar**

Run: `py -3 installer/gerar-imagens.py`
Expected: `Gerados: windows\src\TelaDois.Tray\TelaDois.ico e 6 imagens em installer\imagens`. Abrir `installer/imagens/assistente-100.png` e `icone-100.png` (ferramenta Read) e conferir: desenho azul inteiro, centralizado, sem corte; cantos do ícone transparentes.

- [ ] **Step 3: Ícone e versão no projeto** — em `windows/src/TelaDois.Tray/TelaDois.Tray.csproj`, dentro do `<PropertyGroup>` existente, acrescentar:

```xml
    <ApplicationIcon>TelaDois.ico</ApplicationIcon>
    <Product>TelaDois</Product>
    <Description>TelaDois: o iPhone vira uma tela a mais do PC pelo cabo</Description>
```

e um `ItemGroup` novo:

```xml
  <ItemGroup>
    <EmbeddedResource Include="TelaDois.ico" LogicalName="TelaDois.ico" />
  </ItemGroup>
```

Em `windows/Directory.Build.props`, dentro do `<PropertyGroup>`:

```xml
    <!-- Versão do app do PC; o CI troca pelo número da tag (installer/montar.ps1 -Version). -->
    <VersionPrefix>0.6.0</VersionPrefix>
```

- [ ] **Step 4: `AppIcon.cs`**

```csharp
namespace TelaDois.Tray;

/// <summary>Ícone do TelaDois (o mesmo do .exe), no tamanho que o Windows usa na bandeja.</summary>
internal static class AppIcon
{
    public static Icon ForTray()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("TelaDois.ico");
        return stream is null ? (Icon)SystemIcons.Application.Clone() : new Icon(stream, SystemInformation.SmallIconSize);
    }
}
```

- [ ] **Step 5: Bandeja usa o ícone e registra a versão** — em `TrayContext.cs`, trocar `Icon = SystemIcons.Application,` por `Icon = AppIcon.ForTray(),` e `_log.Write("TelaDois iniciado");` por:

```csharp
        _log.Write($"TelaDois {Application.ProductVersion} iniciado");
```

- [ ] **Step 6: Compilar, testar e publicar para conferir**

Run: `dotnet test windows/TelaDois.sln -c Release` → `Com falha: 0`.
Run: `dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o installer/out/teste-publish`
Expected: a pasta tem só `TelaDois.exe`; `(Get-Item installer/out/teste-publish/TelaDois.exe).VersionInfo.ProductVersion` começa com `0.6.0`. Apagar `installer/out/teste-publish` depois.

- [ ] **Step 7: Ignorar saídas do instalador** — no `.gitignore`, depois do bloco `# .NET`:

```
# Instalador (installer/montar.ps1): saída e downloads conferidos
/installer/out/
/installer/cache/
```

- [ ] **Step 8: Commit**

```bash
git add installer/gerar-imagens.py installer/imagens windows/src/TelaDois.Tray .gitignore windows/Directory.Build.props
git commit -m "feat(windows): ícone do TelaDois no .exe e na bandeja, versão 0.6.0 no registro" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Driver sem internet, com registro e desinstalação

**Files:**
- Modify: `tools/vdd/instalar-vdd.ps1`
- Create: `tools/vdd/desinstalar-vdd.ps1`, `tools/instalador/testar-scripts.ps1`

**Interfaces:**
- Produces: `instalar-vdd.ps1 [-DriverZip <zip>] [-LogPath <arquivo>]` → código 0 (pronto), 3010 (pronto, pede reinício), 1 (falhou). `desinstalar-vdd.ps1 [-LogPath <arquivo>]` → 0 (nada sobrou) ou 1. `testar-scripts.ps1` → 0 ou 1.

- [ ] **Step 1: Teste dos scripts (falha primeiro)** — `tools/instalador/testar-scripts.ps1` (UTF-8 com BOM):

```powershell
<#
.SYNOPSIS
  Confere os scripts do instalador: sintaxe do Windows PowerShell 5.1 e UTF-8 com BOM quando há acentos.
.DESCRIPTION
  Sem BOM o PowerShell 5.1 lê o arquivo como ANSI e os acentos das mensagens saem errados.
  Uso: powershell -ExecutionPolicy Bypass -File tools\instalador\testar-scripts.ps1   (o CI roda o mesmo)
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$folders = 'tools\vdd', 'tools\instalador', 'installer' | ForEach-Object { Join-Path $root $_ } | Where-Object { Test-Path $_ }
$expected = 'instalar-vdd.ps1', 'desinstalar-vdd.ps1', 'verificar-apple.ps1', 'testar-scripts.ps1', 'montar.ps1'
$scripts = @(Get-ChildItem $folders -Filter *.ps1)
$failed = 0
foreach ($name in $expected) {
    if (-not ($scripts | Where-Object Name -eq $name)) { Write-Host "${name}: não encontrado"; $failed++ }
}
foreach ($script in $scripts) {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors) | Out-Null
    foreach ($e in $errors) { Write-Host "$($script.Name):$($e.Extent.StartLineNumber): $($e.Message)"; $failed++ }
    $bytes = [IO.File]::ReadAllBytes($script.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if (-not $hasBom -and ($bytes | Where-Object { $_ -gt 127 } | Select-Object -First 1)) {
        Write-Host "$($script.Name): tem acentos e não está em UTF-8 com BOM"
        $failed++
    }
}
if ($failed) { Write-Host "$failed problema(s)."; exit 1 }
Write-Host "$($scripts.Count) scripts ok."
```

Run: `powershell -ExecutionPolicy Bypass -File tools\instalador\testar-scripts.ps1`
Expected: FAIL — `desinstalar-vdd.ps1: não encontrado`, `verificar-apple.ps1: não encontrado`, `montar.ps1: não encontrado`, exit 1. (Os dois últimos só somem nas Tasks 4 e 5.)

- [ ] **Step 2: `instalar-vdd.ps1` com `-DriverZip` e `-LogPath`** — usar a ferramenta Edit (o arquivo tem BOM; mantenha). Mudanças:

1. Na ajuda (`.DESCRIPTION`), depois da linha do `powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1`, acrescentar:
```
  O instalador do TelaDois roda com -DriverZip (o pacote que vem dentro dele, sem baixar nada; SHA-256 e assinatura
  conferidos do mesmo jeito) e -LogPath (registro em arquivo). Sai com 0 (pronto), 3010 (pronto, falta reiniciar o
  Windows) ou 1 (falhou; o motivo fica na tela e no registro).
```
2. Logo depois de `#Requires -RunAsAdministrator`:
```powershell
param(
    # Zip do driver já baixado (o instalador do TelaDois traz um): instala sem internet.
    [string]$DriverZip,
    # Arquivo onde guardar tudo o que este script mostrar.
    [string]$LogPath
)
```
3. Depois de `$ProgressPreference = 'SilentlyContinue'`:
```powershell
if ($LogPath) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $LogPath) | Out-Null
    Start-Transcript -Path $LogPath -Force | Out-Null
}
$rebootNeeded = $false
```
4. Trocar as duas linhas do download:
```powershell
    Write-Host "Baixando $Asset ($Version)..."
    Invoke-WebRequest -Uri $Url -OutFile $zip -UseBasicParsing
```
por:
```powershell
    if ($DriverZip) {
        Write-Host "Usando o pacote do driver que veio com o instalador ($Version)."
        Copy-Item -LiteralPath $DriverZip -Destination $zip
    } else {
        Write-Host "Baixando $Asset ($Version)..."
        Invoke-WebRequest -Uri $Url -OutFile $zip -UseBasicParsing
    }
```
5. No ramo do `pnputil /add-driver`, depois do `if ($LASTEXITCODE -ne 0 -and ...) { throw ... }`: `if ($LASTEXITCODE -eq 3010) { $rebootNeeded = $true }`.
6. Trocar `if ([TelaDoisVdd]::Install($HardwareId, $inf.FullName)) { Write-Host 'O Windows pediu reinício para terminar a instalação.' }` por:
```powershell
        if ([TelaDoisVdd]::Install($HardwareId, $inf.FullName)) {
            Write-Host 'O Windows pediu reinício para terminar a instalação.'
            $rebootNeeded = $true
        }
```
7. No `if ($LASTEXITCODE -eq 3010) {` do `pnputil /disable-device`, acrescentar `$rebootNeeded = $true` como primeira linha do bloco.
8. Trocar o `finally { Remove-Item -Recurse -Force $work }` final por:
```powershell
}
catch {
    Write-Host "ERRO: $($_.Exception.Message)"
    $failed = $true
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
    if ($LogPath) { Stop-Transcript | Out-Null }
}
if ($failed) { exit 1 }
if ($rebootNeeded) { exit 3010 }
```
(o `try {` existente continua abrindo o bloco; o `catch` entra entre o fim do `try` e o `finally`). Declarar `$failed = $false` junto do `$rebootNeeded = $false`.

- [ ] **Step 3: `tools/vdd/desinstalar-vdd.ps1`** (UTF-8 com BOM):

```powershell
<#
.SYNOPSIS
  Remove o monitor virtual do TelaDois (tudo o que o instalar-vdd.ps1 pôs no PC).
.DESCRIPTION
  Rode como administrador:  powershell -ExecutionPolicy Bypass -File tools\vdd\desinstalar-vdd.ps1
  Apaga as tarefas \TelaDois\ do Agendador, o dispositivo Root\MttVDD, o pacote do driver (MttVDD.inf) e a pasta
  C:\VirtualDisplayDriver. Cada passo ignora o que já não existe: rodar de novo não faz mal.
  Sai com 0 se nada sobrou, 1 se algo não pôde ser removido (o motivo fica na tela e no -LogPath).
#>
#Requires -RunAsAdministrator
param(
    # Arquivo onde guardar tudo o que este script mostrar (o desinstalador do TelaDois usa um no %TEMP%).
    [string]$LogPath
)
$ErrorActionPreference = 'Stop'
$HardwareId = 'Root\MttVDD'
$ConfigDir = 'C:\VirtualDisplayDriver'
$failures = 0

if ($LogPath) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $LogPath) | Out-Null
    Start-Transcript -Path $LogPath -Force | Out-Null
}

function Invoke-Step([string]$What, [scriptblock]$Action) {
    try { & $Action }
    catch {
        $script:failures++
        Write-Host "Não foi possível $($What): $($_.Exception.Message)"
    }
}

try {
    Invoke-Step 'apagar as tarefas do Agendador' {
        $service = New-Object -ComObject 'Schedule.Service'
        $service.Connect()
        $folder = $null
        try { $folder = $service.GetFolder('\TelaDois') } catch { Write-Host 'Tarefas do TelaDois: já não existiam.' }
        if ($folder) {
            foreach ($task in @($folder.GetTasks(1))) {
                $folder.DeleteTask($task.Name, 0)
                Write-Host "Tarefa \TelaDois\$($task.Name) apagada."
            }
            $service.GetFolder('\').DeleteFolder('TelaDois', 0)
        }
    }
    Invoke-Step 'remover o dispositivo do monitor virtual' {
        $devices = @(Get-PnpDevice -Class Display -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $HardwareId })
        if (-not $devices) { Write-Host 'Dispositivo do monitor virtual: já não existia.' }
        foreach ($device in $devices) {
            pnputil /remove-device "$($device.InstanceId)" | Write-Host
            if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) { throw "pnputil /remove-device terminou com o código $LASTEXITCODE." }
        }
    }
    Invoke-Step 'remover o pacote do driver' {
        # Get-WindowsDriver dá o nome publicado (oemNN.inf) sem depender do idioma da saída do pnputil.
        $packages = @(Get-WindowsDriver -Online | Where-Object { $_.OriginalFileName -like '*\MttVDD.inf' })
        if (-not $packages) { Write-Host 'Pacote do driver: já não existia.' }
        foreach ($package in $packages) {
            pnputil /delete-driver $package.Driver /uninstall /force | Write-Host
            if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) { throw "pnputil /delete-driver terminou com o código $LASTEXITCODE." }
        }
    }
    Invoke-Step "apagar $ConfigDir" {
        if (Test-Path $ConfigDir) { Remove-Item -Recurse -Force $ConfigDir; Write-Host "$ConfigDir apagada." }
    }
}
finally {
    if ($failures) { Write-Host "$failures passo(s) não terminaram; rode este script de novo como administrador." }
    else { Write-Host 'Monitor virtual removido.' }
    if ($LogPath) { Stop-Transcript | Out-Null }
}
if ($failures) { exit 1 }
```

- [ ] **Step 4: Rodar o teste dos scripts**

Run: `powershell -ExecutionPolicy Bypass -File tools\instalador\testar-scripts.ps1`
Expected: só `verificar-apple.ps1: não encontrado` e `montar.ps1: não encontrado` (nenhum erro de sintaxe nem de BOM em `instalar-vdd.ps1`/`desinstalar-vdd.ps1`). O comportamento real dos dois é testado como administrador na Task 7.

- [ ] **Step 5: Commit**

```bash
git add tools/vdd/instalar-vdd.ps1 tools/vdd/desinstalar-vdd.ps1 tools/instalador/testar-scripts.ps1
git commit -m "feat(tools): driver do monitor virtual instala sem internet, com registro e desinstalação" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Conferir o driver USB da Apple

**Files:**
- Create: `tools/instalador/verificar-apple.ps1`

**Interfaces:**
- Produces: `verificar-apple.ps1 [-LogPath <arquivo>]` → 0 (serviço `Apple Mobile Device Service` existe ou passou a existir) ou 2 (instalar à mão; o **instalador** abre a página `https://www.apple.com/itunes/download/win64` como o usuário original).

- [ ] **Step 1: Escrever o script** (UTF-8 com BOM):

```powershell
<#
.SYNOPSIS
  Confere o driver USB da Apple (Apple Mobile Device Support, que vem com o iTunes) e instala o iTunes se faltar.
.DESCRIPTION
  O TelaDois só enxerga o iPhone pelo serviço "Apple Mobile Device Service".
  Usa o winget com a fonte "winget" (o instalador do site da Apple), nunca a versão da Microsoft Store, que não serve.
  Sai com 0 se o serviço existe (ou passou a existir) e com 2 se é preciso instalar à mão:
  https://www.apple.com/itunes/download/win64 (o instalador do TelaDois abre essa página).
#>
param(
    # Arquivo onde guardar tudo o que este script mostrar.
    [string]$LogPath
)
$ServiceName = 'Apple Mobile Device Service'

if ($LogPath) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $LogPath) | Out-Null
    Start-Transcript -Path $LogPath -Force | Out-Null
}
$code = 2
try {
    if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
        Write-Host 'Driver USB da Apple: já instalado.'
        $code = 0
    } else {
        $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
        if ($winget) {
            Write-Host 'Driver USB da Apple não encontrado: instalando o iTunes pelo winget (pode levar alguns minutos)...'
            & $winget.Source install --id Apple.iTunes -e --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity | Write-Host
            Write-Host "winget terminou com o código $LASTEXITCODE."
            if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
                Write-Host 'Driver USB da Apple instalado.'
                $code = 0
            }
        } else {
            Write-Host 'winget não encontrado neste PC.'
        }
        if ($code -ne 0) { Write-Host 'Instale o iTunes à mão: https://www.apple.com/itunes/download/win64' }
    }
}
finally {
    if ($LogPath) { Stop-Transcript | Out-Null }
}
exit $code
```

- [ ] **Step 2: Rodar sem administrador (este PC)**

Run: `powershell -ExecutionPolicy Bypass -File tools\instalador\verificar-apple.ps1 -LogPath $env:TEMP\verificar-apple.log; $LASTEXITCODE`
Expected: se `Get-Service 'Apple Mobile Device Service'` já existe → `Driver USB da Apple: já instalado.` e `0`. Se não existe, **não rode agora** (instalaria o iTunes fora do instalador): rode só `Get-Service 'Apple Mobile Device Service'` e anote o resultado para a Task 7.

- [ ] **Step 3: Teste dos scripts**

Run: `powershell -ExecutionPolicy Bypass -File tools\instalador\testar-scripts.ps1`
Expected: só `montar.ps1: não encontrado`.

- [ ] **Step 4: Commit**

```bash
git add tools/instalador/verificar-apple.ps1
git commit -m "feat(tools): confere o driver USB da Apple e instala o iTunes pelo winget se faltar" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Assistente (Inno Setup) e montagem

**Files:**
- Create: `installer/TelaDois.iss`, `installer/montar.ps1`

**Interfaces:**
- Consumes: `TelaDois.ico`, `installer/imagens/*.png` (Task 2); `instalar-vdd.ps1 -DriverZip -LogPath` → 0/3010/1, `desinstalar-vdd.ps1 -LogPath` (Task 3); `verificar-apple.ps1 -LogPath` → 0/2 (Task 4).
- Produces: `installer/montar.ps1 [-Version <x.y.z[-sufixo]>]` → `installer/out/TelaDois-Setup-<versão>.exe` + `installer/out/TelaDois-Setup-<versão>.exe.sha256` (formato `<HASH>  <arquivo>`). Pasta montada: `installer/out/stage/{app,driver,ffmpeg,tools}`.

- [ ] **Step 1: `installer/montar.ps1`** (UTF-8 com BOM):

```powershell
<#
.SYNOPSIS
  Monta o instalador do TelaDois: installer\out\TelaDois-Setup-<versão>.exe e o .sha256 ao lado.
.DESCRIPTION
  Uso (na raiz do repositório):  powershell -ExecutionPolicy Bypass -File installer\montar.ps1 [-Version 0.6.0]
  1. Publica o TelaDois.exe em arquivo único, que não precisa do .NET instalado.
  2. Baixa o driver do monitor virtual e o FFmpeg (guardados em installer\cache) e confere os SHA-256 abaixo.
  3. Compila installer\TelaDois.iss com o Inno Setup 6 (ISCC.exe).
#>
param(
    # Versão do app e do instalador; o CI usa o número da tag (v0.6.0 → 0.6.0) ou 0.6.0-dev.<execução>.
    [string]$Version = '0.6.0'
)
$ErrorActionPreference = 'Stop'
# Sem isto, a barra de progresso do Invoke-WebRequest deixa o download do PowerShell 5.1 muito lento.
$ProgressPreference = 'SilentlyContinue'

$Root = Split-Path -Parent $PSScriptRoot
$Out = Join-Path $PSScriptRoot 'out'
$Stage = Join-Path $Out 'stage'
$Cache = Join-Path $PSScriptRoot 'cache'

# O do driver é o mesmo de tools\vdd\instalar-vdd.ps1, que confere de novo (e a assinatura) no PC do usuário.
$Driver = @{
    Url = 'https://github.com/VirtualDrivers/Virtual-Display-Driver/releases/download/25.7.23/VirtualDisplayDriver-x86.Driver.Only.zip'
    File = 'VirtualDisplayDriver-x86.Driver.Only.zip'
    Sha256 = 'E24210692B442B39AF763536330CE78B423F19342B7A7792C26DE3944E418B3A'
}
$Ffmpeg = @{
    Url = 'https://github.com/GyanD/codexffmpeg/releases/download/9.0.1/ffmpeg-9.0.1-essentials_build.zip'
    File = 'ffmpeg-9.0.1-essentials_build.zip'
    Sha256 = 'FEC81AE03971D9DD4BE3EBE02E263BD2EC1D789483F931BDBA5F5715E65DA2E9'
}

function Get-Verified($Item) {
    New-Item -ItemType Directory -Force $Cache | Out-Null
    $path = Join-Path $Cache $Item.File
    if ((Test-Path $path) -and (Get-FileHash $path -Algorithm SHA256).Hash -eq $Item.Sha256) { return $path }
    Write-Host "Baixando $($Item.File)..."
    Invoke-WebRequest -Uri $Item.Url -OutFile $path -UseBasicParsing
    $hash = (Get-FileHash $path -Algorithm SHA256).Hash
    if ($hash -ne $Item.Sha256) {
        Remove-Item $path
        throw "SHA-256 de $($Item.File) diferente do esperado ($hash): arquivo não confiável."
    }
    return $path
}

function Find-Iscc {
    $candidates = @(
        (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source,
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $found = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $found) { throw 'ISCC.exe (Inno Setup 6) não encontrado: winget install --id JRSoftware.InnoSetup -e' }
    return $found
}

$iscc = Find-Iscc
if (Test-Path $Stage) { Remove-Item -Recurse -Force $Stage }

Write-Host "Publicando o TelaDois $Version..."
$prefix, $suffix = $Version.Split('-', 2)
$versionArgs = @("-p:VersionPrefix=$prefix")
if ($suffix) { $versionArgs += "-p:VersionSuffix=$suffix" }
dotnet publish (Join-Path $Root 'windows\src\TelaDois.Tray\TelaDois.Tray.csproj') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none `
    @versionArgs -o (Join-Path $Stage 'app')
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou (código $LASTEXITCODE)." }

$driverDir = New-Item -ItemType Directory -Force (Join-Path $Stage 'driver')
Copy-Item (Get-Verified $Driver) $driverDir
'instalar-vdd.ps1', 'desinstalar-vdd.ps1', 'vdd_settings.xml' | ForEach-Object { Copy-Item (Join-Path $Root "tools\vdd\$_") $driverDir }

$toolsDir = New-Item -ItemType Directory -Force (Join-Path $Stage 'tools')
Copy-Item (Join-Path $Root 'tools\instalador\verificar-apple.ps1') $toolsDir

# Do FFmpeg vão só o ffmpeg.exe e a licença; o LEIA-ME diz de onde veio e onde está o código-fonte (GPL).
$ffmpegDir = New-Item -ItemType Directory -Force (Join-Path $Stage 'ffmpeg')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Get-Verified $Ffmpeg))
try {
    foreach ($name in 'bin/ffmpeg.exe', 'LICENSE') {
        $entry = $zip.Entries | Where-Object { $_.FullName -like "*/$name" } | Select-Object -First 1
        if (-not $entry) { throw "$name não está no zip do FFmpeg." }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $ffmpegDir (Split-Path $name -Leaf)), $true)
    }
}
finally {
    $zip.Dispose()
}
$readme = @"
FFmpeg 9.0.1 (build "essentials" de https://www.gyan.dev/ffmpeg/builds/), usado pelo TelaDois para capturar e
comprimir a tela. Licença: GPL versão 3 (arquivo LICENSE nesta pasta).
Código-fonte exato desta versão: https://github.com/FFmpeg/FFmpeg/tree/n9.0.1
Pacote original: $($Ffmpeg.Url)
"@
[IO.File]::WriteAllText((Join-Path $ffmpegDir 'LEIA-ME.txt'), $readme, (New-Object Text.UTF8Encoding $true))

Write-Host 'Compilando o instalador...'
& $iscc "/DAppVersion=$Version" "/DStage=$Stage" (Join-Path $PSScriptRoot 'TelaDois.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC falhou (código $LASTEXITCODE)." }

$setup = Join-Path $Out "TelaDois-Setup-$Version.exe"
$hash = (Get-FileHash $setup -Algorithm SHA256).Hash
[IO.File]::WriteAllText("$setup.sha256", "$hash  TelaDois-Setup-$Version.exe`n")
Write-Host ("Pronto: {0} ({1:N1} MB)  SHA-256 {2}" -f $setup, ((Get-Item $setup).Length / 1MB), $hash)
```

- [ ] **Step 2: `installer/TelaDois.iss`** (UTF-8 com BOM — o Inno Setup 6 lê `.iss` UTF-8 com BOM):

```
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
ptbr.SelectTasksLabel2=Tudo já vem marcado. Clique em Instalar.
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
begin
  { /T leva junto o ffmpeg.exe que o TelaDois abriu. Sem TelaDois aberto o taskkill só devolve 128. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM TelaDois.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
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
    AddWarning('O monitor virtual não foi instalado. O TelaDois funciona assim mesmo, mostrando no iPhone uma cópia de um monitor.' +
      #13#10 + 'Para tentar de novo, rode este instalador outra vez. Detalhes: ' + Log);
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
    if (Warnings <> '') and not WizardSilent then
      MsgBox(Warnings, mbInformation, MB_OK);
  end;
end;

function NeedRestart: Boolean;
begin
  Result := NeedsReboot;
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
```

- [ ] **Step 3: Montar localmente**

Run: `powershell -ExecutionPolicy Bypass -File installer\montar.ps1 -Version 0.6.0-local.1`
Expected: termina com `Pronto: ...\installer\out\TelaDois-Setup-0.6.0-local.1.exe (≈ 70–100 MB)  SHA-256 ...`, sem avisos do ISCC. Se o ISCC recusar os `.png` (`WizardImageFile`), gerar `.bmp` de 32 bits no `gerar-imagens.py` (`.convert("RGB")` sobre preto, extensão `.bmp`) e trocar as extensões no `.iss` — o fundo do assistente é preto, não precisa de transparência; para o ícone pequeno, `WizardImageAlphaFormat=defined` com BMP RGBA.

- [ ] **Step 4: Teste dos scripts**

Run: `powershell -ExecutionPolicy Bypass -File tools\instalador\testar-scripts.ps1`
Expected: `5 scripts ok.`, exit 0.

- [ ] **Step 5: Commit**

```bash
git add installer/TelaDois.iss installer/montar.ps1
git commit -m "feat(installer): assistente do Inno Setup e montagem com driver e FFmpeg conferidos" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: GitHub — workflow, Release e guias

**Files:**
- Create: `.github/workflows/windows-installer.yml`, `installer/NOTAS-DA-VERSAO.md`
- Modify: `README.md`, `docs/INSTALACAO.md`

**Interfaces:**
- Consumes: `installer/montar.ps1 -Version` e `tools/instalador/testar-scripts.ps1` (Tasks 3–5).
- Produces: artefato `TelaDois-Setup` em todo push; Release `vX.Y.Z` com `TelaDois-Setup-X.Y.Z.exe` e `.sha256` numa tag.

- [ ] **Step 1: Workflow** — `.github/workflows/windows-installer.yml`:

```yaml
name: windows-installer
on:
  push:
    branches: [main, "etapa-*"]
    tags: ["v*"]
  workflow_dispatch:

permissions:
  contents: write

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  installer:
    runs-on: windows-2025
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: windows/global.json
      - name: Testes do PC
        run: dotnet test windows/TelaDois.sln -c Release
      - name: Scripts do instalador (sintaxe e BOM, Windows PowerShell 5.1)
        shell: powershell
        run: ./tools/instalador/testar-scripts.ps1
      - name: Inno Setup 6.7.3 (SHA-256 conferido)
        shell: pwsh
        run: |
          $ProgressPreference = 'SilentlyContinue'
          $exe = Join-Path $env:RUNNER_TEMP 'innosetup-6.7.3.exe'
          Invoke-WebRequest https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe -OutFile $exe
          $hash = (Get-FileHash $exe -Algorithm SHA256).Hash
          if ($hash -ne '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732') { throw "SHA-256 do Inno Setup diferente: $hash" }
          Start-Process $exe -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/ALLUSERS' -Wait
      - name: Versão
        id: version
        shell: pwsh
        run: |
          # Tag v0.6.0 → 0.6.0; sem tag → 0.6.0-dev.<número da execução> (a base acompanha o VersionPrefix do Directory.Build.props).
          $v = if ($env:GITHUB_REF_TYPE -eq 'tag') { $env:GITHUB_REF_NAME.TrimStart('v') } else { "0.6.0-dev.$env:GITHUB_RUN_NUMBER" }
          "value=$v" >> $env:GITHUB_OUTPUT
      - name: Montar o instalador
        shell: powershell
        run: ./installer/montar.ps1 -Version '${{ steps.version.outputs.value }}'
      - uses: actions/upload-artifact@v7
        with:
          name: TelaDois-Setup
          path: |
            installer/out/TelaDois-Setup-*.exe
            installer/out/TelaDois-Setup-*.exe.sha256
          if-no-files-found: error
      - name: Release
        if: github.ref_type == 'tag'
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          $v = '${{ steps.version.outputs.value }}'
          # Pré-lançamento enquanto o commit da tag não estiver no main.
          git merge-base --is-ancestor $env:GITHUB_SHA origin/main
          $pre = if ($LASTEXITCODE -eq 0) { @() } else { @('--prerelease') }
          gh release create $env:GITHUB_REF_NAME "installer/out/TelaDois-Setup-$v.exe" "installer/out/TelaDois-Setup-$v.exe.sha256" `
            --title "TelaDois $v" --notes-file installer/NOTAS-DA-VERSAO.md --verify-tag @pre
          exit 0
```

- [ ] **Step 2: `installer/NOTAS-DA-VERSAO.md`**

```markdown
## Instalar no PC (Windows 10 2004 ou mais novo, 64 bits)

1. Baixe o **`TelaDois-Setup-….exe`** abaixo e abra.
2. Se aparecer **"O Windows protegeu o computador"**: clique em **Mais informações → Executar assim mesmo** (o instalador não tem assinatura paga).
3. Aceite o pedido de administrador e clique em **Instalar** — as opções já vêm marcadas.

O instalador põe o TelaDois, o FFmpeg e o monitor virtual; se faltar o driver USB da Apple, instala o iTunes. Para tirar: Configurações → Aplicativos → TelaDois → Desinstalar.

Conferir o arquivo (opcional): `Get-FileHash .\TelaDois-Setup-….exe` deve dar o mesmo valor do `.sha256`.

O app do iPhone continua sendo instalado pelo xtool: veja `docs/INSTALACAO.md`.
```

- [ ] **Step 3: README** — depois da linha "- Como instalar no iPhone: ...", acrescentar:

```markdown
- **Instalar no PC (Windows):** baixe o `TelaDois-Setup-….exe` da [página de versões](https://github.com/Pedro3720/teladois/releases) e abra — ele instala o app, o FFmpeg e o monitor virtual de uma vez. Detalhes em [docs/INSTALACAO.md](docs/INSTALACAO.md#no-pc-windows).
```

e trocar a última frase ("...instalado pelo Sideloadly com um Apple ID grátis.") por "...instalado pelo xtool (ou Sideloadly) com um Apple ID grátis."

- [ ] **Step 4: `docs/INSTALACAO.md`** — trocar o título por `# Instalar o TelaDois (PC e iPhone, sem conta paga)` e inserir logo depois dele:

```markdown
## No PC (Windows)

1. Baixe o `TelaDois-Setup-<versão>.exe` da versão mais nova em https://github.com/Pedro3720/teladois/releases e abra.
2. **"O Windows protegeu o computador"** (SmartScreen): **Mais informações → Executar assim mesmo**. Aparece porque o instalador não tem assinatura paga. Conferir o arquivo (opcional): `Get-FileHash <arquivo>` igual ao `.sha256` da página.
3. Aceite o pedido de administrador (é para o monitor virtual) e clique em **Instalar**. As três opções já vêm marcadas: monitor virtual, abrir com o Windows e atalho na Área de Trabalho.
4. O instalador:
   - coloca o TelaDois em `C:\Program Files\TelaDois`, com o FFmpeg em `ffmpeg\` (não precisa instalar à parte);
   - instala o monitor virtual (Virtual Display Driver 25.7.23, conferido pelo SHA-256 e pela assinatura) e as tarefas `\TelaDois\` do Agendador;
   - confere o driver USB da Apple; se faltar, instala o iTunes pelo winget (ou abre a página da Apple);
   - abre o TelaDois na bandeja, perto do relógio.
5. **Atualizar:** baixe a versão nova e abra por cima; as preferências ficam.
6. **Desinstalar:** Configurações → Aplicativos → TelaDois → Desinstalar (remove também o monitor virtual).

Registros: `C:\Program Files\TelaDois\logs\` (instalação) e `%LOCALAPPDATA%\TelaDois\tela-dois.log` (o app).
**PC com Controle de Aplicativos** (ex.: do trabalho): o Windows pode bloquear o instalador sem assinatura ou o script do monitor virtual. Sem o monitor virtual, o TelaDois mostra no iPhone uma cópia de um monitor.

## No iPhone
```

(e rebaixar os títulos seguintes "## Com o xtool (principal)" e "## Com o Sideloadly (alternativa)" para `###`, e "## Uma vez só", "## A cada nova versão", "## Validade" para `####`).

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/windows-installer.yml installer/NOTAS-DA-VERSAO.md README.md docs/INSTALACAO.md
git commit -m "ci: workflow do instalador do Windows e Release; guias de instalação no PC" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Instalar de verdade neste PC e revisar com a apple-design

**Files:** nenhum novo; correções que aparecerem vão para os arquivos das Tasks 2–6.

- [ ] **Step 1: Estado antes** — anotar: `Get-Service 'Apple Mobile Device Service'`, `Get-PnpDevice -Class Display | ? HardwareID -contains 'Root\MttVDD'`, `Get-ScheduledTask -TaskPath '\TelaDois\' -EA SilentlyContinue`, `Test-Path C:\VirtualDisplayDriver`.
- [ ] **Step 2: Instalar** — `Start-Process installer\out\TelaDois-Setup-0.6.0-local.1.exe` (o usuário aceita o UAC). Em cada página do assistente, capturar a tela (`[System.Windows.Forms.Screen]::PrimaryScreen` + `Graphics.CopyFromScreen` para um `.png` no scratchpad) e olhar o print.
- [ ] **Step 3: Conferir** — `C:\Program Files\TelaDois\TelaDois.exe` e `ffmpeg\ffmpeg.exe` existem; `TelaDois` rodando com o ícone novo na bandeja; registro `%LOCALAPPDATA%\TelaDois\tela-dois.log` com `TelaDois 0.6.0-local.1… iniciado`; `HKLM\…\Run\TelaDois` aponta para o `.exe`; atalho na Área de Trabalho; dispositivo `Root\MttVDD` com `Problem = CM_PROB_DISABLED`; tarefas `\TelaDois\Ligar monitor virtual` e `\TelaDois\Desligar monitor virtual`; `logs\instalar-vdd.log` termina com "Monitor virtual instalado"; `logs\verificar-apple.log`.
- [ ] **Step 4: Revisão apple-design** — invocar a skill `apple-design` com os prints do assistente e da bandeja; aplicar o que for cabível (textos, ordem, imagens), remontar, reinstalar por cima.
- [ ] **Step 5: Instalar por cima** — rodar o mesmo `.exe` de novo: termina sem erro, TelaDois reaberto, nada duplicado.
- [ ] **Step 6: Desinstalar** — Configurações → Aplicativos → TelaDois → Desinstalar. Conferir que sumiram: pasta `C:\Program Files\TelaDois`, valor `Run`, atalho, tarefas `\TelaDois\`, dispositivo `Root\MttVDD`, pacote `MttVDD.inf` (`Get-WindowsDriver -Online | ? OriginalFileName -like '*MttVDD.inf'`), `C:\VirtualDisplayDriver`; o `%TEMP%\TelaDois-desinstalar-vdd.log` diz "Monitor virtual removido."
- [ ] **Step 7: Reinstalar** (para o usuário ficar com o TelaDois instalado) e commitar as correções:

```bash
git add -A installer tools windows docs
git commit -m "fix(installer): ajustes da instalação real e da revisão apple-design" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Publicar no GitHub e fechar a etapa

- [ ] **Step 1:** `uv tool run --from graphifyy python -m graphify update .` e commitar `graphify-out/` (`chore: atualiza o grafo do graphify (Etapa 6a)`).
- [ ] **Step 2:** `CLAUDE.md`: estrutura de pastas (`installer/`, `tools/instalador/`), "Como rodar" (`installer\montar.ps1`), workflows (`windows-installer.yml`), "Estado atual" (Etapa 6a pronta, testada neste PC `D:\Projetos\TelaDois`), tabela "O que está em cada PC" com este PC (.NET 10.0.403, gh 2.103.3, Inno Setup 6.7.3, sem WSL/xtool). Commit `docs: Etapa 6a no CLAUDE.md`.
- [ ] **Step 3:** `gh auth status` (o usuário faz `gh auth login` se preciso). `git push -u origin etapa-6a-instalador`.
- [ ] **Step 4:** acompanhar `windows-test` e `windows-installer` (`gh run list --branch etapa-6a-instalador`, `gh run watch <id>`); ambos verdes. Baixar o artefato e conferir o `.sha256`.
- [ ] **Step 5:** **com a aprovação do usuário**, `git tag v0.6.0` no último commit verde e `git push origin v0.6.0` → o workflow cria a Release (pré-lançamento, porque não está no `main`; por isso os guias apontam para `/releases`, não `/releases/latest`). Conferir `https://github.com/Pedro3720/teladois/releases/tag/v0.6.0` com o `.exe` e o `.sha256`.
