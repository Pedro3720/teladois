namespace TelaDois.Core.Display;

/// <summary>
/// O monitor do Virtual Display Driver. O driver mantém 1 monitor sempre instalado (<c>SETDISPLAYCOUNT 0</c> vira 1);
/// ligar é pô-lo no desktop em Estender, à direita do monitor mais à direita; desligar é tirá-lo (tamanho 0), e o
/// Windows devolve as janelas aos outros monitores.
/// </summary>
public sealed class VirtualDisplay(IDisplayApi api, IDriverPipe pipe) : IVirtualMonitor
{
    /// <summary>Nome exato do adaptador no Windows (<c>DeviceName</c> do <c>MttVDD.inf</c>); conferido no driver real em 2026-10-20.</summary>
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
        // Encosta no monitor mais à direita, também na altura (o topo dele, não sempre 0).
        var rightmost = api.Devices()
            .Where(d => d.AttachedToDesktop && !SameName(d.DeviceName, device))
            .Select(d => api.Bounds(d.DeviceName))
            .Where(b => b.HasValue)
            .Select(b => b!.Value)
            .OrderByDescending(b => b.X + b.Width)
            .Cast<DisplayBounds?>()
            .FirstOrDefault();
        var x = rightmost is { } r ? r.X + r.Width : 0;
        var y = rightmost?.Y ?? 0;
        Check(ApplyToDriverMonitor(device, x, y, mode.Width, mode.Height, mode.Frequency), "ligar");
        return (device, mode);
    }

    /// <summary>
    /// Tira do desktop o monitor do driver. Qualquer outro nome (monitor físico, nome desconhecido, vazio) é ignorado:
    /// o TelaDois nunca desliga um monitor que não é o virtual.
    /// </summary>
    public void Detach(string deviceName)
    {
        if (FindDriverDevice(deviceName) is { } device)
        {
            Check(ApplyToDriverMonitor(device.DeviceName, 0, 0, 0, 0, 0), "desligar");
        }
    }

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

    // Único caminho até o Apply: só o monitor do driver pode ser mudado (confere de novo, mesmo para quem já o achou).
    private int ApplyToDriverMonitor(string deviceName, int x, int y, int width, int height, int frequency) =>
        FindDriverDevice(deviceName) is { } device
            ? api.Apply(device.DeviceName, x, y, width, height, frequency)
            : throw new InvalidOperationException($"{deviceName} não é o monitor do Virtual Display Driver");

    private DisplayDeviceInfo? FindDriverDevice(string deviceName) =>
        api.Devices().FirstOrDefault(d => SameName(d.DeviceName, deviceName) && IsVirtual(d));

    private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // Nome exato (medido no driver 25.7.23: "Virtual Display Driver"; os físicos aqui são "Intel(R) UHD Graphics"):
    // um adaptador que só contém o nome, como o de outro driver virtual, nunca é tocado.
    private static bool IsVirtual(DisplayDeviceInfo device) =>
        string.Equals(device.Description, AdapterName, StringComparison.OrdinalIgnoreCase);

    private static void Check(int code, string action)
    {
        if (code != 0)
        {
            throw new DisplayChangeException(code, action);
        }
    }
}
