using System.Collections.Concurrent;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;

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
}
