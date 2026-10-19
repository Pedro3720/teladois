using TelaDois.Core.Link;

namespace TelaDois.Core.Tests;

public class LinkProtocolTests
{
    [Fact]
    public void HelloMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("010000000A0001000009FC0000049B"), LinkMessages.Hello(2556, 1179).Encode());

    [Fact]
    public void PingMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("05000000080000011F71FB04CB"), LinkMessages.Ping(1234567890123).Encode());

    [Fact]
    public void ParsesHello()
    {
        Assert.True(LinkMessages.TryParseHello(LinkMessages.Hello(2622, 1206), out var hello));
        Assert.Equal(new HelloInfo(1, 2622, 1206), hello);
    }

    [Fact]
    public void RejectsHelloWithWrongSize() =>
        Assert.False(LinkMessages.TryParseHello(new LinkFrame(LinkMessageType.Hello, [0, 1]), out _));

    [Fact]
    public void PongCarriesTimestamp()
    {
        Assert.True(LinkMessages.TryReadTimestamp(LinkMessages.Pong(42), out var timestamp));
        Assert.Equal(42UL, timestamp);
        Assert.False(LinkMessages.TryReadTimestamp(LinkMessages.Bye(), out _));
    }

    [Fact]
    public void DecoderHandlesSplitAndBatchedFrames()
    {
        var bytes = LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Ping(7).Encode()).ToArray();
        var decoder = new LinkFrameDecoder();
        Assert.Empty(decoder.Append(bytes.AsSpan(0, 3)));
        var frames = decoder.Append(bytes.AsSpan(3));
        Assert.Equal(new LinkMessageType?[] { LinkMessageType.Hello, LinkMessageType.Ping }, frames.Select(f => f.MessageType));
        Assert.Empty(decoder.Append([]));
    }

    [Fact]
    public void DecoderRejectsHugePayload() =>
        Assert.Throws<InvalidDataException>(() => new LinkFrameDecoder().Append(Convert.FromHexString("03FFFFFFFF")));

    [Fact]
    public void ConfigMatchesGoldenBytes()
    {
        var frame = LinkMessages.Config(new VideoConfig(2556, 1179, 60));
        Assert.Equal(Convert.FromHexString("020000000A000009FC0000049B003C"), frame.Encode());
        Assert.True(LinkMessages.TryParseConfig(frame, out var config));
        Assert.Equal(new VideoConfig(2556, 1179, 60), config);
    }

    [Fact]
    public void KeyframeRequestMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("0400000000"), LinkMessages.KeyframeRequest().Encode());

    [Fact]
    public void UnknownTypeHasNoMessageType() => Assert.Null(new LinkFrame(99, []).MessageType);

    [Fact]
    public async Task ReaderReturnsFramesThenNullWhenClosed()
    {
        var bytes = LinkMessages.Ping(1).Encode().Concat(LinkMessages.Bye().Encode()).ToArray();
        var reader = new LinkFrameReader(new MemoryStream(bytes));
        Assert.Equal(LinkMessageType.Ping, (await reader.NextAsync(CancellationToken.None))?.MessageType);
        Assert.Equal(LinkMessageType.Bye, (await reader.NextAsync(CancellationToken.None))?.MessageType);
        Assert.Null(await reader.NextAsync(CancellationToken.None));
    }
}
