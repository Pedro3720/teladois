using System.Collections.Concurrent;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class LinkSessionTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    [Fact]
    public async Task ReportsHelloThenRoundTrip()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = FakePhone.RespondAsync(phone, cts.Token);
        var statuses = new ConcurrentQueue<LinkStatus>();
        var measured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var run = LinkSession.RunAsync(pc, status =>
        {
            statuses.Enqueue(status);
            if (status.RoundTrip is not null)
            {
                measured.TrySetResult();
            }
        }, Fast, cts.Token);

        await measured.Task.WaitAsync(cts.Token);
        Assert.True(statuses.TryPeek(out var first));
        Assert.Equal(new LinkStatus(new HelloInfo(1, 2556, 1179), null), first);
        cts.Cancel();
        Assert.Equal(LinkEndReason.Cancelled, await run);
    }

    [Fact]
    public async Task EndsWithByeFromPhone()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Bye().Encode()).ToArray());
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task EndsWhenPhoneClosesTheSocket()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode());
        await phone.DisposeAsync();
        Assert.Equal(LinkEndReason.Closed, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task TimesOutWhenPhoneGoesSilent()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = FakePhone.SilentAfterHelloAsync(phone, cts.Token);
        Assert.Equal(LinkEndReason.Timeout, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(cts.Token));
    }

    [Fact]
    public async Task TimesOutWithoutHello()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        Assert.Equal(LinkEndReason.Timeout, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task RejectsFirstMessageThatIsNotHello()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Ping(1).Encode());
        await Assert.ThrowsAsync<InvalidDataException>(() => LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None));
    }

    private sealed class FixedFeed(VideoConfig config, IEnumerable<AccessUnit> frames) : IVideoFeed
    {
        private readonly Queue<AccessUnit> _frames = new(frames);
        public bool Disposed { get; private set; }
        public VideoConfig Config => config;

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            lock (_frames)
            {
                if (_frames.TryDequeue(out var frame))
                {
                    return frame;
                }
            }
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
    public async Task SendsConfigThenVideoAndDisposesFeed()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), [new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true), new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x41], false)]);
        LinkStatus? last = null;
        var run = LinkSession.RunAsync(pc, s => last = s, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var frames = new List<LinkFrame>();
        while (frames.Count(f => f.MessageType == LinkMessageType.Video) < 2)
        {
            frames.Add((await reader.NextAsync(cts.Token))!);
        }
        var first = frames.First(f => f.MessageType != LinkMessageType.Ping);
        Assert.True(LinkMessages.TryParseConfig(first, out var config));
        Assert.Equal(new VideoConfig(1366, 768, 60), config);
        var videos = frames.Where(f => f.MessageType == LinkMessageType.Video).ToList();
        Assert.True(LinkMessages.TryParseVideo(videos[0], out var key0, out _, out _));
        Assert.True(LinkMessages.TryParseVideo(videos[1], out var key1, out _, out _));
        Assert.True(key0);
        Assert.False(key1);
        Assert.Equal(new VideoConfig(1366, 768, 60), last?.Video);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.True(feed.Disposed);
    }

    [Fact]
    public async Task WithoutFeedBehavesLikeBefore()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Bye().Encode()).ToArray());
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None, (_, _) => Task.FromResult<IVideoFeed?>(null)).WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
