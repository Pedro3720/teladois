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
