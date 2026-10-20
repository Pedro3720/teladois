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
    public async Task ForgetMakesTheNextChoiceProbeAgain()
    {
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("h264_qsv") ? 0 : 1));
        var probe = new EncoderProbe(runner, "ffmpeg", _ => { });

        var chosen = await probe.ChooseAsync(1, CancellationToken.None);
        Assert.False(probe.Forget(EncoderProfiles.Candidates[^1])); // outro perfil: o guardado fica
        Assert.Equal(chosen, await probe.ChooseAsync(1, CancellationToken.None));
        Assert.Equal(3, runner.Started.Count);

        Assert.True(probe.Forget(chosen));
        Assert.Equal(chosen, await probe.ChooseAsync(2, CancellationToken.None));
        Assert.Equal(6, runner.Started.Count);
    }

    [Fact]
    public async Task ConcurrentChoicesProbeOnlyOnce()
    {
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("h264_qsv") ? 0 : 1));
        var probe = new EncoderProbe(runner, "ffmpeg", _ => { });

        var chosen = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => probe.ChooseAsync(1, CancellationToken.None))));

        Assert.All(chosen, c => Assert.Equal("h264_qsv", c.Codec));
        Assert.Equal(3, runner.Started.Count);
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

    [Fact]
    public async Task HungProbeIsDroppedAndNextProfileIsTried()
    {
        var log = new List<string>();
        var calls = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref calls) == 1
            ? new FakeProcess(0, hangs: true)
            : new FakeProcess(0));
        var probe = new EncoderProbe(runner, "ffmpeg", log.Add) { ProbeTimeout = TimeSpan.FromMilliseconds(50) };

        var chosen = await probe.ChooseAsync(0, CancellationToken.None);

        Assert.Equal(EncoderProfiles.Candidates[1], chosen);
        Assert.Equal(2, runner.Started.Count);
        Assert.Equal(2, runner.Disposed);
        Assert.Contains(log, line => line.Contains(EncoderProfiles.Candidates[0].Id) && line.Contains("sem resposta"));
    }

    [Fact]
    public async Task AlreadyCancelledTokenStartsNothing()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(0));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new EncoderProbe(runner, "ffmpeg", _ => { }).ChooseAsync(0, cts.Token));
        Assert.Empty(runner.Started);
    }

    [Fact]
    public async Task CancellingDuringWaitDisposesRunningProcess()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(0, hangs: true));
        using var cts = new CancellationTokenSource();
        var probe = new EncoderProbe(runner, "ffmpeg", _ => { });

        var task = probe.ChooseAsync(0, cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Single(runner.Started);
        Assert.Equal(1, runner.Disposed);
    }
}
