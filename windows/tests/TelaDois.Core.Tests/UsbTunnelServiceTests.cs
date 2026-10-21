using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class UsbTunnelServiceTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    private static UsbTunnelService Create(IPEndPoint endpoint, ConcurrentQueue<string> log) =>
        new(new UsbmuxClient(endpoint), Fast, log.Enqueue) { Retry = TimeSpan.FromMilliseconds(100) };

    private static Task<TunnelStatus> WaitFor(UsbTunnelService service, Func<TunnelStatus, bool> match)
    {
        var found = new TaskCompletionSource<TunnelStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.StatusChanged += status =>
        {
            if (match(status))
            {
                found.TrySetResult(status);
            }
        };
        return found.Task;
    }

    [Fact]
    public async Task ConnectsAndMeasuresLatency()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var service = Create(usbmuxd.Endpoint, new());
        var connected = WaitFor(service, s => s is { State: TunnelState.Connected, RoundTrip: not null });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        var status = await connected.WaitAsync(cts.Token);
        Assert.Equal(new HelloInfo(1, 2556, 1179), status.Hello);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task WaitsForAppWhileConnectIsRefused()
    {
        await using var usbmuxd = new FakeUsbmuxd { ConnectResult = UsbmuxMessages.ResultConnectionRefused };
        var service = Create(usbmuxd.Endpoint, new());
        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForApp);
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await waiting.WaitAsync(cts.Token);
        usbmuxd.ConnectResult = UsbmuxMessages.ResultOk;
        await connected.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task GoesBackToWaitingWhenCableIsRemoved()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var service = Create(usbmuxd.Endpoint, new());
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);
        await connected.WaitAsync(cts.Token);

        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Detached(1));
        await waiting.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task WaitsForIPhoneWhenNoneIsPlugged()
    {
        await using var usbmuxd = new FakeUsbmuxd { EventsAfterListen = [] };
        var service = Create(usbmuxd.Endpoint, new());
        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await waiting.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task ReportsMissingAppleService()
    {
        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var endpoint = (IPEndPoint)closed.LocalEndpoint;
        closed.Stop();
        var log = new ConcurrentQueue<string>();
        var service = Create(endpoint, log);
        var missing = WaitFor(service, s => s.State == TunnelState.ServiceMissing);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await missing.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
        Assert.Single(log, line => line.StartsWith("usbmuxd indisponível", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeepsTunnelAndReconnectsWhenVideoFails()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var sessions = 0;
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        usbmuxd.Phone = async (stream, ct) =>
        {
            if (Interlocked.Increment(ref sessions) == 2)
            {
                second.TrySetResult();
            }
            await FakePhone.ByeAfterPongsAsync(stream, 3, ct);
        };
        var log = new ConcurrentQueue<string>();
        var service = new UsbTunnelService(new UsbmuxClient(usbmuxd.Endpoint), Fast, log.Enqueue,
            (_, _) => throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC"))
        {
            Retry = TimeSpan.FromMilliseconds(100),
        };
        var measured = WaitFor(service, s => s is { State: TunnelState.Connected, RoundTrip: not null });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        Assert.Null((await measured.WaitAsync(cts.Token)).Video);
        await second.Task.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
        Assert.Contains(log, line => line.StartsWith("sessão encerrada", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RetriesAfterUnexpectedSessionFailure()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var log = new ConcurrentQueue<string>();
        var service = Create(usbmuxd.Endpoint, log);
        var connections = 0;
        service.StatusChanged += status =>
        {
            if (status.State == TunnelState.Connected && status.RoundTrip is null && Interlocked.Increment(ref connections) == 1)
            {
                throw new InvalidOperationException("falha inesperada");
            }
        };
        var measured = WaitFor(service, s => s is { State: TunnelState.Connected, RoundTrip: not null });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await measured.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
        Assert.Contains("falha no túnel: falha inesperada", log);
    }

    [Fact]
    public async Task SurvivesDeviceThatFailedWhenCableIsRemoved()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var log = new ConcurrentQueue<string>();
        var service = Create(usbmuxd.Endpoint, log);
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.StatusChanged += status =>
        {
            if (status.State == TunnelState.WaitingForApp && !failed.Task.IsCompleted)
            {
                failed.TrySetResult();
                throw new InvalidOperationException("falha inesperada");
            }
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);
        await failed.Task.WaitAsync(cts.Token);

        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Detached(1));
        await waiting.WaitAsync(cts.Token);
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Attached(1));
        await connected.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
        Assert.Contains(log, line => line.StartsWith("falha no túnel: falha inesperada", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(TunnelState.ServiceMissing,"TelaDois: falta o Apple Mobile Device Service")]
    [InlineData(TunnelState.WaitingForIPhone, "TelaDois: conecte o iPhone no cabo")]
    [InlineData(TunnelState.WaitingForApp, "TelaDois: abra o app no iPhone")]
    public void DescribesWaitingStates(TunnelState state, string text) =>
        Assert.Equal(text, TunnelStatusText.For(new TunnelStatus(state)));

    [Fact]
    public void DescribesConnectedState()
    {
        var hello = new HelloInfo(1, 2556, 1179);
        Assert.Equal("TelaDois: conectado · 2556×1179 · 12 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello, TimeSpan.FromMilliseconds(12.4))));
        Assert.Equal("TelaDois: conectado · 2556×1179",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello)));
    }

    [Fact]
    public async Task MonitorModeFollowsTheSession()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var monitor = new MonitorMode();
        var service = new UsbTunnelService(new UsbmuxClient(usbmuxd.Endpoint), Fast, _ => { }, monitor: monitor) { Retry = TimeSpan.FromMilliseconds(100) };
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await connected.WaitAsync(cts.Token);
        Assert.True(monitor.IsConnected);
        Assert.True(monitor.WantsVideo); // liga sozinho ao conectar

        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Detached(1));
        await waiting.WaitAsync(cts.Token);
        Assert.False(monitor.IsConnected);
        Assert.False(monitor.WantsVideo);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task ToggleDarkWithTheVideoOnSendsScreenToThePhone()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var sawVideo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var screens = new ConcurrentQueue<bool>();
        var screenArrived = new SemaphoreSlim(0);
        usbmuxd.Phone = async (stream, ct) =>
        {
            await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
            var reader = new LinkFrameReader(stream);
            while (await reader.NextAsync(ct) is { } frame)
            {
                if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
                {
                    await stream.WriteAsync(LinkMessages.Pong(sent).Encode(), ct);
                }
                else if (frame.MessageType == LinkMessageType.Video)
                {
                    sawVideo.TrySetResult();
                }
                else if (frame.MessageType == LinkMessageType.Screen && LinkMessages.TryParseScreen(frame, out var dark))
                {
                    screens.Enqueue(dark);
                    screenArrived.Release();
                }
            }
        };
        var monitor = new MonitorMode();
        var service = new UsbTunnelService(new UsbmuxClient(usbmuxd.Endpoint), Fast, _ => { }, (_, _) => Task.FromResult<IVideoFeed?>(new SteadyFeed()), monitor)
        {
            Retry = TimeSpan.FromMilliseconds(100),
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await sawVideo.Task.WaitAsync(cts.Token);
        Assert.Equal(DarkToggleResult.TurnedDark, monitor.ToggleDark());
        await screenArrived.WaitAsync(cts.Token);
        Assert.Equal([true], screens);
        cts.Cancel();
        await run;
    }

    /// <summary>Uma imagem completa e depois imagens a cada 20 ms, até ser descartado.</summary>
    private sealed class SteadyFeed : IVideoFeed
    {
        private int _sent;
        public VideoConfig Config => new(2556, 1179, 60);

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            if (Interlocked.Increment(ref _sent) == 1)
            {
                return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true);
            }
            await Task.Delay(20, ct);
            return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x41], false);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
