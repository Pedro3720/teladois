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
