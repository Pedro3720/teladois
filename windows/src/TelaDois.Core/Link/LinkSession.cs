using System.Diagnostics;
using TelaDois.Core.Video;

namespace TelaDois.Core.Link;

public sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout)
{
    public static LinkSessionOptions Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));

    /// <summary>
    /// Quanto o feed aberto tem para entregar a 1ª imagem. Estourou: o vídeo desliga como se tivesse parado sozinho
    /// (uma captura que nunca entrega deixaria um monitor estendido invisível, sem aviso).
    /// </summary>
    public TimeSpan FirstFrameTimeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>Estado da conexão: resolução do iPhone, a última latência medida e, com o vídeo ligado, formato e tipo.</summary>
public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null, VideoKind? Kind = null);

public enum LinkEndReason
{
    Bye,
    Timeout,
    Closed,
    Cancelled,
}

/// <summary>
/// Lado do PC: espera o HELLO, manda PING logo em seguida e a cada intervalo, mede o PONG e encerra no BYE,
/// quando o iPhone fecha o socket ou após o tempo-limite sem receber nada (ou com uma escrita travada).
/// O vídeo liga e desliga pelo <see cref="IVideoSwitch"/> (sem ele, fica ligado): ligar abre o feed em paralelo (depois
/// do descarte do feed anterior) e manda CONFIG junto com a 1ª imagem; desligar — ou o feed parar sozinho, falhar ou não abrir — descarta o feed em
/// segundo plano (os PINGs continuam) e, se o iPhone tinha recebido CONFIG, manda VIDEO_END; a sessão continua.
/// Ao sair, descarta o vídeo e manda BYE.
/// </summary>
public static class LinkSession
{
    // Ao sair, quanto o BYE espera o descarte dos feeds (o FFmpeg tem até 3 s para sair).
    private static readonly TimeSpan RetireLimit = TimeSpan.FromSeconds(5);

