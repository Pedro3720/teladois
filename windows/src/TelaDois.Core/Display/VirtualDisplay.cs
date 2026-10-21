namespace TelaDois.Core.Display;

/// <summary>
/// O monitor do Virtual Display Driver. O driver mantém 1 monitor sempre instalado (<c>SETDISPLAYCOUNT 0</c> vira 1);
/// ligar é pô-lo no desktop em Estender, à direita do monitor mais à direita; desligar é tirá-lo (tamanho 0), e o
/// Windows devolve as janelas aos outros monitores. Com <paramref name="device"/> (Etapa 5c), o dispositivo do driver
/// também é ativado antes de ligar (<see cref="PowerOnAsync"/>) e desativado depois de desligar
/// (<see cref="PowerOffAsync"/>): desativado, o Windows deixa de listar o monitor.
/// </summary>
public sealed class VirtualDisplay(IDisplayApi api, IDriverPipe pipe, IDeviceSwitch? device = null, Action<string>? log = null) : IVirtualMonitor
{
    /// <summary>Nome exato do adaptador no Windows (<c>DeviceName</c> do <c>MttVDD.inf</c>); conferido no driver real em 2026-10-20.</summary>
    public const string AdapterName = "Virtual Display Driver";

    public const int Fps = 60;

    /// <summary>
    /// Prazo para o monitor aparecer depois de ativar o dispositivo. 20 s porque a tarefa roda o <c>Enable-PnpDevice</c>
    /// do PowerShell (o <c>pnputil</c> dava erro 1167 depois de reinícios): o dispositivo fica OK em ~3–4 s e o monitor
    /// aparece no <c>EnumDisplayDevices</c> um pouco depois; a primeira ativação depois do boot pode levar muito mais.
    /// </summary>
    public TimeSpan PowerOnWait { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>Prazo para o monitor sumir depois de desativar o dispositivo.</summary>
    public TimeSpan PowerOffWait { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Intervalo entre as consultas ao Windows durante as esperas.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    // Ligar e desligar um de cada vez, de qualquer origem (feed novo, feed antigo saindo, bandeja): um ligar durante um
    // desligar acharia o monitor ainda listado e seguiria sem a tarefa, e a desativação atrasada o tiraria logo depois.
    private readonly SemaphoreSlim _power = new(1, 1);

    public Task<bool> IsInstalledAsync(CancellationToken ct) => pipe.PingAsync(ct);

    /// <summary>
    /// Garante o monitor do driver presente no Windows: já presente → <c>true</c> na hora; senão ativa o dispositivo pela
    /// tarefa e espera até <see cref="PowerOnWait"/>. <c>false</c> sem as tarefas, com a tarefa recusada ou se o monitor não
    /// apareceu. Não mexe em nenhum monitor (pôr no desktop é o <see cref="Attach"/>). Espera um desligar em andamento
    /// terminar antes. Só lança o cancelamento de <paramref name="ct"/>.
    /// </summary>
    public async Task<bool> PowerOnAsync(CancellationToken ct)
    {
        await _power.WaitAsync(ct);
        try
        {
            return await PowerOnInTurnAsync(ct);
        }
        finally
        {
            _power.Release();
        }
    }

    private async Task<bool> PowerOnInTurnAsync(CancellationToken ct)
    {
        if (TryFind() is not null)
        {
            return true;
        }
        if (!await IsDeviceSwitchAvailableAsync(ct))
        {
            Log(@"monitor virtual ausente e sem as tarefas do Windows que o ativam (instale com tools\vdd\instalar-vdd.ps1)");
            return false;
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        if (!await RunDeviceSwitchAsync(true, ct))
        {
            Log("monitor virtual não ativou: a tarefa do Windows não rodou");
            return false;
        }
        var taskMs = watch.ElapsedMilliseconds;
        if (await WaitUntilAsync(() => TryFind() is not null, PowerOnWait, ct))
        {
            Log($"monitor virtual ativado em {watch.ElapsedMilliseconds} ms (tarefa voltou em {taskMs} ms)");
            return true;
        }
        Log($"monitor virtual não apareceu em {Seconds(PowerOnWait)} depois de ativar o dispositivo");
        return false;
    }

    /// <summary>
    /// Desativa o dispositivo do driver (só com as tarefas) e espera o monitor sumir até <see cref="PowerOffWait"/>.
    /// Sem as tarefas, não faz nada. Espera um ligar em andamento terminar antes. Só lança o cancelamento de <paramref name="ct"/>.
    /// </summary>
    public async Task PowerOffAsync(CancellationToken ct)
    {
        await _power.WaitAsync(ct);
        try
        {
            await PowerOffInTurnAsync(ct);
        }
        finally
        {
            _power.Release();
        }
    }

    private async Task PowerOffInTurnAsync(CancellationToken ct)
    {
        if (!await IsDeviceSwitchAvailableAsync(ct))
        {
            return;
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        if (!await RunDeviceSwitchAsync(false, ct))
        {
            Log("monitor virtual não desativou: a tarefa do Windows não rodou");
            return;
        }
        if (await WaitUntilAsync(() => TryFind() is null, PowerOffWait, ct))
        {
            Log($"monitor virtual desativado em {watch.ElapsedMilliseconds} ms");
        }
        else
        {
            Log($"monitor virtual não desativou em {Seconds(PowerOffWait)}");
        }
    }

    /// <summary>As tarefas que ativam/desativam o dispositivo existem.</summary>
    public Task<bool> CanPowerOffAsync(CancellationToken ct) => IsDeviceSwitchAvailableAsync(ct);

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

    /// <summary>
    /// DPI efetivo do monitor do driver (96 = 100%). Só pergunta pelo monitor do driver; qualquer outro nome, ou uma falha
    /// do Windows (registrada no log), dá <c>null</c>. Nunca lança.
    /// </summary>
    public int? EffectiveDpi(string deviceName)
    {
        try
        {
            return FindDriverDevice(deviceName) is { } device ? api.EffectiveDpi(device.DeviceName) : null;
        }
        catch (Exception ex)
        {
            Log($"não deu para ler a escala do monitor virtual: {ex.Message}");
            return null;
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

    // Find() que nunca lança: uma falha da API do Windows conta como "monitor não listado".
    private string? TryFind()
    {
        try
        {
            return Find();
        }
        catch (Exception ex)
        {
            Log($"não deu para listar os monitores: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> IsDeviceSwitchAvailableAsync(CancellationToken ct)
    {
        if (device is null)
        {
            return false;
        }
        try
        {
            return await device.IsAvailableAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log($"não deu para consultar as tarefas do monitor virtual: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> RunDeviceSwitchAsync(bool enable, CancellationToken ct)
    {
        try
        {
            return device is not null && await device.RunAsync(enable, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log($"a tarefa do monitor virtual falhou: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan limit, CancellationToken ct)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            var remaining = limit - watch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }
            await Task.Delay(remaining < PollInterval ? remaining : PollInterval, ct);
        }
        return true;
    }

    private static string Seconds(TimeSpan time) => $"{time.TotalSeconds:0.#} s";

    private void Log(string line)
    {
        try
        {
            log?.Invoke(line);
        }
        catch (Exception)
        {
        }
    }

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
