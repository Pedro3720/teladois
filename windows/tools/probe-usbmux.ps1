param([int]$DeviceId, [int[]]$Ports = @(62078, 7100))
# Pede ao usbmuxd um túnel até cada porta do iPhone e mostra o código de resposta (0 = ok, 3 = recusado).
function Send-Usbmux([System.IO.Stream]$s, [string]$body, [uint32]$tag) {
    $plist = [Text.Encoding]::UTF8.GetBytes("<?xml version=`"1.0`" encoding=`"UTF-8`"?>`n<plist version=`"1.0`"><dict>$body</dict></plist>`n")
    $len = 16 + $plist.Length
    $hdr = [BitConverter]::GetBytes([uint32]$len) + [BitConverter]::GetBytes([uint32]1) + [BitConverter]::GetBytes([uint32]8) + [BitConverter]::GetBytes($tag)
    $s.Write([byte[]]($hdr + $plist), 0, $len)
    $h = New-Object byte[] 16; $r = 0; while ($r -lt 16) { $r += $s.Read($h, $r, 16 - $r) }
    $n = [BitConverter]::ToUInt32($h, 0) - 16
    $b = New-Object byte[] $n; $r = 0; while ($r -lt $n) { $r += $s.Read($b, $r, $n - $r) }
    [Text.Encoding]::UTF8.GetString($b)
}
$common = "<key>ClientVersionString</key><string>probe</string><key>ProgName</key><string>probe</string><key>kLibUSBMuxVersion</key><integer>3</integer>"
$tcp = New-Object Net.Sockets.TcpClient('127.0.0.1', 27015)
$list = Send-Usbmux $tcp.GetStream() "<key>MessageType</key><string>ListDevices</string>$common" 1
$tcp.Close()
"ListDevices DeviceIDs: " + (([regex]'<key>DeviceID</key>\s*<integer>(\d+)</integer>').Matches($list) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique) -join ", "
"ConnectionTypes: " + (([regex]'<key>ConnectionType</key>\s*<string>(\w+)</string>').Matches($list) | ForEach-Object { $_.Groups[1].Value }) -join ", "
foreach ($port in $Ports) {
    $swapped = [int](([uint16]$port -shr 8) -bor (([uint16]$port -band 0xFF) -shl 8))
    $tcp = New-Object Net.Sockets.TcpClient('127.0.0.1', 27015)
    $reply = Send-Usbmux $tcp.GetStream() "<key>MessageType</key><string>Connect</string>$common<key>DeviceID</key><integer>$DeviceId</integer><key>PortNumber</key><integer>$swapped</integer>" 2
    $code = ([regex]'<key>Number</key>\s*<integer>(\d+)</integer>').Match($reply).Groups[1].Value
    "porta $port -> código $code"
    $tcp.Close()
}
