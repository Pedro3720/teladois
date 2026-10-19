using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FrameQueueTests
{
    private static AccessUnit Frame(byte id, bool key = false) => new([id], key);

    [Fact]
    public async Task DeliversInOrder()
    {
        var queue = new FrameQueue();
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        Assert.Equal(1, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(2, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
    }

    [Fact]
    public async Task WhenFullDropsUntilNextKeyframe()
    {
        var queue = new FrameQueue(capacity: 3);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        queue.Add(Frame(3));
        queue.Add(Frame(4));        // cheia: descarta 1, 2, 3 e 4
        queue.Add(Frame(5));        // ainda esperando imagem completa: descarta
        queue.Add(Frame(6, true));  // completa: volta a entregar
        queue.Add(Frame(7));
        Assert.Equal(6, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(7, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(5, queue.Dropped);
    }

    [Fact]
    public async Task KeyframeArrivingWhenFullReplacesTheBacklog()
    {
        var queue = new FrameQueue(capacity: 2);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        queue.Add(Frame(3, true));
        Assert.Equal(3, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(2, queue.Dropped);
    }

    [Fact]
    public async Task TakeWaitsAndHonorsCancellation()
    {
        var queue = new FrameQueue();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.TakeAsync(cts.Token));
    }
}
