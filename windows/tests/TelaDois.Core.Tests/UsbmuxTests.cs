using System.Buffers.Binary;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests;

public class UsbmuxTests
{
    [Fact]
    public void PortIsSentWithBytesSwapped() => Assert.Equal(48155, UsbmuxPacket.NetworkPort(7100));

    [Fact]
    public void HeaderIsLittleEndianPlistPacket()
    {
        var packet = UsbmuxPacket.Build(5, UsbmuxMessages.Listen());
        Assert.Equal((uint)packet.Length, BinaryPrimitives.ReadUInt32LittleEndian(packet));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4)));
        Assert.Equal(8u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(8)));
        Assert.Equal(5u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(12)));
    }

    [Fact]
    public void ParsesAttachedAndDetachedEvents()
    {
        Assert.Equal(new DeviceAttached(1, "00008120-TESTE", "USB"), UsbmuxMessages.ParseEvent(RoundTrip(FakeUsbmuxd.Attached(1))));
        Assert.Equal(new DeviceDetached(1), UsbmuxMessages.ParseEvent(RoundTrip(FakeUsbmuxd.Detached(1))));
        Assert.Null(UsbmuxMessages.ParseEvent(RoundTrip(new Dictionary<string, object> { ["MessageType"] = "Paired", ["DeviceID"] = 1 })));
    }

    [Fact]
    public async Task ListenReceivesAttachedDevice()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var events = new UsbmuxClient(usbmuxd.Endpoint).ListenAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.True(await events.MoveNextAsync());
        Assert.Equal(new ListenStarted(), events.Current);
        Assert.True(await events.MoveNextAsync());
        Assert.Equal(new DeviceAttached(1, "00008120-TESTE", "USB"), events.Current);
    }

    [Fact]
    public async Task ConnectIsRefusedWhenAppIsClosed()
    {
        await using var usbmuxd = new FakeUsbmuxd { ConnectResult = UsbmuxMessages.ResultConnectionRefused };
        var error = await Assert.ThrowsAsync<UsbmuxException>(() => new UsbmuxClient(usbmuxd.Endpoint).ConnectAsync(1, LinkMessages.Port));
        Assert.Equal(3L, error.Code);
        var request = Assert.Single(usbmuxd.Requests, r => (string)r["MessageType"] == "Connect");
        Assert.Equal(48155L, request["PortNumber"]);
        Assert.Equal(1L, request["DeviceID"]);
    }

    [Fact]
    public async Task ConnectReturnsTunnelToThePhone()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var tunnel = await new UsbmuxClient(usbmuxd.Endpoint).ConnectAsync(1, LinkMessages.Port, cts.Token);
        var first = await new LinkFrameReader(tunnel).NextAsync(cts.Token);
        Assert.True(LinkMessages.TryParseHello(first!, out var hello));
        Assert.Equal(new HelloInfo(1, 2556, 1179), hello);
    }

    private static Dictionary<string, object> RoundTrip(Dictionary<string, object> body) => Plist.Parse(Plist.Serialize(body));
}
