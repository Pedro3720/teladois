using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Ao abrir uma sessão: acha o FFmpeg, escolhe o monitor e a compressão e começa a captura.</summary>
public sealed class VideoFeedFactory(string? ffmpegPath, IProcessRunner runner, Func<IReadOnlyList<DisplayOutput>> displays, Func<string?> preferredMonitor, Action<string> log)
{
    public const ushort Fps = 60;

    private readonly EncoderProbe? _probe = ffmpegPath is null ? null : new EncoderProbe(runner, ffmpegPath, log);

    public event Action? FfmpegMissing;

    public async Task<IVideoFeed?> OpenAsync(HelloInfo hello, CancellationToken ct)
    {
        if (ffmpegPath is null || _probe is null)
        {
            log("FFmpeg não encontrado: instale com 'winget install Gyan.FFmpeg'");
            FfmpegMissing?.Invoke();
            return null;
        }
        var output = DisplayCatalog.Choose(displays(), preferredMonitor());
        if (output is null)
        {
            log("nenhum monitor para transmitir");
            return null;
        }
        var profile = await _probe.ChooseAsync(output.OutputIndex, ct);
        log($"transmitindo {output.DeviceName} ({output.Width}×{output.Height}) com {profile.Id} para o iPhone {hello.Width}×{hello.Height}");
        return new FfmpegCapture(runner, ffmpegPath, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
            new VideoConfig((uint)output.Width, (uint)output.Height, Fps), log);
    }
}
