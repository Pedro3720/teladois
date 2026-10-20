using System.Collections.Concurrent;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VirtualMonitorFeedTests
{
    /// <summary>Captura que nunca entrega imagem (como o FFmpeg reiniciando sem o monitor).</summary>
    private sealed class WaitingFeed : IVideoFeed
    {
        public bool Disposed { get; private set; }
        public VideoConfig Config => new(2556, 1179, 60);

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new OperationCanceledException(ct);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task StopsWhenTheMonitorLeavesTheDesktop()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var log = new ConcurrentQueue<string>();
        await using var feed = new VirtualMonitorFeed(new WaitingFeed(), monitor, monitor.Device, log.Enqueue, TimeSpan.FromMilliseconds(50));
        Assert.Equal(VideoKind.Extend, ((IVideoFeed)feed).Kind);
        Assert.Equal(new VideoConfig(2556, 1179, 60), feed.Config);

        var next = feed.NextAsync(CancellationToken.None);
        monitor.Attached = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains("o monitor virtual saiu do desktop: Segunda Tela desligada", log);
    }

    [Fact]
    public async Task DisposeStopsTheCaptureAndDetachesOnce()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var capture = new WaitingFeed();
        var feed = new VirtualMonitorFeed(capture, monitor, monitor.Device, _ => { });
        await feed.DisposeAsync();
        await feed.DisposeAsync();
        Assert.True(capture.Disposed);
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
    }

    [Fact]
    public async Task DisposeLogsWhenTheMonitorStaysOnTheDesktop()
    {
        var monitor = new FakeVirtualMonitor { Attached = true, StaysAttached = true };
        var log = new ConcurrentQueue<string>();
        var feed = new VirtualMonitorFeed(new WaitingFeed(), monitor, monitor.Device, log.Enqueue);
        await feed.DisposeAsync();
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
        Assert.Contains($"o monitor virtual {monitor.Device} continua no desktop depois de desligar", log);
    }
}
