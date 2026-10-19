using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

/// <summary>
/// Liga e desliga o monitor do Virtual Display Driver no desktop do Windows. O driver mantém o
/// monitor instalado (não aceita 0 monitores); "desligar" é tirar do desktop (tamanho 0), o que faz
/// o Windows devolver as janelas ao monitor principal. Não precisa de administrador.
/// </summary>
public static class DisplayManager
{
    public const string VirtualAdapterName = "Virtual Display";

    private const int AttachedToDesktop = 0x1;
    private const int PrimaryDevice = 0x4;
    private const int DmPosition = 0x20;
    private const int DmPelsWidth = 0x80000;
    private const int DmPelsHeight = 0x100000;
    private const int DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1;
    private const uint CdsNoReset = 0x10000000;
    private const int DispChangeSuccessful = 0;
    private const int EnumCurrentSettings = -1;

    /// <summary>Nome do dispositivo (ex.: <c>\\.\DISPLAY3</c>) do monitor virtual, ligado ou não.</summary>
    public static string? FindVirtualDisplay() => FindVirtualAdapter()?.DeviceName;

    public static bool IsAttached(string deviceName) =>
        Adapters().Any(a => a.DeviceName == deviceName && (a.StateFlags & AttachedToDesktop) != 0);

    public static void Attach(string deviceName, uint width, uint height, uint fps)
    {
        var primary = Adapters().First(a => (a.StateFlags & PrimaryDevice) != 0);
        var primaryMode = CurrentMode(primary.DeviceName);
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight | DmDisplayFrequency;
        mode.dmPositionX = primaryMode.dmPelsWidth; // à direita do principal
        mode.dmPositionY = 0;
        mode.dmPelsWidth = (int)width;
        mode.dmPelsHeight = (int)height;
        mode.dmDisplayFrequency = (int)fps;
        Apply(deviceName, ref mode, "ligar");
    }

    public static void Detach(string deviceName)
    {
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight;
        Apply(deviceName, ref mode, "desligar");
    }

    /// <summary>Ao iniciar: desliga o monitor virtual que tenha sobrado de uma execução anterior (spec §5).</summary>
    public static void DetachLeftover(Action<string> log)
    {
        if (FindVirtualAdapter() is { } adapter && (adapter.StateFlags & AttachedToDesktop) != 0)
        {
            Detach(adapter.DeviceName);
            log($"monitor virtual que sobrou foi desligado ({adapter.DeviceName})");
        }
    }

    private static DisplayDevice? FindVirtualAdapter()
    {
        foreach (var adapter in Adapters())
        {
            if (adapter.DeviceString.Contains(VirtualAdapterName, StringComparison.OrdinalIgnoreCase))
            {
                return adapter;
            }
        }
        return null;
    }

    private static void Apply(string deviceName, ref DevMode mode, string action)
    {
        var staged = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsUpdateRegistry | CdsNoReset, IntPtr.Zero);
        var applied = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (staged != DispChangeSuccessful || applied != DispChangeSuccessful)
        {
            throw new InvalidOperationException($"o Windows não deixou {action} o monitor virtual (códigos {staged}/{applied})");
        }
    }

    private static IEnumerable<DisplayDevice> Adapters()
    {
        for (uint i = 0; ; i++)
        {
            var device = new DisplayDevice { cb = Marshal.SizeOf<DisplayDevice>() };
            if (!EnumDisplayDevices(null, i, ref device, 0))
            {
                yield break;
            }
            yield return device;
        }
    }

    private static DevMode CurrentMode(string deviceName)
    {
        var mode = NewMode();
        EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode);
        return mode;
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
