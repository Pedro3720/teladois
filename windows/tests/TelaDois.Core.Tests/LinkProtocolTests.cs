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
    public void ConfigMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("020000000A0000055600000300003C"), LinkMessages.Config(new VideoConfig(1366, 768, 60)).Encode());

    [Fact]
    public void VideoMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("030000000F0100000000000003E80000000109F0"),
            LinkMessages.Video(true, 1000, Convert.FromHexString("0000000109F0")).Encode());

    [Fact]
    public void VideoEndMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("0800000000"), LinkMessages.VideoEnd().Encode());

    [Fact]
    public void VideoEndIsAKnownType() =>
        Assert.Equal(LinkMessageType.VideoEnd, new LinkFrame(8, []).MessageType);

    [Fact]
    public void ScreenMatchesGoldenBytes()
    {
        Assert.Equal(Convert.FromHexString("0A0000000101"), LinkMessages.Screen(true).Encode());
        Assert.Equal(Convert.FromHexString("0A0000000100"), LinkMessages.Screen(false).Encode());
    }

    [Fact]
    public void ScreenParsesAndRejectsWrongLength()
    {
        Assert.True(LinkMessages.TryParseScreen(new LinkFrame(10, [1]), out var dark));
        Assert.True(dark);
        Assert.True(LinkMessages.TryParseScreen(new LinkFrame(10, [0]), out dark));
        Assert.False(dark);
        Assert.False(LinkMessages.TryParseScreen(new LinkFrame(10, []), out _));
        Assert.False(LinkMessages.TryParseScreen(new LinkFrame(10, [1, 0]), out _));
        Assert.Equal(LinkMessageType.Screen, new LinkFrame(10, [0]).MessageType);
    }

    [Fact]
    public void StatsMatchesGoldenBytes()
    {
        Assert.Equal(
            Convert.FromHexString("0900000024000000010000000200000003000000040000000500000006000000070000000800000009"),
            LinkMessages.Stats(new LinkStats(1, 2, 3, 4, 5, 6, 7, 8, 9)).Encode());
        Assert.Equal(LinkMessageType.Stats, new LinkFrame(9, []).MessageType);
    }

    [Fact]
    public void ParsesStats()
    {
        var stats = new LinkStats(60, 59, 1, 400_000, 2_000, 40_000, 1_000, 9_000, 0);
        Assert.True(LinkMessages.TryParseStats(LinkMessages.Stats(stats), out var parsed));
        Assert.Equal(stats, parsed);
    }

    [Fact]
    public void RejectsStatsWithWrongSize()
    {
        Assert.False(LinkMessages.TryParseStats(new LinkFrame(LinkMessageType.Stats, new byte[35]), out _));
        Assert.False(LinkMessages.TryParseStats(new LinkFrame(LinkMessageType.Stats, new byte[37]), out _));
        Assert.False(LinkMessages.TryParseStats(new LinkFrame(LinkMessageType.Pong, new byte[36]), out _));
    }

    [Fact]
    public void ParsesConfigAndVideo()
    {
        Assert.True(LinkMessages.TryParseConfig(LinkMessages.Config(new VideoConfig(2556, 1179, 30)), out var config));
        Assert.Equal(new VideoConfig(2556, 1179, 30), config);
        Assert.True(LinkMessages.TryParseVideo(LinkMessages.Video(false, 42, [1, 2, 3]), out var keyframe, out var timestamp, out var data));
        Assert.False(keyframe);
        Assert.Equal(42UL, timestamp);
        Assert.Equal(new byte[] { 1, 2, 3 }, data.ToArray());
    }

    [Fact]
    public void RejectsMalformedConfigAndVideo()
    {
        Assert.False(LinkMessages.TryParseConfig(new LinkFrame(LinkMessageType.Config, [0, 1]), out _));
        Assert.False(LinkMessages.TryParseVideo(new LinkFrame(LinkMessageType.Video, [1, 0, 0]), out _, out _, out _));
        Assert.False(LinkMessages.TryParseVideo(LinkMessages.Ping(1), out _, out _, out _));
    }

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
