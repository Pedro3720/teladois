using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Usbmux;

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

    [Theory]
    [InlineData(TunnelState.ServiceMissing, "TelaDois: falta o Apple Mobile Device Service")]
    [InlineData(TunnelState.WaitingForIPhone, "TelaDois: conecte o iPhone no cabo")]
    [InlineData(TunnelState.WaitingForApp, "TelaDois: abra o app no iPhone")]
    public void DescribesWaitingStates(TunnelState state, string text) =>
        Assert.Equal(text, TunnelStatusText.For(new TunnelStatus(state)));

    [Fact]
    public void DescribesSecondScreen() =>
        Assert.Equal("TelaDois: segunda tela · 2556×1179 · 60 fps · 12 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(12), new VideoConfig(2556, 1179, 60))));

    [Fact]
    public void DescribesConnectedState()
    {
        var hello = new HelloInfo(1, 2556, 1179);
        Assert.Equal("TelaDois: conectado · 2556×1179 · 12 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello, TimeSpan.FromMilliseconds(12.4))));
        Assert.Equal("TelaDois: conectado · 2556×1179",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello)));
    }
}
