using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VideoFeedFactoryTests
{
    private static readonly HelloInfo Hello = new(1, 2556, 1179);
    private static readonly DisplayOutput Main = new(0, @"\.\DISPLAY2", 1920, 1080, true);
    private static readonly DisplayOutput Side = new(1, @"\.\DISPLAY1", 1366, 768, false);

    [Fact]
    public async Task WithoutFfmpegReportsMissingAndReturnsNull()
    {
        var missing = 0;
        var factory = new VideoFeedFactory(null, new FakeProcessRunner(_ => new FakeProcess(0)), () => [Main], () => null, _ => { });
        factory.FfmpegMissing += () => missing++;
        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Equal(1, missing);
    }

    [Fact]
    public async Task CapturesChosenMonitorWithProbedEncoder()
    {
        var runner = new FakeProcessRunner(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, AccessUnitReaderTests.Fixture()));
        var factory = new VideoFeedFactory("ffmpeg", runner, () => [Main, Side], () => null, _ => { });
        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(feed);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed!.Config);
        await feed.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        var capture = runner.Started.Last(a => !a.Contains("-frames:v"));
        Assert.Contains("ddagrab=output_idx=1:framerate=60:draw_mouse=1", capture);
        Assert.Contains("h264_mf", capture);
    }

    [Fact]
    public async Task NoMonitorReturnsNull() =>
        Assert.Null(await new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(0)), () => [], () => null, _ => { }).OpenAsync(Hello, CancellationToken.None));
}
