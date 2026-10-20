using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

/// <summary>
/// <see cref="IDisplayApi"/> com o <c>user32</c>. Não precisa de administrador. Com <paramref name="persist"/>, ligar e
/// desligar ficam gravados no registro do Windows (valem depois de reiniciar); sem, valem só até reiniciar.
/// </summary>
public sealed class Win32DisplayApi(bool persist) : IDisplayApi
{
    private const int AttachedToDesktop = 0x1;
    private const int PrimaryDevice = 0x4;
    private const int DmPosition = 0x20;
    private const int DmPelsWidth = 0x80000;
    private const int DmPelsHeight = 0x100000;
    private const int DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1;
    private const uint CdsNoReset = 0x10000000;
    private const int EnumCurrentSettings = -1;

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
