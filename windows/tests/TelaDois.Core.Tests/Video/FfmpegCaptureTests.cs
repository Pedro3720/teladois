using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FfmpegCaptureTests
{
    private static readonly VideoConfig Config = new(64, 64, 30);
    private static readonly TimeSpan[] FastBackoff = [TimeSpan.FromMilliseconds(10)];

    [Fact]
    public async Task DeliversFramesAndRestartsWhenFfmpegExits()
    {
        var log = new List<string>();
        var runner = new FakeProcessRunner(_ => new FakeProcess(1, AccessUnitReaderTests.Fixture(), "caiu"));
        // O roteiro entrega as 4 imagens de uma vez: com a fila padrão (3) toda rajada estoura e é descartada
        // se o teste não estiver lendo naquele instante. Capacidade 4 = a rajada cabe e o teste é determinístico.
        await using (var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, line => { lock (log) { log.Add(line); } }, FastBackoff, queueCapacity: 4))
        {
            Assert.Equal(Config, capture.Config);
            var frames = new List<AccessUnit>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 8; i++)
            {
                frames.Add(await capture.NextAsync(cts.Token));
            }
            Assert.True(frames[0].IsKeyframe);
            Assert.True(runner.Started.Count >= 2, "deveria ter reiniciado o FFmpeg");
        }
        Assert.Contains(log, line => line.Contains("caiu"));
        Assert.Equal(runner.Started.Count, runner.Disposed);
    }

    [Fact]
    public async Task RealFfmpegProducesFrames()
    {
        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            return; // FFmpeg ausente nesta máquina: o teste com o FFmpeg real só roda onde ele está instalado.
        }
        var profile = new EncoderProfile("teste", "libx264", "format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]);
        var args = FfmpegArguments.Encode(profile, "testsrc2=size=320x180:rate=30", 30, frames: 20);
        await using var capture = new FfmpegCapture(new SystemProcessRunner(), ffmpeg, args, new VideoConfig(320, 180, 30), _ => { });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        // O FFmpeg codifica as 20 imagens mais rápido do que o teste lê: a fila pode descartar algumas.
        // Basta provar que a primeira é completa e que outras chegam.
        var first = await capture.NextAsync(cts.Token);
        Assert.True(first.IsKeyframe);
        Assert.NotEmpty((await capture.NextAsync(cts.Token)).Data);
    }
}
