<#
.SYNOPSIS
  Instala o Virtual Display Driver 25.7.23 (o monitor extra da Segunda Tela do TelaDois).
.DESCRIPTION
  Rode uma vez por PC, num PowerShell aberto como administrador:
    powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1
  Baixa só o pacote do driver (versão e SHA-256 fixos), confere a assinatura, grava
  C:\VirtualDisplayDriver\vdd_settings.xml e cria o dispositivo Root\MttVDD.
  O pacote 25.7.23 é assinado pela SignPath Foundation: o Windows pode perguntar se confia
  nesse fornecedor antes de instalar.
  Também cria duas tarefas agendadas sob demanda (\TelaDois\Ligar monitor virtual e \TelaDois\Desligar
  monitor virtual, como SYSTEM, disparáveis por qualquer usuário) e deixa o dispositivo DESATIVADO ao fim:
  o monitor virtual só aparece no Windows enquanto o TelaDois estiver com o modo monitor ligado.
  Depois disso o TelaDois não precisa de administrador. Se algo falhar depois da instalação do driver,
  basta rodar o instalador de novo.
#>
#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
# Sem isto, a barra de progresso do Invoke-WebRequest deixa o download do PowerShell 5.1 muito lento.
$ProgressPreference = 'SilentlyContinue'

$Version = '25.7.23'
$Asset = 'VirtualDisplayDriver-x86.Driver.Only.zip'
$Sha256 = 'E24210692B442B39AF763536330CE78B423F19342B7A7792C26DE3944E418B3A'
$Url = "https://github.com/VirtualDrivers/Virtual-Display-Driver/releases/download/$Version/$Asset"
$HardwareId = 'Root\MttVDD'
$ConfigDir = 'C:\VirtualDisplayDriver'
$Signer = 'SignPath Foundation'

# O Controle de Aplicativos do Windows (ex.: PC do trabalho) pode deixar o PowerShell em modo de linguagem restrito,
# que bloqueia o Add-Type usado para criar o dispositivo.
$languageMode = $ExecutionContext.SessionState.LanguageMode
$restrictedMessage = "O PowerShell está em modo de linguagem restrito ($languageMode), provavelmente pelo Controle de Aplicativos do Windows: este instalador não consegue criar o monitor virtual. Peça ao administrador do PC para instalar o driver ou liberar o script. Sem o driver, o TelaDois duplica um monitor."
if ($languageMode -ne 'FullLanguage') {
    Write-Warning $restrictedMessage
}

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
            var data = new SpDevinfoData { CbSize = (uint)Marshal.SizeOf(typeof(SpDevinfoData)) };
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
    $subject = $signature.SignerCertificate.Subject
    if ($subject -notlike "*$Signer*") { throw "O driver está assinado por '$subject', não pela $($Signer): nada foi instalado." }
    Write-Host "Assinado por: $subject"

    New-Item -ItemType Directory -Force $ConfigDir | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'vdd_settings.xml') (Join-Path $ConfigDir 'vdd_settings.xml') -Force
    Write-Host "Configuração gravada em $ConfigDir\vdd_settings.xml"

    $existing = Get-PnpDevice -Class Display -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $HardwareId }
    if ($existing) {
        Write-Host 'O monitor virtual já existe: atualizando o driver.'
        pnputil /add-driver $inf.FullName /install | Write-Host
        # 259 (ERROR_NO_MORE_ITEMS) = o driver já estava instalado e atualizado: nada a fazer, segue.
        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010 -and $LASTEXITCODE -ne 259) { throw "pnputil falhou (código $LASTEXITCODE)." }
    } else {
        if ($languageMode -ne 'FullLanguage') { throw $restrictedMessage }
        Add-Type -TypeDefinition $DeviceCode
        if ([TelaDoisVdd]::Install($HardwareId, $inf.FullName)) { Write-Host 'O Windows pediu reinício para terminar a instalação.' }
    }
    # O Windows só deixa de listar o monitor com o dispositivo desativado (precisa de administrador).
    # Duas tarefas sob demanda, como SYSTEM, que qualquer usuário do PC pode disparar: o TelaDois liga/desliga sem senha.
    # Os objetos COM do Agendador de Tarefas também são bloqueados no modo de linguagem restrito.
    if ($languageMode -ne 'FullLanguage') { throw $restrictedMessage }
    $device = Get-PnpDevice -Class Display -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $HardwareId } | Select-Object -First 1
    if (-not $device) { throw 'O dispositivo do monitor virtual não apareceu depois da instalação.' }
    $instanceId = $device.InstanceId
    $service = New-Object -ComObject 'Schedule.Service'
    $service.Connect()
    $root = $service.GetFolder('\')
    try { $folder = $root.GetFolder('\TelaDois') } catch { $folder = $root.CreateFolder('TelaDois') }
    foreach ($pair in @(@('Ligar monitor virtual', '/enable-device'), @('Desligar monitor virtual', '/disable-device'))) {
        $definition = $service.NewTask(0)
        $definition.RegistrationInfo.Description = 'TelaDois: liga/desliga o monitor virtual (Virtual Display Driver).'
        $definition.Settings.ExecutionTimeLimit = 'PT1M'
        $definition.Settings.MultipleInstances = 2   # TASK_INSTANCES_IGNORE_NEW
        $definition.Settings.DisallowStartIfOnBatteries = $false
        $definition.Settings.StopIfGoingOnBatteries = $false
        $definition.Principal.UserId = 'SYSTEM'
        $definition.Principal.LogonType = 5          # TASK_LOGON_SERVICE_ACCOUNT
        $definition.Principal.RunLevel = 1           # TASK_RUNLEVEL_HIGHEST
        $action = $definition.Actions.Create(0)      # TASK_ACTION_EXEC
        $action.Path = "$env:SystemRoot\System32\pnputil.exe"
        $action.Arguments = "$($pair[1]) `"$instanceId`""
        # SYSTEM e administradores: tudo; Usuários (BU): ler e executar.
        $sddl = 'D:(A;;FA;;;SY)(A;;FA;;;BA)(A;;GRGX;;;BU)'
        $folder.RegisterTaskDefinition($pair[0], $definition, 6, 'SYSTEM', $null, 5, $sddl) | Out-Null   # 6 = criar ou atualizar
        Write-Host "Tarefa \TelaDois\$($pair[0]) pronta."
    }
    pnputil /disable-device "$instanceId" | Write-Host
    if ($LASTEXITCODE -eq 3010) {
        Write-Host 'Monitor virtual desativado, mas o monitor só some da lista do Windows depois de reiniciar o Windows.'
    } elseif ($LASTEXITCODE -ne 0) {
        throw "pnputil não conseguiu desativar o monitor virtual (código $LASTEXITCODE). Rode o instalador de novo."
    } else {
        Write-Host 'Monitor virtual desativado: o Windows só vai mostrá-lo quando o TelaDois ligar o modo monitor.'
    }
    Write-Host 'Monitor virtual instalado e deixado desativado. O TelaDois ativa o dispositivo só enquanto o modo monitor estiver ligado. Abra (ou reabra) o TelaDois.'
    Write-Host 'Se algo falhar depois da instalação do driver, basta rodar este instalador de novo.'
}
finally {
    Remove-Item -Recurse -Force $work
}
