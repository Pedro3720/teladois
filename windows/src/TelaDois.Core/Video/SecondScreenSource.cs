using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Liga o monitor virtual na resolução do iPhone, acha a saída no ddagrab e inicia o FFmpeg.</summary>
public sealed class SecondScreenSource(string ffmpegPath, Action<string> log) : IVideoSource
{
    public const ushort Fps = 60;
    private string? _encoder;

    public async Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct)
    {
        var device = DisplayManager.FindVirtualDisplay()
            ?? throw new InvalidOperationException("monitor virtual não encontrado: instale o Virtual Display Driver");
        DisplayManager.Attach(device, hello.Width, hello.Height, Fps);
        log($"monitor virtual ligado ({device}, {hello.Width}×{hello.Height})");
        try
        {
            _encoder ??= await FfmpegCapture.ChooseEncoderAsync(ffmpegPath, ct);
            int? output = null;
            for (var attempt = 0; attempt < 10 && output is null; attempt++)
            {
                await Task.Delay(300, ct); // o Windows leva um instante para criar a saída
                output = await FfmpegCapture.FindOutputIndexAsync(ffmpegPath, hello.Width, hello.Height, ct);
            }
            if (output is null)
            {
                throw new InvalidOperationException($"o ddagrab não achou uma tela de {hello.Width}×{hello.Height}");
            }
            var config = new VideoConfig(hello.Width, hello.Height, Fps);
            log($"capturando a saída {output} com {_encoder}");
            var capture = FfmpegCapture.Start(ffmpegPath, FfmpegArgs.Desktop(output.Value, config, _encoder), config, log);
            return new DetachOnDispose(capture, device, log);
        }
        catch
        {
            DisplayManager.Detach(device);
            log("monitor virtual desligado (falha ao iniciar a captura)");
            throw;
        }
    }

    private sealed class DetachOnDispose(IVideoStream inner, string device, Action<string> log) : IVideoStream
    {
        public VideoConfig Config => inner.Config;

        public IAsyncEnumerable<byte[]> AccessUnitsAsync(CancellationToken ct) => inner.AccessUnitsAsync(ct);

        public void RequestKeyframe() => inner.RequestKeyframe();

        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            DisplayManager.Detach(device);
            log("monitor virtual desligado");
        }
    }
}
