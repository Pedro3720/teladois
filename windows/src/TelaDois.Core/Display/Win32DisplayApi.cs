using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

/// <summary>
/// <see cref="IDisplayApi"/> com o <c>user32</c>. Não precisa de administrador. Com <paramref name="persist"/>, ligar e
/// desligar ficam gravados no registro do Windows (valem depois de reiniciar); sem, valem só até reiniciar.
/// </summary>
public sealed class Win32DisplayApi(bool persist) : IDisplayApi
{
    /// <summary>
    /// A do monitor virtual: grava no registro. Medido no Virtual Display Driver 25.7.23 (2026-10-20): sem gravar,
    /// ligar o monitor que está fora do desktop falha com DISP_CHANGE_FAILED (-1); gravando, ele entra em Estender e
    /// aparece na DXGI na hora. Efeito bom: desligado assim, continua desligado depois de reiniciar o Windows.
    /// </summary>
    public static Win32DisplayApi ForVirtualMonitor() => new(persist: true);

    public bool Persist => persist;

    private const int AttachedToDesktop = 0x1;
    private const int PrimaryDevice = 0x4;
    private const int DmPosition = 0x20;
    private const int DmPelsWidth = 0x80000;
    private const int DmPelsHeight = 0x100000;
    private const int DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1;
    private const uint CdsNoReset = 0x10000000;
    private const int EnumCurrentSettings = -1;
    private const int MdtEffectiveDpi = 0;
    private static readonly IntPtr PerMonitorAwareV2 = new(-4); // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2

    public IReadOnlyList<DisplayDeviceInfo> Devices()
    {
        var devices = new List<DisplayDeviceInfo>();
        for (uint index = 0; ; index++)
        {
            var device = new DisplayDevice { cb = Marshal.SizeOf<DisplayDevice>() };
            if (!EnumDisplayDevices(null, index, ref device, 0))
            {
                return devices;
            }
            devices.Add(new DisplayDeviceInfo(device.DeviceName, device.DeviceString,
                (device.StateFlags & AttachedToDesktop) != 0, (device.StateFlags & PrimaryDevice) != 0));
        }
    }

    public IReadOnlyList<DisplayModeInfo> Modes(string deviceName)
    {
        var modes = new List<DisplayModeInfo>();
        for (var index = 0; ; index++)
        {
            var mode = NewMode();
            if (!EnumDisplaySettings(deviceName, index, ref mode))
            {
                return modes;
            }
            modes.Add(new DisplayModeInfo(mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency));
        }
    }

    public DisplayBounds? Bounds(string deviceName)
    {
        var mode = NewMode();
        return EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode) && mode.dmPelsWidth > 0
            ? new DisplayBounds(mode.dmPositionX, mode.dmPositionY, mode.dmPelsWidth, mode.dmPelsHeight)
            : null;
    }

    public int Apply(string deviceName, int x, int y, int width, int height, int frequency)
    {
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight | (frequency > 0 ? DmDisplayFrequency : 0);
        mode.dmPositionX = x;
        mode.dmPositionY = y;
        mode.dmPelsWidth = width;
        mode.dmPelsHeight = height;
        mode.dmDisplayFrequency = frequency;
        if (!persist)
        {
            return ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, 0, IntPtr.Zero);
        }
        var staged = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsUpdateRegistry | CdsNoReset, IntPtr.Zero);
        return staged != 0 ? staged : ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    }

    /// <summary>
    /// Acha o <c>HMONITOR</c> pelo nome (<c>EnumDisplayMonitors</c> + <c>GetMonitorInfo</c>) e lê o DPI efetivo. A thread
    /// fica com reconhecimento de DPI por monitor (V2) só durante a consulta: sem isso, um processo sem esse modo recebe
    /// sempre 96 do Windows.
    /// </summary>
    public int? EffectiveDpi(string deviceName)
    {
        var previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        try
        {
            var found = IntPtr.Zero;
            MonitorEnumProc callback = (monitor, _, _, _) =>
            {
                var info = new MonitorInfoEx { cbSize = Marshal.SizeOf<MonitorInfoEx>() };
                if (GetMonitorInfo(monitor, ref info) && string.Equals(info.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    found = monitor;
                    return false;
                }
                return true;
            };
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            GC.KeepAlive(callback);
            if (found == IntPtr.Zero)
            {
                return null;
            }
            return GetDpiForMonitor(found, MdtEffectiveDpi, out var dpiX, out _) == 0 && dpiX > 0 ? (int)dpiX : null;
        }
        finally
        {
            if (previous != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previous);
            }
        }
    }

    private static DevMode NewMode() => new() { dmSize = (short)Marshal.SizeOf<DevMode>() };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string? device, uint index, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode mode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref DevMode mode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, IntPtr mode, IntPtr hwnd, uint flags, IntPtr param);
}
