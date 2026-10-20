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
