using System.Diagnostics;
using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Ao ligar o vídeo de uma sessão: acha o FFmpeg; com o monitor virtual instalado, liga-o com a resolução do iPhone
/// e captura ele (Estender); sem driver, ou se ligar/achar o monitor falhar, captura um monitor existente (Duplicar).
/// Só mexe em monitores pelo <see cref="IVirtualMonitor"/>.
/// </summary>
public sealed class VideoFeedFactory(
    string? ffmpegPath,
    IProcessRunner runner,
    Func<IReadOnlyList<DisplayOutput>> displays,
    Func<string?> preferredMonitor,
    Action<string> log,
    IVirtualMonitor? virtualMonitor = null)
{
    public const ushort Fps = 60;

    private readonly EncoderProbe? _probe = ffmpegPath is null ? null : new EncoderProbe(runner, ffmpegPath, log);

    public event Action? FfmpegMissing;

    /// <summary>O monitor virtual não respondeu: a Segunda Tela duplica um monitor existente.</summary>
    public event Action? DriverMissing;

    /// <summary>Quanto esperar o monitor recém-ligado aparecer na DXGI (o IddCx pode levar segundos).</summary>
    public TimeSpan OutputWait { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// A captura, ou <c>null</c> sem FFmpeg, sem monitor ou com qualquer falha (registrada no log): o vídeo nunca
    /// derruba o túnel. Só o cancelamento de <c>ct</c> sai como exceção.
    /// </summary>
    public async Task<IVideoFeed?> OpenAsync(HelloInfo hello, CancellationToken ct)
    {
        try
        {
            return await TryOpenAsync(hello, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            log($"Segunda Tela indisponível: {e.Message}");
            return null;
        }
    }

    private async Task<IVideoFeed?> TryOpenAsync(HelloInfo hello, CancellationToken ct)
    {
        if (ffmpegPath is null || _probe is null)
        {
            log("FFmpeg não encontrado: instale com 'winget install Gyan.FFmpeg'");
            FfmpegMissing?.Invoke();
            return null;
        }
        if (virtualMonitor is not null)
        {
            if (await virtualMonitor.IsInstalledAsync(ct))
            {
                if (await TryExtendAsync(virtualMonitor, _probe, ffmpegPath, hello, ct) is { } extended)
                {
                    return extended;
                }
            }
            else
            {
                log(@"monitor virtual não instalado: duplicando um monitor (para estender, rode tools\vdd\instalar-vdd.ps1 como administrador)");
                DriverMissing?.Invoke();
            }
        }
        // Duplicar nunca escolhe o monitor virtual (pode ter ficado ligado se o Detach falhou).
        var output = DisplayCatalog.Choose(displays(), preferredMonitor(), virtualMonitor?.Find());
        if (output is null)
        {
            log("nenhum monitor para transmitir");
            return null;
        }
        var profile = await _probe.ChooseAsync(output.OutputIndex, ct);
        log($"duplicando {output.DeviceName} ({output.Width}×{output.Height}) com {profile.Id} para o iPhone {hello.Width}×{hello.Height}");
        return Capture(_probe, ffmpegPath, profile, output);
    }

    /// <summary>
    /// A captura de <paramref name="output"/>. Se ela terminar sem nenhuma imagem (a sondagem passou, mas a linha real
    /// falha nesta saída), o perfil guardado é esquecido: a próxima abertura sonda de novo.
    /// </summary>
    private FfmpegCapture Capture(EncoderProbe probe, string ffmpeg, EncoderProfile profile, DisplayOutput output) =>
        new(runner, ffmpeg, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
            new VideoConfig((uint)output.Width, (uint)output.Height, Fps), log,
            endedWithoutFrames: () =>
            {
                if (probe.Forget(profile))
                {
                    log($"a captura com {profile.Id} terminou sem nenhuma imagem: a próxima conexão testa as compressões de novo");
                }
            });

    /// <summary>Liga o monitor virtual e captura ele; <c>null</c> para cair em Duplicar (motivo no log).</summary>
    private async Task<IVideoFeed?> TryExtendAsync(IVirtualMonitor monitor, EncoderProbe probe, string ffmpeg, HelloInfo hello, CancellationToken ct)
    {
        string device;
        DisplayModeInfo mode;
        try
        {
            (device, mode) = monitor.Attach((int)hello.Width, (int)hello.Height);
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            log($"não deu para ligar o monitor virtual: {e.Message}; duplicando um monitor");
            return null;
        }
        try
        {
            if (mode.Width != hello.Width || mode.Height != hello.Height)
            {
                log($"o monitor virtual não tem {hello.Width}×{hello.Height}: usando {mode.Width}×{mode.Height}");
            }
            var waited = Stopwatch.StartNew();
            var output = await WaitForOutputAsync(device, ct);
            if (output is null)
            {
                // Distingue os dois casos para o diagnóstico no aparelho: mexer no adaptador (<gpu> do XML) só ajuda no 2º.
                log(monitor.IsAttached(device)
                    ? $"monitor virtual {device} no desktop, mas fora do adaptador 0 da DXGI em {OutputWait.TotalSeconds:0} s (o ddagrab não o enxerga): duplicando um monitor"
                    : $"o Windows aceitou ligar o monitor virtual {device}, mas ele não entrou no desktop em {OutputWait.TotalSeconds:0} s (testar gravar no registro, persist): duplicando um monitor");
                VirtualMonitorFeed.DetachAndVerify(monitor, device, log);
                return null;
            }
            log($"monitor virtual {device} na DXGI em {waited.ElapsedMilliseconds} ms (saída {output.OutputIndex})");
            var profile = await probe.ChooseAsync(output.OutputIndex, ct);
            log($"estendendo para o iPhone: {device} ({output.Width}×{output.Height}) com {profile.Id}");
            return new VirtualMonitorFeed(Capture(probe, ffmpeg, profile, output), monitor, device, log);
        }
        catch
        {
            VirtualMonitorFeed.DetachAndVerify(monitor, device, log);
            throw;
        }
    }

    private async Task<DisplayOutput?> WaitForOutputAsync(string device, CancellationToken ct)
    {
        var waited = Stopwatch.StartNew();
        while (true)
        {
            if (displays().FirstOrDefault(o => string.Equals(o.DeviceName, device, StringComparison.OrdinalIgnoreCase)) is { } output)
            {
                return output;
            }
            if (waited.Elapsed >= OutputWait)
            {
                return null;
            }
            await Task.Delay(100, ct);
        }
    }
}
