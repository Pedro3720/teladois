using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests;

public class LinkSessionVideoTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    [Fact]
    public async Task SendsConfigThenFramesAndForwardsKeyframeRequests()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var source = new FakeVideoSource(frames: 3);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, source);

        await phone.WriteAsync(LinkMessages.Hello(640, 296).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var received = new List<LinkFrame>();
        while (received.Count(f => f.MessageType == LinkMessageType.Video) < 3)
        {
            var frame = await reader.NextAsync(cts.Token) ?? throw new InvalidOperationException("sessão fechou cedo");
            if (frame.MessageType is LinkMessageType.Config or LinkMessageType.Video)
            {
                received.Add(frame);
            }
            if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
            {
                await phone.WriteAsync(LinkMessages.Pong(sent).Encode(), cts.Token);
            }
        }
        Assert.True(LinkMessages.TryParseConfig(received[0], out var config));
        Assert.Equal(new VideoConfig(640, 296, 60), config);
        Assert.Equal(new byte[] { 0, 1, 2 }, received.Skip(1).Select(f => f.Payload[^1]));

        await phone.WriteAsync(LinkMessages.KeyframeRequest().Encode(), cts.Token);
        while (source.Stream!.KeyframeRequests == 0)
        {
            await Task.Delay(10, cts.Token);
        }
        cts.Cancel();
        Assert.Equal(LinkEndReason.Cancelled, await run);
        Assert.True(source.Stream.Disposed);
    }

    [Fact]
    public async Task ReportsVideoConfigInStatus()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var withVideo = new TaskCompletionSource<LinkStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = FakePhone.RespondAsync(phone, cts.Token);
        var run = LinkSession.RunAsync(pc, s =>
        {
            if (s.Video is not null)
            {
                withVideo.TrySetResult(s);
            }
        }, Fast, cts.Token, new FakeVideoSource(1));

        var status = await withVideo.Task.WaitAsync(cts.Token);
        Assert.Equal(new VideoConfig(2556, 1179, 60), status.Video);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task DisposesTheVideoWhenThePhoneLeaves()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        var source = new FakeVideoSource(2);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None, source);
        await phone.WriteAsync(LinkMessages.Hello(640, 296).Encode());
        while (source.Stream is null)
        {
            await Task.Delay(10);
        }
        await phone.DisposeAsync();
        Assert.Equal(LinkEndReason.Closed, await run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(source.Stream.Disposed);
    }
}
