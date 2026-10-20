using System.Collections.Concurrent;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class LinkSessionTests
{
    // Fast: PINGs rápidos e tempo-limite curto, só para os testes que provam o próprio tempo-limite (e a ausência de
    // silêncio maior que ele). Patient: PINGs igualmente rápidos, mas um tempo-limite que uma pausa do agendador (CI com
    // 2 núcleos, testes em paralelo) não alcança. Com o tempo-limite de 400 ms, uma pausa dessas fazia a sessão acabar
    // sozinha (Timeout + BYE) e o teste, que não fecha o socket, ficava lendo até o limite de 10 s e falhava com
    // OperationCanceledException sem dizer por quê.
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));
    private static readonly LinkSessionOptions Patient = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(30));

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
        }, Patient, cts.Token);

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
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Patient, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task EndsWhenPhoneClosesTheSocket()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode());
        await phone.DisposeAsync();
        Assert.Equal(LinkEndReason.Closed, await LinkSession.RunAsync(pc, _ => { }, Patient, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
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
        await Assert.ThrowsAsync<InvalidDataException>(() => LinkSession.RunAsync(pc, _ => { }, Patient, CancellationToken.None));
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
        }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

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
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Patient, CancellationToken.None, (_, _) => Task.FromResult<IVideoFeed?>(null)).WaitAsync(TimeSpan.FromSeconds(5)));
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

    /// <summary>
    /// Lado do iPhone: lê mensagens (anotando o tipo em <c>seen</c> e, se pedido, a hora de chegada em ms em <c>arrivals</c>)
    /// e responde PING com PONG até <c>done</c>.
    /// </summary>
    private static async Task AnswerUntilAsync(Stream phone, LinkFrameReader reader, Func<bool> done, List<LinkMessageType> seen, CancellationToken ct, List<long>? arrivals = null)
    {
        while (!done())
        {
            var frame = await reader.NextAsync(ct) ?? throw new IOException("o PC fechou o socket");
            arrivals?.Add(Environment.TickCount64);
            seen.Add(frame.MessageType ?? throw new InvalidDataException($"tipo desconhecido: {frame.Type}"));
            if (frame.MessageType == LinkMessageType.Bye && !done())
            {
                // A sessão acabou antes do esperado; sem isto o teste esperaria o socket (que ninguém fecha) até o limite.
                throw new InvalidOperationException($"o PC encerrou a sessão (BYE) antes do esperado; recebido: {string.Join(", ", seen)}");
            }
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
        }, Patient, cts.Token, async (_, token) =>
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
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, CancellationToken.None, async (_, token) =>
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
        }, Patient, cts.Token, (_, _) => throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC"));

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
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), [new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true)]);
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

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

    /// <summary>Feed cuja 1ª imagem só sai quando <see cref="Release"/> completar; depois manda imagens a cada 20 ms.</summary>
    private sealed class GatedFeed(VideoConfig config, VideoKind kind = VideoKind.Mirror) : IVideoFeed
    {
        private int _sent;
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }
        public VideoConfig Config => config;
        public VideoKind Kind => kind;

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            await Release.Task.WaitAsync(ct);
            if (Interlocked.Increment(ref _sent) == 1)
            {
                return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true);
            }
            await Task.Delay(20, ct);
            return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x41], false);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Uma imagem e depois para sozinho (como o monitor virtual que saiu do desktop) ou, com <c>failure</c>, falha
    /// (como uma captura que quebrou).
    /// </summary>
    private sealed class OneFrameThenStopsFeed(Exception? failure = null) : IVideoFeed
    {
        private int _calls;
        public bool Disposed { get; private set; }
        public VideoConfig Config => new(1366, 768, 60);

        public Task<AccessUnit> NextAsync(CancellationToken ct) =>
            Interlocked.Increment(ref _calls) == 1
                ? Task.FromResult(new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true))
                : Task.FromException<AccessUnit>(failure ?? new OperationCanceledException("o monitor virtual saiu do desktop"));

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ConfigWaitsForTheFirstFrame()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var feed = new GatedFeed(new VideoConfig(1366, 768, 60));
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        // Feed aberto mas sem imagem: nada de CONFIG (o iPhone ficaria preto esperando).
        await AnswerUntilAsync(phone, reader, () => seen.Count(t => t == LinkMessageType.Ping) >= 6, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Config, seen);

        feed.Release.SetResult();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video), seen, cts.Token);
        var config = seen.IndexOf(LinkMessageType.Config);
        Assert.True(config >= 0);
        Assert.Equal(LinkMessageType.Video, seen.Skip(config + 1).First(t => t != LinkMessageType.Ping));

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task SwitchOffSendsVideoEndAndSwitchOnOpensANewFeed()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feeds = new ConcurrentQueue<GatedFeed>();
        var statuses = new ConcurrentQueue<LinkStatus>();
        var run = LinkSession.RunAsync(pc, statuses.Enqueue, Patient, cts.Token, (_, _) =>
        {
            var feed = new GatedFeed(new VideoConfig(2556, 1179, 60), VideoKind.Extend);
            feed.Release.SetResult();
            feeds.Enqueue(feed);
            return Task.FromResult<IVideoFeed?>(feed);
        }, mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.Contains(statuses, s => s.Kind == VideoKind.Extend && s.Video == new VideoConfig(2556, 1179, 60));

        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.VideoEnd), seen, cts.Token);
        Assert.True(Assert.Single(feeds).Disposed);
        var afterEnd = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Count(t => t == LinkMessageType.Ping) >= 4, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Video, seen.Skip(afterEnd));
        Assert.False(run.IsCompleted);
        Assert.Null(statuses.Last().Video);

        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.Equal(LinkMessageType.Config, seen.Skip(afterEnd).First(t => t != LinkMessageType.Ping));
        Assert.Equal(2, feeds.Count);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.All(feeds, f => Assert.True(f.Disposed));
    }

    [Fact]
    public async Task FeedThatStopsByItselfSendsVideoEndAndTurnsTheSwitchOff()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feed = new OneFrameThenStopsFeed();
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.VideoEnd), seen, cts.Token);
        Assert.True(feed.Disposed);
        Assert.False(mode.WantsVideo);
        Assert.True(mode.IsConnected);

        var afterEnd = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.False(run.IsCompleted);
        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task SwitchOffBeforeTheFirstFrameSendsNoVideoEnd()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feed = new GatedFeed(new VideoConfig(1366, 768, 60)); // nunca libera a 1ª imagem
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Count(t => t == LinkMessageType.Ping) >= 2, seen, cts.Token);
        mode.Toggle();
        await AnswerUntilAsync(phone, reader, () => feed.Disposed && seen.Count(t => t == LinkMessageType.Ping) >= 6, seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Config, seen);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task VideoThatDoesNotOpenTurnsTheSwitchOff()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(null), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, new LinkFrameReader(phone), () => !mode.WantsVideo && seen.Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.True(mode.IsConnected);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    [Fact]
    public async Task VideoThatThrowsWhileOpeningTurnsTheSwitchOffAndKeepsTheSession()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC"), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, new LinkFrameReader(phone), () => !mode.WantsVideo && seen.Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.True(mode.IsConnected);
        Assert.False(run.IsCompleted);
        Assert.DoesNotContain(LinkMessageType.Config, seen);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }

    /// <summary>Imagens a cada 20 ms, sem fim; o descarte demora <c>disposeDelay</c> e conta as chamadas.</summary>
    private sealed class SlowToDisposeFeed(TimeSpan disposeDelay) : IVideoFeed
    {
        private int _disposals;
        private int _disposed;
        public int Disposals => Volatile.Read(ref _disposals);
        public bool Disposed => Volatile.Read(ref _disposed) == 1;
        public VideoConfig Config => new(2556, 1179, 60);
        public VideoKind Kind => VideoKind.Extend;

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            await Task.Delay(20, ct);
            return new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true);
        }

        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            await Task.Delay(disposeDelay); // como o FFmpeg que leva segundos para sair
            Volatile.Write(ref _disposed, 1);
        }
    }

    /// <summary>O iPhone desconecta depois do tempo-limite sem receber nada: nenhum intervalo entre mensagens pode chegar a ele.</summary>
    private static void AssertNoSilenceLongerThanTimeout(List<long> arrivals)
    {
        var longest = arrivals.Zip(arrivals.Skip(1), (a, b) => b - a).DefaultIfEmpty(0).Max();
        Assert.True(longest < Fast.Timeout.TotalMilliseconds, $"o iPhone ficou {longest} ms sem receber nada");
    }

    [Fact]
    public async Task SwitchOffWhileVideoIsStillOpeningKeepsPingingAndDisposesTheLateFeedOnce()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feed = new SlowToDisposeFeed(TimeSpan.Zero);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, async (_, _) =>
        {
            started.TrySetResult();
            await Task.Delay(TimeSpan.FromSeconds(1)); // ignora o token, como a sondagem do FFmpeg que não para a tempo
            return feed;
        }, mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => started.Task.IsCompleted && seen.Count(t => t == LinkMessageType.Ping) >= 2, seen, cts.Token);

        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        var afterOff = seen.Count;
        var arrivals = new List<long> { Environment.TickCount64 };
        await AnswerUntilAsync(phone, reader, () =>
            seen.Contains(LinkMessageType.Bye) || (feed.Disposed && seen.Skip(afterOff).Count(t => t == LinkMessageType.Ping) >= 4), seen, cts.Token, arrivals);
        Assert.DoesNotContain(LinkMessageType.Bye, seen);
        Assert.False(run.IsCompleted);
        AssertNoSilenceLongerThanTimeout(arrivals);
        Assert.Equal(1, feed.Disposals);
        Assert.DoesNotContain(LinkMessageType.Config, seen);
        Assert.DoesNotContain(LinkMessageType.VideoEnd, seen);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.Equal(1, feed.Disposals);
    }

    [Fact]
    public async Task SwitchOffWithAFeedThatIsSlowToDisposeKeepsPingingAndCanTurnOnAgainRightAway()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feeds = new ConcurrentQueue<SlowToDisposeFeed>();
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, (_, _) =>
        {
            var feed = new SlowToDisposeFeed(TimeSpan.FromSeconds(1));
            feeds.Enqueue(feed);
            return Task.FromResult<IVideoFeed?>(feed);
        }, mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.True(feeds.TryPeek(out var first));

        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        var afterOff = seen.Count;
        var arrivals = new List<long> { Environment.TickCount64 };
        await AnswerUntilAsync(phone, reader, () =>
            seen.Contains(LinkMessageType.Bye)
            || (first.Disposed && seen.Contains(LinkMessageType.VideoEnd) && seen.Skip(afterOff).Count(t => t == LinkMessageType.Ping) >= 4), seen, cts.Token, arrivals);
        Assert.DoesNotContain(LinkMessageType.Bye, seen);
        Assert.False(run.IsCompleted);
        AssertNoSilenceLongerThanTimeout(arrivals);
        Assert.Equal(1, first.Disposals);
        var end = seen.IndexOf(LinkMessageType.VideoEnd);
        Assert.DoesNotContain(LinkMessageType.Video, seen.Skip(end));

        // Desligar e ligar logo em seguida: o feed novo não espera o descarte lento do anterior.
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        var afterOn = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.Bye) || seen.Skip(afterOn).Contains(LinkMessageType.Video), seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Bye, seen);
        Assert.Equal(LinkMessageType.Config, seen.Skip(afterOn).First(t => t != LinkMessageType.Ping));

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.All(feeds, f => Assert.Equal(1, f.Disposals));
        Assert.All(feeds, f => Assert.True(f.Disposed));
    }

    [Theory]
    [InlineData(typeof(InvalidDataException))]
    [InlineData(typeof(IOException))]
    public async Task FeedThatFailsSendsVideoEndAndKeepsTheSession(Type failure)
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mode = new MonitorMode();
        mode.SessionStarted();
        var feed = new OneFrameThenStopsFeed((Exception)Activator.CreateInstance(failure, "a captura quebrou")!);
        var run = LinkSession.RunAsync(pc, _ => { }, Patient, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed), mode);

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var seen = new List<LinkMessageType>();
        await AnswerUntilAsync(phone, reader, () => seen.Contains(LinkMessageType.VideoEnd) || seen.Contains(LinkMessageType.Bye), seen, cts.Token);
        Assert.DoesNotContain(LinkMessageType.Bye, seen);
        Assert.Contains(LinkMessageType.VideoEnd, seen);
        Assert.False(mode.WantsVideo);
        Assert.True(mode.IsConnected);

        var afterEnd = seen.Count;
        await AnswerUntilAsync(phone, reader, () => seen.Skip(afterEnd).Count(t => t == LinkMessageType.Ping) >= 3, seen, cts.Token);
        Assert.False(run.IsCompleted);
        Assert.True(feed.Disposed);
        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
    }
}