    public static async Task<LinkEndReason> RunAsync(
        Stream stream,
        Action<LinkStatus> onStatus,
        LinkSessionOptions options,
        CancellationToken ct,
        Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null,
        IVideoSwitch? videoSwitch = null,
        Action<string>? log = null)
    {
        var reader = new LinkFrameReader(stream);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clock = Stopwatch.StartNew();
        long lastHeard = 0;
        var statusLock = new object();
        LinkStatus? status = null;
        Task<LinkEndReason>? receiving = null;
        CancellationTokenSource? videoCts = null;
        Task<IVideoFeed?>? opening = null;
        IVideoFeed? feed = null;
        Task? sending = null;
        var retiring = new List<Task>();
        var configSent = 0;

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

        void StartVideo(HelloInfo hello)
        {
            if (openVideo is null)
            {
                return;
            }
            videoCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            var token = videoCts.Token;
            // Uma operação por vez: o feed novo só abre depois do descarte dos anteriores, que usam o mesmo monitor
            // virtual (um Detach atrasado tiraria o monitor do feed novo). A espera fica dentro da tarefa, fora do laço:
            // os PINGs continuam. Um descarte travado não prende o ligar para sempre (limite do RetireLimit).
            var previous = retiring.Where(t => !t.IsCompleted).ToArray();
            // Task.Run: uma abertura que trava ou lança sem await também fica numa tarefa observada.
            opening = Task.Run(async () =>
            {
                if (previous.Length > 0)
                {
                    await ObserveAsync(Task.WhenAll(previous).WaitAsync(RetireLimit, token));
                    token.ThrowIfCancellationRequested();
                }
                return await openVideo(hello, token);
            }, CancellationToken.None);
        }

        // Termina sem erro quando o feed acaba, falha ou é cancelado (o vídeo desliga e a sessão segue); só uma falha
        // de escrita (IOException/TimeoutException) ou o fim da sessão saem como exceção.
        void StartSending(IVideoFeed activeFeed)
        {
            var token = videoCts!.Token;
            async Task<AccessUnit?> NextOrEndAsync(CancellationToken next)
            {
                try
                {
                    return await activeFeed.NextAsync(next);
                }
                catch (Exception)
                {
                    return null; // quem captura registra o motivo
                }
            }

            sending = Task.Run(async () =>
            {
                // CONFIG só com a 1ª imagem pronta (I1): uma captura que falha logo não deixa o iPhone preto. Sem imagem
                // no prazo, termina como um feed que parou sozinho: o vídeo desliga e o feed é descartado.
                AccessUnit? unit;
                using (var firstFrame = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    firstFrame.CancelAfter(options.FirstFrameTimeout);
                    unit = await NextOrEndAsync(firstFrame.Token);
                    if (unit is null && firstFrame.IsCancellationRequested && !token.IsCancellationRequested)
                    {
                        log?.Invoke($"nenhuma imagem do vídeo em {options.FirstFrameTimeout.TotalSeconds:0} s: Segunda Tela desligada");
                    }
                }
                if (unit is null || token.IsCancellationRequested)
                {
                    return;
                }
                await SendAsync(LinkMessages.Config(activeFeed.Config), cts.Token);
                Interlocked.Exchange(ref configSent, 1);
                Update(s => s with { Video = activeFeed.Config, Kind = activeFeed.Kind });
                var started = Stopwatch.StartNew();
                while (unit is not null && !token.IsCancellationRequested)
                {
                    // Escritas com o token da sessão: cancelar o vídeo no meio de uma escrita cortaria a mensagem.
                    await SendAsync(LinkMessages.Video(unit.IsKeyframe, (ulong)(started.Elapsed.Ticks / 10), unit.Data), cts.Token);
                    unit = await NextOrEndAsync(token);
                }
            }, CancellationToken.None);
        }

        // Espera a abertura e descarta o feed (uma vez só) fora do laço: os dois podem levar segundos (sondagem do
        // FFmpeg, processo saindo) e o laço não pode ficar sem mandar PING. Engole qualquer falha deles.
        async Task RetireAsync(Task? stoppedSending, Task<IVideoFeed?>? stoppedOpening, IVideoFeed? stoppedFeed, CancellationTokenSource stoppedCts)
        {
            await ObserveAsync(stoppedSending);
            if (stoppedOpening is not null)
            {
                try
                {
                    stoppedFeed ??= await stoppedOpening;
                }
                catch (Exception)
                {
                }
            }
            if (stoppedFeed is not null)
            {
                try
                {
                    await stoppedFeed.DisposeAsync();
                }
                catch (Exception)
                {
                }
            }
            stoppedCts.Dispose();
        }

        // Cancela o vídeo atual e entrega abertura, envio e feed para uma tarefa "aposentando"; não manda nada.
        // Devolve a tarefa de envio parada (ou null), para quem chama esperar o fim das escritas dela.
        Task? RetireVideo()
        {
            if (videoCts is null)
            {
                return null;
            }
            var stoppedSending = sending;
            videoCts.Cancel();
            retiring.RemoveAll(t => t.IsCompleted);
            retiring.Add(RetireAsync(stoppedSending, opening, feed, videoCts));
            videoCts = null;
            opening = null;
            feed = null;
            sending = null;
            return stoppedSending;
        }

        // Desliga o vídeo com a sessão viva, sem esperar a abertura nem o descarte do feed. Antes do VIDEO_END espera
        // só o envio parar (limitado pelo tempo-limite): assim nenhum VIDEO sai depois dele. O iPhone só recebe
        // VIDEO_END se chegou a receber CONFIG; uma falha de escrita do envio encerra a sessão.
        async Task StopVideoAsync()
        {
            if (RetireVideo() is { } stoppedSending)
            {
                await stoppedSending.WaitAsync(options.Timeout, cts.Token);
            }
            if (Interlocked.Exchange(ref configSent, 0) == 1)
            {
                Update(s => s with { Video = null, Kind = null });
                await SendAsync(LinkMessages.VideoEnd(), cts.Token);
            }
        }

        try
        {
            var hello = await ReadHelloAsync(reader, options.Timeout, cts.Token);
            if (hello is null)
            {
                return LinkEndReason.Closed;
            }
            var helloInfo = hello.Value;
            status = new LinkStatus(helloInfo, null);
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

            var wantVideo = videoSwitch?.WantsVideo ?? true;
            if (wantVideo)
            {
                StartVideo(helloInfo);
            }
            var switchChanged = videoSwitch?.WaitForChangeAsync(wantVideo, cts.Token) ?? Task.Delay(Timeout.Infinite, cts.Token);

            await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
            var tick = Task.Delay(options.PingInterval, cts.Token);
            while (true)
            {
                var pending = new List<Task>(5) { receiving, tick, switchChanged };
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
                if (done == switchChanged)
                {
                    ct.ThrowIfCancellationRequested();
                    var wanted = videoSwitch!.WantsVideo;
                    switchChanged = videoSwitch.WaitForChangeAsync(wanted, cts.Token);
                    if (wanted != wantVideo)
                    {
                        wantVideo = wanted;
                        if (wanted)
                        {
                            StartVideo(helloInfo);
                        }
                        else
                        {
                            await StopVideoAsync();
                        }
                    }
                    continue;
                }
                if (done == sending)
                {
                    // Relança a falha de escrita (IOException → Closed, TimeoutException → Timeout).
                    await sending;
                    ct.ThrowIfCancellationRequested();
                    // O feed parou sozinho (ex.: o monitor virtual saiu do desktop) ou falhou: vídeo desligado, a sessão segue.
                    await StopVideoAsync();
                    wantVideo = false;
                    videoSwitch?.VideoEnded();
                    continue;
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
                        // Quem abre registra o motivo.
                    }
                    if (feed is not null)
                    {
                        StartSending(feed);
                    }
                    else
                    {
                        // Sem Segunda Tela agora: o túnel continua e o atalho pode tentar de novo. Ainda não há envio.
                        _ = RetireVideo();
                        wantVideo = false;
                        videoSwitch?.VideoEnded();
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
            try
            {
                // Inclui um feed que ainda termina de abrir: todo feed é descartado (uma vez só) antes do BYE.
                // O envio parado é esperado pela própria tarefa que aposenta.
                _ = RetireVideo();
                await ObserveAsync(Task.WhenAll(retiring).WaitAsync(RetireLimit));
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
