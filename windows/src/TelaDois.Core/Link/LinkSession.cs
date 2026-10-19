using System.Diagnostics;

namespace TelaDois.Core.Link;

public sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout)
{
    public static LinkSessionOptions Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
}

/// <summary>Estado da conexão: resolução do iPhone e a última latência de ida e volta medida.</summary>
public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip);

public enum LinkEndReason
{
    Bye,
    Timeout,
    Closed,
    Cancelled,
}

/// <summary>
/// Lado do PC: espera o HELLO, manda PING a cada intervalo, mede o PONG e encerra no BYE,
/// quando o iPhone fecha o socket ou após o tempo-limite sem receber nada. Ao sair, manda BYE.
/// </summary>
public static class LinkSession
{
    public static async Task<LinkEndReason> RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct)
    {
        var reader = new LinkFrameReader(stream);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clock = Stopwatch.StartNew();
        long lastHeard = 0;
        Task<LinkEndReason>? receiving = null;

        async Task SendAsync(LinkFrame frame, CancellationToken token)
        {
            await writeLock.WaitAsync(token);
            try
            {
                await stream.WriteAsync(frame.Encode(), token);
            }
            finally
            {
                writeLock.Release();
            }
        }

        try
        {
            var hello = await ReadHelloAsync(reader, options.Timeout, cts.Token);
            if (hello is null)
            {
                return LinkEndReason.Closed;
            }
            var status = new LinkStatus(hello.Value, null);
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
                            onStatus(status with { RoundTrip = TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds - (long)sent) });
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

            while (true)
            {
                var tick = Task.Delay(options.PingInterval, cts.Token);
                if (await Task.WhenAny(receiving, tick) == receiving)
                {
                    return await receiving;
                }
                ct.ThrowIfCancellationRequested();
                if (clock.ElapsedMilliseconds - Interlocked.Read(ref lastHeard) > options.Timeout.TotalMilliseconds)
                {
                    return LinkEndReason.Timeout;
                }
                await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
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
            cts.Cancel();
            if (receiving is not null)
            {
                try
                {
                    await receiving;
                }
                catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
                {
                }
            }
            using var byeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            try
            {
                await SendAsync(LinkMessages.Bye(), byeTimeout.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
            }
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
