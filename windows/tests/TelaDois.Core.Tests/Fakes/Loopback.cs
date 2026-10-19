using System.Net;
using System.Net.Sockets;

namespace TelaDois.Core.Tests.Fakes;

internal static class Loopback
{
    /// <summary>Dois streams TCP ligados entre si (PC e iPhone); fechar um fecha o socket.</summary>
    public static async Task<(Stream Pc, Stream Phone)> CreatePairAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var pc = new TcpClient { NoDelay = true };
            var connecting = pc.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
            var phone = await listener.AcceptTcpClientAsync();
            await connecting;
            return (pc.GetStream(), phone.GetStream());
        }
        finally
        {
            listener.Stop();
        }
    }
}
