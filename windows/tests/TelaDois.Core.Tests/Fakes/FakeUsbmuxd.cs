using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>usbmuxd falso no loopback: responde Listen (e manda eventos) e Connect (e vira o iPhone).</summary>
internal sealed class FakeUsbmuxd : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private volatile Stream? _listenStream;

    public FakeUsbmuxd()
    {
        _listener.Start();
        _acceptLoop = AcceptLoopAsync();
    }

    public IPEndPoint Endpoint => (IPEndPoint)_listener.LocalEndpoint;

    /// <summary>Resposta ao Connect: 0 = ok, 3 = app fechado.</summary>
    public volatile int ConnectResult;

    public Func<Stream, CancellationToken, Task> Phone { get; set; } = FakePhone.RespondAsync;

    public IReadOnlyList<Dictionary<string, object>> EventsAfterListen { get; set; } = [Attached(1)];

    public ConcurrentQueue<Dictionary<string, object>> Requests { get; } = new();

    public static Dictionary<string, object> Attached(int id) => new()
    {
        ["MessageType"] = "Attached",
        ["DeviceID"] = id,
        ["Properties"] = new Dictionary<string, object>
        {
            ["ConnectionType"] = "USB",
            ["DeviceID"] = id,
            ["SerialNumber"] = "00008120-TESTE",
        },
    };

    public static Dictionary<string, object> Detached(int id) => new()
    {
        ["MessageType"] = "Detached",
        ["DeviceID"] = id,
    };

    public async Task SendEventAsync(Dictionary<string, object> body)
    {
        var stream = _listenStream ?? throw new InvalidOperationException("ninguém está em Listen");
        await stream.WriteAsync(UsbmuxPacket.Build(0, body), _cts.Token);
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleAsync(client);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
        {
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            try
            {
                var (tag, request) = await UsbmuxPacket.ReadAsync(stream, _cts.Token);
                Requests.Enqueue(request);
                var type = (string)request["MessageType"];
                var code = type == "Connect" ? ConnectResult : 0;
                await stream.WriteAsync(UsbmuxPacket.Build(tag, Result(code)), _cts.Token);
                if (code != 0)
                {
                    return;
                }
                if (type == "Listen")
                {
                    _listenStream = stream;
                    foreach (var body in EventsAfterListen)
                    {
                        await stream.WriteAsync(UsbmuxPacket.Build(0, body), _cts.Token);
                    }
                    await Task.Delay(Timeout.Infinite, _cts.Token);
                }
                else if (type == "Connect")
                {
                    await Phone(stream, _cts.Token);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
            }
        }
    }

    private static Dictionary<string, object> Result(int code) => new()
    {
        ["MessageType"] = "Result",
        ["Number"] = code,
    };

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        await _acceptLoop;
        _cts.Dispose();
    }
}
