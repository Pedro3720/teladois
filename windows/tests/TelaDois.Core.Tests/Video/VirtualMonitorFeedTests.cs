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
        Assert.Equal(new[] { "Detach", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task DisposePowersOffEvenWhenTheMonitorAlreadyLeftTheDesktop()
    {
        // O usuário tirou o monitor nas Configurações: o dispositivo continua ativo e listado até desativar.
        var monitor = new FakeVirtualMonitor { Attached = false };
        var feed = new VirtualMonitorFeed(new WaitingFeed(), monitor, monitor.Device, _ => { });
        await feed.DisposeAsync();
        Assert.Empty(monitor.Detached);
        Assert.Equal(1, monitor.PowerOffs);
    }

    [Fact]
    public async Task DisposeFinishesAndLogsWhenPowerOffFails()
    {
        var monitor = new FakeVirtualMonitor { Attached = true, PowerOffError = new IOException("o Agendador de Tarefas não respondeu") };
        var capture = new WaitingFeed();
        var log = new ConcurrentQueue<string>();
        var feed = new VirtualMonitorFeed(capture, monitor, monitor.Device, log.Enqueue);

        await feed.DisposeAsync();
        await feed.DisposeAsync();

        Assert.True(capture.Disposed);
        Assert.Equal(new[] { "Detach", "PowerOff" }, monitor.Calls);
        Assert.Contains("não deu para desativar o monitor virtual: o Agendador de Tarefas não respondeu", log);
    }

    /// <summary>Captura cujo descarte falha (ex.: o processo não deixou ser encerrado).</summary>
    private sealed class FailingDisposeFeed : IVideoFeed
    {
        public VideoConfig Config => new(2556, 1179, 60);
        public Task<AccessUnit> NextAsync(CancellationToken ct) => Task.Delay(Timeout.Infinite, ct).ContinueWith<AccessUnit>(_ => throw new OperationCanceledException(ct));
        public ValueTask DisposeAsync() => ValueTask.FromException(new InvalidOperationException("o FFmpeg não saiu"));
    }

    [Fact]
    public async Task WatchFailureIsLoggedAndDisposeStillStopsTheCaptureAndDetaches()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var capture = new WaitingFeed();
        var log = new ConcurrentQueue<string>();
        var feed = new VirtualMonitorFeed(capture, monitor, monitor.Device, log.Enqueue, TimeSpan.FromMilliseconds(50));
        monitor.IsAttachedError = new IOException("a API do Windows falhou");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!log.Any(line => line.StartsWith("não deu para conferir o monitor virtual", StringComparison.Ordinal)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        await feed.DisposeAsync();

        Assert.Contains("não deu para conferir o monitor virtual: a API do Windows falhou", log);
        Assert.True(capture.Disposed);
        Assert.Equal(new[] { monitor.Device }, monitor.Detached);
        Assert.False(monitor.Attached);
    }

    [Fact]
    public async Task DisposeDetachesEvenWhenTheCaptureFailsToDispose()
    {
        var monitor = new FakeVirtualMonitor { Attached = true };
        var feed = new VirtualMonitorFeed(new FailingDisposeFeed(), monitor, monitor.Device, _ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() => feed.DisposeAsync().AsTask());

        Assert.Equal(new[] { "Detach", "PowerOff" }, monitor.Calls);
        Assert.False(monitor.Attached);
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
