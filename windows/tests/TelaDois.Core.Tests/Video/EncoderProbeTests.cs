using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class EncoderProbeTests
{
    [Fact]
    public async Task PicksFirstProfileThatEncodesAndCachesIt()
    {
        var log = new List<string>();
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("h264_qsv") ? 0 : 1, errors: "sem placa"));
        var probe = new EncoderProbe(runner, "ffmpeg", log.Add);

        var chosen = await probe.ChooseAsync(1, CancellationToken.None);
        Assert.Equal("h264_qsv", chosen.Codec);
        Assert.Equal(3, runner.Started.Count); // amf, amf, qsv
        Assert.Equal(chosen, await probe.ChooseAsync(1, CancellationToken.None));
        Assert.Equal(3, runner.Started.Count);
        Assert.Contains(log, line => line.Contains("qsv"));
        Assert.Equal(3, runner.Disposed);
    }

    [Fact]
    public async Task FallsBackToSoftware()
    {
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("libx264") ? 0 : 1));
        Assert.Equal("libx264", (await new EncoderProbe(runner, "ffmpeg", _ => { }).ChooseAsync(0, CancellationToken.None)).Codec);
    }

    [Fact]
    public async Task ThrowsWhenNothingWorks()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EncoderProbe(runner, "ffmpeg", _ => { }).ChooseAsync(0, CancellationToken.None));
    }
}
