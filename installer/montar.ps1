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

# Do FFmpeg vão só o ffmpeg.exe, a licença e a lista de bibliotecas do build; o LEIA-ME diz de onde veio e onde está o código-fonte (GPL).
$ffmpegDir = New-Item -ItemType Directory -Force (Join-Path $Stage 'ffmpeg')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Get-Verified $Ffmpeg))
try {
    # README.txt do pacote lista as bibliotecas embutidas e a configuração do build (exigência da GPL); vai como BUILD-INFO.txt.
    foreach ($item in @(@('bin/ffmpeg.exe', 'ffmpeg.exe'), @('LICENSE', 'LICENSE'), @('README.txt', 'BUILD-INFO.txt'))) {
        $name = $item[0]
        $entry = $zip.Entries | Where-Object { $_.FullName -like "*/$name" } | Select-Object -First 1
        if (-not $entry) { throw "$name não está no zip do FFmpeg." }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $ffmpegDir $item[1]), $true)
    }
}
finally {
    $zip.Dispose()
}
$readme = @"
FFmpeg 9.0.1, build "essentials" de Gyan Doshi, usado pelo TelaDois para capturar e comprimir a tela.
Licença: GPL versão 3 (arquivo LICENSE nesta pasta).
As bibliotecas incluídas neste build e a configuração usada para compilá-lo estão listadas no arquivo
BUILD-INFO.txt, nesta pasta.
Código-fonte do FFmpeg desta versão: https://github.com/FFmpeg/FFmpeg/tree/n9.0.1
Página do build e código-fonte das bibliotecas incluídas: https://www.gyan.dev/ffmpeg/builds/
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
