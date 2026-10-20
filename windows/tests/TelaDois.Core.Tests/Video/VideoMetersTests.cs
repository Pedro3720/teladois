using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VideoMetersTests
{
    [Fact]
    public void StartsEmpty() =>
        Assert.Equal(new VideoMetersSnapshot(0, 0, 0, 0, default, default), new VideoMeters().Take());

    [Fact]
    public void SumsTheIntervalAndStartsANewOneOnTake()
    {
        var meters = new VideoMeters();
        meters.FrameRead();
        meters.FrameRead();
        meters.FramesDropped(3);
        meters.QueueAge(TimeSpan.FromMilliseconds(2));
        meters.QueueAge(TimeSpan.FromMilliseconds(10));
        meters.FrameSent(1_000, TimeSpan.FromMilliseconds(1));
        meters.FrameSent(500, TimeSpan.FromMilliseconds(25));

        var first = meters.Take();
        Assert.Equal(2, first.Read);
        Assert.Equal(2, first.Sent);
        Assert.Equal(1_500, first.Bytes);
        Assert.Equal(3, first.Dropped);
        Assert.Equal(new DurationSummary(2, 6, 10), first.Queue);
        Assert.Equal(new DurationSummary(2, 13, 25), first.Write);

        Assert.Equal(new VideoMetersSnapshot(0, 0, 0, 0, default, default), meters.Take());
    }

    [Fact]
    public async Task CountsFromManyThreads()
    {
        var meters = new VideoMeters();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 1_000; i++)
            {
                meters.FrameRead();
            }
        })));
        Assert.Equal(8_000, meters.Take().Read);
    }
}
