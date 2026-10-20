using System.Diagnostics;
using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Ao ligar o vídeo de uma sessão: acha o FFmpeg; ativa o dispositivo do monitor virtual, liga-o com a resolução do
/// iPhone e captura ele (Estender); sem driver, ou se ativar/ligar/achar o monitor falhar, captura um monitor existente
/// (Duplicar). Toda falha depois de ativar desliga e desativa o monitor (o Windows só o lista enquanto está em uso).
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

    /// <summary>
    /// O driver está instalado, mas sem as tarefas do Windows que ativam/desativam o dispositivo (instalação antiga): a
    /// Segunda Tela estende, mas o Windows continua listando o monitor virtual quando ele não está em uso.
    /// </summary>
    public event Action? DeviceSwitchMissing;

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
            // Desativado, o driver nem responde ao pipe: primeiro tenta ativar; "sem driver" só quando nada responde.
            if (await virtualMonitor.PowerOnAsync(ct))
            {
                if (!await virtualMonitor.CanPowerOffAsync(ct))
                {
                    DeviceSwitchMissing?.Invoke();
                }
                if (await TryExtendAsync(virtualMonitor, _probe, ffmpegPath, hello, ct) is { } extended)
                {
                    return extended;
                }
            }
            else if (await virtualMonitor.CanPowerOffAsync(ct))
            {
                log("o monitor virtual não ativou: duplicando um monitor");
            }
            else if (await virtualMonitor.IsInstalledAsync(ct))
            {
                log("o driver do monitor virtual responde, mas o monitor não aparece no Windows: duplicando um monitor");
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
    /// falha nesta saída), o perfil guardado é esquecido: a próxima abertura sonda de novo. O CONFIG leva o tamanho
    /// codificado (par, se o perfil corta), não o do monitor.
    /// </summary>
    private FfmpegCapture Capture(EncoderProbe probe, string ffmpeg, EncoderProfile profile, DisplayOutput output)
    {
        var (width, height) = profile.EncodedSize(output.Width, output.Height);
        return new(runner, ffmpeg, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
            new VideoConfig((uint)width, (uint)height, Fps), log,
            endedWithoutFrames: () =>
            {
                if (probe.Forget(profile))
                {
                    log($"a captura com {profile.Id} terminou sem nenhuma imagem: a próxima conexão testa as compressões de novo");
                }
            });
    }

    /// <summary>
    /// Liga o monitor virtual (já ativado) e captura ele; <c>null</c> para cair em Duplicar (motivo no log). Em qualquer
    /// falha, inclusive o cancelamento, tira o monitor do desktop e desativa o dispositivo antes de sair.
    /// </summary>
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
            await PowerOffQuietlyAsync(monitor);
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
                    : $"o Windows aceitou ligar o monitor virtual {device}, mas ele não entrou no desktop em {OutputWait.TotalSeconds:0} s (mesmo gravando no registro): duplicando um monitor");
                VirtualMonitorFeed.DetachAndVerify(monitor, device, log);
                await PowerOffQuietlyAsync(monitor);
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
            await PowerOffQuietlyAsync(monitor);
            throw;
        }
    }

    // Sem o ct da sessão: também roda depois do cancelamento.
    private Task PowerOffQuietlyAsync(IVirtualMonitor monitor) => VirtualMonitorFeed.PowerOffQuietlyAsync(monitor, log);

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
