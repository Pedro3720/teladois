<#
  Diagnóstico do erro 1167 ao ligar o monitor virtual (TelaDois, Etapa 5c/5d).
  Rode num PowerShell COMO ADMINISTRADOR, na pasta do projeto:
    powershell -ExecutionPolicy Bypass -File tools\vdd\diagnostico-1167.ps1
  Tenta ligar o dispositivo do Virtual Display Driver de 3 jeitos, mostra o resultado de cada um
  e no fim deixa o dispositivo DESATIVADO de novo (como o TelaDois espera). Não mexe em mais nada.
#>
#Requires -RunAsAdministrator
$ErrorActionPreference = 'Continue'
$id = (Get-PnpDevice -Class Display | Where-Object { $_.HardwareID -contains 'Root\MttVDD' } | Select-Object -First 1).InstanceId
if (-not $id) { Write-Host 'Dispositivo do monitor virtual não encontrado.'; exit 1 }
function Estado { (Get-PnpDevice -InstanceId $id).Status }
Write-Host "Dispositivo: $id  (estado inicial: $(Estado))"

Write-Host "`n[1] pnputil /enable-device"
pnputil /enable-device "$id" | Out-Host
Write-Host "    código: $LASTEXITCODE  estado: $(Estado)"

if ((Estado) -ne 'OK') {
    Write-Host "`n[2] Enable-PnpDevice"
    try { Enable-PnpDevice -InstanceId $id -Confirm:$false -ErrorAction Stop; Write-Host '    sem erro' } catch { Write-Host "    erro: $($_.Exception.Message)" }
    Start-Sleep -Seconds 2
    Write-Host "    estado: $(Estado)"
}

if ((Estado) -ne 'OK') {
    Write-Host "`n[3] pnputil /scan-devices + /restart-device + /enable-device"
    pnputil /scan-devices | Out-Host
    pnputil /restart-device "$id" | Out-Host
    Write-Host "    restart código: $LASTEXITCODE"
    pnputil /enable-device "$id" | Out-Host
    Write-Host "    enable código: $LASTEXITCODE  estado: $(Estado)"
}

Write-Host "`n[fim] tarefa agendada 'Ligar' (como o TelaDois faz):"
schtasks /run /tn "\TelaDois\Ligar monitor virtual" | Out-Host
Start-Sleep -Seconds 3
Write-Host "    estado depois da tarefa: $(Estado)"
schtasks /query /tn "\TelaDois\Ligar monitor virtual" /v /fo LIST | Select-String 'resultado|Result' | Out-Host

Write-Host "`nDesativando de novo..."
pnputil /disable-device "$id" | Out-Host
Write-Host "    código: $LASTEXITCODE  estado final: $(Estado)"
Write-Host "`nPronto. Copie tudo acima e mande para o Claude."
