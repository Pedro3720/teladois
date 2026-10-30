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
