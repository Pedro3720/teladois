using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace TelaDois.Core.Usbmux;

public sealed class UsbmuxException(string operation, long code)
    : IOException($"o usbmuxd recusou {operation} (código {code})")
{
    public long Code { get; } = code;
}

/// <summary>Cliente do usbmuxd do Apple Mobile Device Service (cada operação usa uma conexão nova).</summary>
public sealed class UsbmuxClient(IPEndPoint endpoint)
{
    public static IPEndPoint DefaultEndpoint { get; } = new(IPAddress.Loopback, 27015);

    /// <summary>
    /// <see cref="ListenStarted"/> e depois os eventos de iPhone conectado/desconectado,
    /// até o token ser cancelado ou a conexão cair.
    /// </summary>
    public async IAsyncEnumerable<UsbmuxEvent> ListenAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(endpoint, ct);
        var stream = tcp.GetStream();
        await RequestAsync(stream, 1, UsbmuxMessages.Listen(), "Listen", ct);
        yield return new ListenStarted();
        while (true)
        {
            var (_, body) = await UsbmuxPacket.ReadAsync(stream, ct);
            if (UsbmuxMessages.ParseEvent(body) is { } usbmuxEvent)
            {
                yield return usbmuxEvent;
            }
        }
    }

    /// <summary>Abre um túnel TCP até <paramref name="port"/> no iPhone; o stream devolvido é o túnel.</summary>
    public async Task<Stream> ConnectAsync(int deviceId, ushort port, CancellationToken ct = default)
    {
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            await tcp.ConnectAsync(endpoint, ct);
            var stream = tcp.GetStream();
            await RequestAsync(stream, 2, UsbmuxMessages.Connect(deviceId, port), "Connect", ct);
            return stream;
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    private static async Task RequestAsync(Stream stream, uint tag, Dictionary<string, object> body, string operation, CancellationToken ct)
    {
        await stream.WriteAsync(UsbmuxPacket.Build(tag, body), ct);
        var (_, reply) = await UsbmuxPacket.ReadAsync(stream, ct);
        var code = UsbmuxMessages.ResultCode(reply) ?? -1;
        if (code != UsbmuxMessages.ResultOk)
        {
            throw new UsbmuxException(operation, code);
        }
    }
}
