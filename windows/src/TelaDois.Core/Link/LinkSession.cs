using System.Diagnostics;
using TelaDois.Core.Video;

namespace TelaDois.Core.Link;

public sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout)
{
    public static LinkSessionOptions Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
}

/// <summary>Estado da conexão: resolução do iPhone e a última latência de ida e volta medida.</summary>
public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null);

public enum LinkEndReason
{
    Bye,
    Timeout,
    Closed,
    Cancelled,
}

/// <summary>
/// Lado do PC: espera o HELLO, manda PING logo em seguida e a cada intervalo, mede o PONG e encerra no BYE,
/// quando o iPhone fecha o socket ou após o tempo-limite sem receber nada (ou com uma escrita travada). Enquanto isso
/// abre o vídeo em paralelo: se abrir, manda CONFIG e passa a mandar VIDEO; se falhar, a sessão segue sem vídeo.
/// Ao sair, descarta o vídeo (mesmo o que terminar de abrir depois) e manda BYE.
/// </summary>
public static class LinkSession
{
    public static async Task<LinkEndReason> RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)
    {
        var reader = new LinkFrameReader(stream);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clock = Stopwatch.StartNew();
        long lastHeard = 0;
        var statusLock = new object();
        LinkStatus? status = null;
        Task<LinkEndReason>? receiving = null;
        Task<IVideoFeed?>? opening = null;
        IVideoFeed? feed = null;
        Task? sending = null;

        // Toda escrita, inclusive a espera pela vez, tem tempo-limite: um iPhone que parou de ler encerra a sessão.
        async Task SendAsync(LinkFrame frame, CancellationToken token)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
            limit.CancelAfter(options.Timeout);
            try
            {
                await writeLock.WaitAsync(limit.Token);
                try
                {
                    await stream.WriteAsync(frame.Encode(), limit.Token);
                }
                finally
                {
                    writeLock.Release();
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException("o iPhone parou de receber");
            }
        }

        // Serializa as atualizações para uma medição de latência nunca apagar o vídeo do status (e vice-versa).
        void Update(Func<LinkStatus, LinkStatus> change)
        {
            lock (statusLock)
            {
                status = change(status!);
                onStatus(status);
            }
        }

        try
        {
            var hello = await ReadHelloAsync(reader, options.Timeout, cts.Token);
            if (hello is null)
            {
                return LinkEndReason.Closed;
            }
            status = new LinkStatus(hello.Value, null);
            onStatus(status);
            Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);

            receiving = Task.Run(async () =>
            {
                while (await reader.NextAsync(cts.Token) is { } frame)
                {
                    Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);
                    switch (frame.MessageType)
                    {
                        case LinkMessageType.Pong when LinkMessages.TryReadTimestamp(frame, out var sent):
                            var roundTrip = TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds - (long)sent);
                            Update(s => s with { RoundTrip = roundTrip });
                            break;
                        case LinkMessageType.Ping when LinkMessages.TryReadTimestamp(frame, out var theirs):
                            await SendAsync(LinkMessages.Pong(theirs), cts.Token);
                            break;
                        case LinkMessageType.Bye:
                            return LinkEndReason.Bye;
                    }
                }
                return LinkEndReason.Closed;
            }, CancellationToken.None);

            if (openVideo is not null)
            {
                // Task.Run: uma abertura que trava ou lança sem await também fica numa tarefa observada.
                var helloInfo = hello.Value;
                opening = Task.Run(() => openVideo(helloInfo, cts.Token), CancellationToken.None);
            }

            await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
            var tick = Task.Delay(options.PingInterval, cts.Token);
            while (true)
            {
                var pending = new List<Task>(4) { receiving, tick };
                if (sending is not null)
                {
                    pending.Add(sending);
                }
                if (opening is not null)
                {
                    pending.Add(opening);
                }
                var done = await Task.WhenAny(pending);
                if (done == receiving)
                {
                    return await receiving;
                }
                if (done == sending)
                {
                    try
                    {
                        await sending; // relança a falha de escrita (IOException → Closed, TimeoutException → Timeout)
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        // O próprio vídeo parou (descartado por fora): fecha a sessão e o túnel abre outra.
                    }
                    return LinkEndReason.Closed;
                }
                if (done == opening)
                {
                    var opened = opening;
                    opening = null;
                    try
                    {
                        feed = await opened;
                    }
                    catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
                    {
                        // Sem Segunda Tela nesta sessão: o túnel continua (quem abre registra o motivo).
                    }
                    if (feed is not null)
                    {
                        var activeFeed = feed;
                        await SendAsync(LinkMessages.Config(activeFeed.Config), cts.Token);
                        Update(s => s with { Video = activeFeed.Config });
                        var started = Stopwatch.StartNew();
                        sending = Task.Run(async () =>
                        {
                            while (true)
                            {
                                var unit = await activeFeed.NextAsync(cts.Token);
                                await SendAsync(LinkMessages.Video(unit.IsKeyframe, (ulong)(started.Elapsed.Ticks / 10), unit.Data), cts.Token);
                            }
                        }, CancellationToken.None);
                    }
                    continue;
                }
                ct.ThrowIfCancellationRequested();
                if (clock.ElapsedMilliseconds - Interlocked.Read(ref lastHeard) > options.Timeout.TotalMilliseconds)
                {
                    return LinkEndReason.Timeout;
                }
                await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
                tick = Task.Delay(options.PingInterval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return LinkEndReason.Cancelled;
        }
        catch (TimeoutException)
        {
            return LinkEndReason.Timeout;
        }
        catch (IOException)
        {
            return LinkEndReason.Closed;
        }
        finally
        {
            // A limpeza engole qualquer falha das tarefas: a exceção que sai é a original da sessão.
            cts.Cancel();
            await ObserveAsync(receiving);
            await ObserveAsync(sending);
            if (opening is not null)
            {
                try
                {
                    feed ??= await opening;
                }
                catch (Exception)
                {
                }
            }
            try
            {
                if (feed is not null)
                {
                    try
                    {
                        await feed.DisposeAsync();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            finally
            {
                using var byeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                try
                {
                    await SendAsync(LinkMessages.Bye(), byeTimeout.Token);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private static async Task ObserveAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }
        try
        {
            await task;
        }
        catch (Exception)
        {
        }
    }

    private static async Task<HelloInfo?> ReadHelloAsync(LinkFrameReader reader, TimeSpan timeout, CancellationToken ct)
    {
        using var helloCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        helloCts.CancelAfter(timeout);
        LinkFrame? first;
        try
        {
            first = await reader.NextAsync(helloCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("o iPhone não mandou HELLO a tempo");
        }
        if (first is null)
        {
            return null;
        }
        return LinkMessages.TryParseHello(first, out var hello)
            ? hello
            : throw new InvalidDataException("a primeira mensagem do iPhone não é HELLO");
    }
}
