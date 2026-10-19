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
        var withVideo = new TaskCompletionSource<LinkStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = LinkSession.RunAsync(pc, s =>
        {
            if (s.Video is not null)
            {
                withVideo.TrySetResult(s);
            }
        }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

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
        Assert.Equal(new VideoConfig(1366, 768, 60), (await withVideo.Task.WaitAsync(cts.Token)).Video);

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

    /// <summary>Imagens do tamanho dado, sem fim e sem espera.</summary>
    private sealed class EndlessFeed(VideoConfig config, int frameSize) : IVideoFeed
    {
        private readonly byte[] _frame = new byte[frameSize];
        public bool Disposed { get; private set; }
        public VideoConfig Config => config;

        public Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new AccessUnit(_frame, true));
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Lado do iPhone: lê mensagens (anotando o tipo em <c>seen</c>) e responde PING com PONG até <c>done</c>.</summary>
    private static async Task AnswerUntilAsync(Stream phone, LinkFrameReader reader, Func<bool> done, List<LinkMessageType> seen, CancellationToken ct)
    {
        while (!done())
        {
            var frame = await reader.NextAsync(ct) ?? throw new IOException("o PC fechou o socket");
            seen.Add(frame.MessageType ?? throw new InvalidDataException($"tipo desconhecido: {frame.Type}"));
            if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
            {
                await phone.WriteAsync(LinkMessages.Pong(sent).Encode(), ct);
            }
        }
    }

    [Fact]
    public async Task PingsRightAfterHelloWhileVideoIsStillOpening()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), [new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true)]);
        var statuses = new ConcurrentQueue<LinkStatus>();
        var videoStatuses = 0;
        var run = LinkSession.RunAsync(pc, s =>
        {
            statuses.Enqueue(s);
            if (s.Video is not null)
            {
                Interlocked.Increment(ref videoStatuses);
            }
        }, Fast, cts.Token, async (_, token) =>
        {
            await gate.Task.WaitAsync(token);
            return feed;
        });

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        // 12 PINGs a cada 50 ms passam do tempo-limite de 400 ms: a sessão segue viva enquanto o vídeo não abre.
        await AnswerUntilAsync(phone, reader, () => seen.Count(t => t == LinkMessageType.Ping) >= 12, seen, cts.Token);
        Assert.False(run.IsCompleted);
        Assert.DoesNotContain(LinkMessageType.Config, seen);
        Assert.All(statuses, s => Assert.Null(s.Video));

        gate.SetResult();
        // Depois do CONFIG, as medições de latência seguintes continuam com o vídeo no status.
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video) && Volatile.Read(ref videoStatuses) >= 3, seen, cts.Token);
        Assert.Equal(LinkMessageType.Ping, seen[0]);
        var config = seen.IndexOf(LinkMessageType.Config);
        Assert.True(config > 0);
        Assert.True(config < seen.IndexOf(LinkMessageType.Video));
        Assert.True(statuses.TryPeek(out var first));
        Assert.Equal(new LinkStatus(new HelloInfo(1, 2556, 1179), null), first);
        var ordered = statuses.ToList();
        var firstVideo = ordered.FindIndex(s => s.Video is not null);
        Assert.All(ordered.Skip(firstVideo), s => Assert.Equal(new VideoConfig(1366, 768, 60), s.Video));
        Assert.Contains(ordered.Skip(firstVideo + 1), s => s.RoundTrip is not null);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.True(feed.Disposed);
    }

    [Fact]
    public async Task DisposesFeedThatArrivesAfterSessionEnded()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), []);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None, async (_, token) =>
        {
            opening.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
            }
            return feed; // termina mesmo cancelado, como uma abertura que não conseguiu parar a tempo
        });

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode());
        await opening.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await phone.WriteAsync(LinkMessages.Bye().Encode());
        Assert.Equal(LinkEndReason.Bye, await run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(feed.Disposed);
    }

    [Fact]
    public async Task KeepsSessionWithoutVideoWhenOpeningFails()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var measured = new TaskCompletionSource<LinkStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = LinkSession.RunAsync(pc, s =>
        {
            if (s.RoundTrip is not null)
            {
                measured.TrySetResult(s);
            }
        }, Fast, cts.Token, (_, _) => throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC"));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, new LinkFrameReader(phone), () => measured.Task.IsCompleted, seen, cts.Token);
        Assert.Null((await measured.Task).Video);
        Assert.DoesNotContain(LinkMessageType.Config, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task DisposesFeedAndSaysByeWhenPhoneSendsInvalidData()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), []);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        while ((await reader.NextAsync(cts.Token) ?? throw new IOException("o PC fechou o socket")).MessageType != LinkMessageType.Config)
        {
        }
        // Cabeçalho de uma mensagem acima do limite de 16 MB.
        await phone.WriteAsync(new byte[] { (byte)LinkMessageType.Ping, 0x7F, 0xFF, 0xFF, 0xFF }, cts.Token);
        await Assert.ThrowsAsync<InvalidDataException>(() => run.WaitAsync(cts.Token));
        Assert.True(feed.Disposed);
        LinkFrame? frame;
        do
        {
            frame = await reader.NextAsync(cts.Token);
        }
        while (frame is not null && frame.MessageType != LinkMessageType.Bye);
        Assert.Equal(LinkMessageType.Bye, frame?.MessageType);
    }

    [Fact]
    public async Task TimesOutWhenPhoneStopsReading()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        var feed = new EndlessFeed(new VideoConfig(1920, 1080, 60), 1024 * 1024);
        // O iPhone manda HELLO e nunca mais lê o socket (sem fechar): as escritas do PC acabam travando.
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode());
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None, (_, _) => Task.FromResult<IVideoFeed?>(feed));
        Assert.Equal(LinkEndReason.Timeout, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(feed.Disposed);
    }
}
