<#
.SYNOPSIS
  Confere o driver USB da Apple (serviço "Apple Mobile Device Service") e, se faltar, instala só ele.
.DESCRIPTION
  O TelaDois só enxerga o iPhone pelo serviço "Apple Mobile Device Service".
  O winget com --silent instala o iTunes mas NÃO instala o driver (o AppleMobileDeviceSupport64.msi fica
  dentro do iTunes64Setup.exe e o instalador silencioso não o executa). Por isso este script:
    1. baixa o iTunes64Setup.exe com "winget download" (o winget confere o SHA-256 do manifesto);
    2. extrai do .exe o pacote CAB e dele o AppleMobileDeviceSupport64.msi;
    3. só segue se o .msi tiver assinatura válida da Apple Inc.;
    4. instala o .msi com o msiexec (não instala o iTunes).
  Sai com 0 se o serviço existe (ou passou a existir) e com 2 se é preciso instalar à mão:
  https://www.apple.com/itunes/download/win64 (o instalador do TelaDois abre essa página).
#>
param(
    # Arquivo onde guardar tudo o que este script mostrar.
    [string]$LogPath
)
$ServiceName = 'Apple Mobile Device Service'
$MsiName = 'AppleMobileDeviceSupport64.msi'

# Posição da primeira assinatura "MSCF" (início do pacote CAB dentro do instalador), lendo em blocos. -1 se não achar.
function Find-CabOffset([string]$Path) {
    $latin1 = [Text.Encoding]::GetEncoding(28591)
    $chunkSize = 4MB
    $overlap = 3
    $stream = [IO.File]::OpenRead($Path)
    try {
        $buffer = New-Object byte[] ($chunkSize + $overlap)
        $start = 0
        $carry = 0
        while ($true) {
            $read = $stream.Read($buffer, $carry, $chunkSize)
            if ($read -le 0) { return -1 }
            $total = $carry + $read
            $index = $latin1.GetString($buffer, 0, $total).IndexOf('MSCF', [StringComparison]::Ordinal)
            if ($index -ge 0) { return ($start + $index) }
            if ($total -le $overlap) { return -1 }
            # Guarda o fim do bloco: a assinatura pode atravessar dois blocos.
            [Array]::Copy($buffer, $total - $overlap, $buffer, 0, $overlap)
            $start += $total - $overlap
            $carry = $overlap
        }
    }
    finally { $stream.Dispose() }
}

# Grava em $Dest os bytes de $Source a partir de $Offset.
function Save-FromOffset([string]$Source, [long]$Offset, [string]$Dest) {
    $in = [IO.File]::OpenRead($Source)
    try {
        $null = $in.Seek($Offset, [IO.SeekOrigin]::Begin)
        $out = [IO.File]::Create($Dest)
        try { $in.CopyTo($out) } finally { $out.Dispose() }
    }
    finally { $in.Dispose() }
}

# Baixa o instalador do iTunes, extrai o .msi do driver e confere a assinatura. Devolve o caminho do .msi ou $null.
function Get-AppleDriverMsi([string]$WorkDir) {
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if (-not $winget) {
        Write-Host 'winget não encontrado neste PC.'
        return $null
    }

    Write-Host 'Baixando o instalador da Apple (o winget confere o arquivo)...'
    $downloadDir = Join-Path $WorkDir 'baixado'
    New-Item -ItemType Directory -Force $downloadDir | Out-Null
    & $winget.Source download --id Apple.iTunes -e --source winget --download-directory $downloadDir --accept-package-agreements --accept-source-agreements --disable-interactivity | Write-Host
    Write-Host "winget terminou com o código $LASTEXITCODE."
    if ($LASTEXITCODE -ne 0) { return $null }
    $setup = Get-ChildItem -Path $downloadDir -Filter '*.exe' -File -ErrorAction SilentlyContinue |
        Sort-Object @{ Expression = { $_.Name -like '*64*' }; Descending = $true } | Select-Object -First 1
    if (-not $setup) {
        Write-Host 'O winget não baixou o instalador da Apple.'
        return $null
    }

    Write-Host 'Extraindo o driver USB do instalador...'
    $offset = Find-CabOffset $setup.FullName
    if ($offset -lt 0) {
        Write-Host 'Não achei o pacote do driver dentro do instalador da Apple.'
        return $null
    }
    $cab = Join-Path $WorkDir 'pacote.cab'
    Save-FromOffset $setup.FullName $offset $cab
    $extractDir = Join-Path $WorkDir 'extraido'
    New-Item -ItemType Directory -Force $extractDir | Out-Null
    & (Join-Path $env:SystemRoot 'System32\expand.exe') "-F:$MsiName" $cab $extractDir | Write-Host
    $msi = Join-Path $extractDir $MsiName
    if (($LASTEXITCODE -ne 0) -or -not (Test-Path -LiteralPath $msi)) {
        Write-Host "Não consegui extrair o $MsiName (expand.exe terminou com o código $LASTEXITCODE)."
        return $null
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $msi
    if (($signature.Status -ne 'Valid') -or -not $signature.SignerCertificate -or ($signature.SignerCertificate.Subject -notlike 'CN=Apple Inc.,*')) {
        Write-Host "A assinatura do $MsiName não é da Apple Inc. (estado: $($signature.Status)). Por segurança, não vou instalar."
        return $null
    }
    Write-Host "Assinatura conferida: $($signature.SignerCertificate.Subject)"
    return $msi
}

if ($LogPath) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $LogPath) | Out-Null
    Start-Transcript -Path $LogPath -Force | Out-Null
}
$code = 2
$workDir = Join-Path ([IO.Path]::GetTempPath()) ('TelaDois-apple-' + [Guid]::NewGuid().ToString('N'))
try {
    if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
        Write-Host 'Driver USB da Apple: já instalado.'
        $code = 0
    } else {
        Write-Host 'Instalando o driver USB da Apple (pode levar alguns minutos)...'
        New-Item -ItemType Directory -Force $workDir | Out-Null
        $msi = Get-AppleDriverMsi $workDir
        if ($msi) {
            $msiLog = if ($LogPath) { Join-Path (Split-Path -Parent $LogPath) 'apple-msi.log' } else { Join-Path $workDir 'apple-msi.log' }
            $process = Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\msiexec.exe') `
                -ArgumentList @('/i', "`"$msi`"", '/qn', '/norestart', '/l*v', "`"$msiLog`"") -Wait -PassThru
            Write-Host "msiexec terminou com o código $($process.ExitCode)."
            if (($process.ExitCode -eq 0) -or ($process.ExitCode -eq 3010)) {
                if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
                    Write-Host 'Driver USB da Apple instalado.'
                    $code = 0
                }
            }
        }
        if ($code -ne 0) { Write-Host 'Instale o iTunes à mão: https://www.apple.com/itunes/download/win64' }
    }
}
catch {
    Write-Host "Erro ao instalar o driver USB da Apple: $($_.Exception.Message)"
    $code = 2
    Write-Host 'Instale o iTunes à mão: https://www.apple.com/itunes/download/win64'
}
finally {
    Remove-Item -LiteralPath $workDir -Recurse -Force -ErrorAction SilentlyContinue
    if ($LogPath) { Stop-Transcript | Out-Null }
}
exit $code
