namespace TelaDois.Core.Display;

/// <summary>
/// O monitor do Virtual Display Driver. O driver mantém 1 monitor sempre instalado (<c>SETDISPLAYCOUNT 0</c> vira 1);
/// ligar é pô-lo no desktop em Estender, à direita do monitor mais à direita; desligar é tirá-lo (tamanho 0), e o
/// Windows devolve as janelas aos outros monitores.
/// </summary>
public sealed class VirtualDisplay(IDisplayApi api, IDriverPipe pipe) : IVirtualMonitor
{
    /// <summary>Nome do adaptador no Windows (<c>DeviceName</c> do <c>MttVDD.inf</c>); conferido no driver real (Task 2).</summary>
    public const string AdapterName = "Virtual Display Driver";

    public const int Fps = 60;

    public Task<bool> IsInstalledAsync(CancellationToken ct) => pipe.PingAsync(ct);

    /// <summary><c>\\.\DISPLAYn</c> do monitor do driver, ligado ou não; <c>null</c> sem o driver.</summary>
    public string? Find() => api.Devices().FirstOrDefault(IsVirtual)?.DeviceName;

    public bool IsAttached(string deviceName) =>
        api.Devices().Any(d => d.DeviceName == deviceName && d.AttachedToDesktop);

    public (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height)
    {
        var device = Find() ?? throw new InvalidOperationException("o monitor do Virtual Display Driver não aparece no Windows");
        var mode = ChooseMode(api.Modes(device), width, height) ?? throw new InvalidOperationException("o monitor virtual não informou nenhuma resolução");
        var right = api.Devices()
            .Where(d => d.AttachedToDesktop && d.DeviceName != device)
            .Select(d => api.Bounds(d.DeviceName))
            .Where(b => b.HasValue)
            .Select(b => b!.Value.X + b.Value.Width)
            .DefaultIfEmpty(0)
            .Max();
        Check(api.Apply(device, right, 0, mode.Width, mode.Height, mode.Frequency), "ligar");
        return (device, mode);
    }

    public void Detach(string deviceName) => Check(api.Apply(deviceName, 0, 0, 0, 0, 0), "desligar");

    /// <summary>Ao abrir o TelaDois: tira do desktop o monitor virtual que sobrou ligado. Devolve qual, ou <c>null</c>.</summary>
    public string? DetachLeftover()
    {
        var leftover = api.Devices().FirstOrDefault(d => IsVirtual(d) && d.AttachedToDesktop);
        if (leftover is null)
        {
            return null;
        }
        Detach(leftover.DeviceName);
        return leftover.DeviceName;
    }

    /// <summary>O tamanho pedido (preferindo 60 Hz); senão a proporção mais próxima, o maior e o mais perto de 60 Hz.</summary>
    public static DisplayModeInfo? ChooseMode(IEnumerable<DisplayModeInfo> modes, int width, int height)
    {
        var candidates = modes.Where(m => m.Width > 0 && m.Height > 0).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }
        var target = (double)width / height;
        return candidates
            .OrderBy(m => m.Width == width && m.Height == height ? 0 : 1)
            .ThenBy(m => Math.Round(Math.Abs((double)m.Width / m.Height - target), 3))
            .ThenByDescending(m => (long)m.Width * m.Height)
            .ThenBy(m => Math.Abs(m.Frequency - Fps))
            .First();
    }

    private static bool IsVirtual(DisplayDeviceInfo device) =>
        device.Description.Contains(AdapterName, StringComparison.OrdinalIgnoreCase);

    private static void Check(int code, string action)
    {
        if (code != 0)
        {
            throw new DisplayChangeException(code, action);
        }
    }
}
