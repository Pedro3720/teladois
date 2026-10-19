using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

/// <summary>Rodam o FFmpeg de verdade: só no PC (o CI filtra a categoria Local).</summary>
[Trait("Category", "Local")]
public class FfmpegCaptureTests
{
    private static string Ffmpeg => FfmpegCapture.Locate() ?? throw new InvalidOperationException("FFmpeg não encontrado (winget install Gyan.FFmpeg)");

    [Fact]
    public async Task EncodesTestPatternIntoAccessUnitsStartingWithAKeyframe()
    {
        var config = new VideoConfig(640, 296, 30);
        await using var capture = FfmpegCapture.Start(Ffmpeg, FfmpegArgs.TestPattern(config, 30), config, _ => { });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var units = new List<byte[]>();
        await foreach (var unit in capture.AccessUnitsAsync(cts.Token))
        {
            units.Add(unit);
        }
        Assert.Equal(30, units.Count);
        Assert.Contains(units[0].Take(64), b => (b & 0x1F) == 7 && b == 0x67); // SPS no primeiro quadro
    }

    [Fact]
    public async Task ChoosesAWorkingEncoder() =>
        Assert.Contains(await FfmpegCapture.ChooseEncoderAsync(Ffmpeg, CancellationToken.None), new[] { "h264_amf", "libx264" });

    [Fact]
    public async Task FindsThePrimaryMonitorByItsSize()
    {
        var index = await FfmpegCapture.FindOutputIndexAsync(Ffmpeg, 1600, 900, CancellationToken.None);
        Assert.NotNull(index);
    }
}
