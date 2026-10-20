using System.Globalization;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Roda o FFmpeg e põe as imagens na <see cref="FrameQueue"/>. Se o processo morrer, não iniciar ou a leitura falhar,
/// registra no log e reinicia após a espera seguinte de <c>backoff</c> (1 s, 2 s, 5 s), que volta ao início quando
/// chegam imagens de novo. Descartar encerra o processo (mesmo com uma leitura do pipe presa) e cancela quem espera
/// em <see cref="NextAsync"/>.
/// </summary>
public sealed class FfmpegCapture : IVideoFeed
{
    private static readonly TimeSpan[] DefaultBackoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private readonly FrameQueue _queue;
    // Não é descartado de propósito: sem timer nem vínculo, não segura recurso, e o token continua válido para
    // NextAsync/DisposeAsync chamados depois do DisposeAsync (cancelado, em vez de ObjectDisposedException).
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;
    private readonly Action? _endedWithoutFrames;
    private readonly Action<string> _log;
    private int _produced;
    private int _reported;

    /// <param name="maxQueueAge">Idade máxima de uma imagem esperando o cabo (padrão 100 ms, ver <see cref="FrameQueue"/>).</param>
    /// <param name="endedWithoutFrames">
    /// Chamado no máximo uma vez, quando a linha de captura falha sem nunca ter produzido uma imagem: o FFmpeg termina
    /// (ou não inicia) sozinho <b>2 vezes seguidas</b> (a 1ª falha logo depois de ligar o monitor, ex.: ddagrab
    /// <c>887a0026</c>, é comum e passa), ou a sessão avisa por <see cref="FirstFrameMissed"/> que o prazo da 1ª imagem
    /// estourou. Descartar (o usuário desligou, tirou o cabo ou saiu) não chama: isso não diz nada sobre a compressão.
    /// Se ele lançar, a falha só vai para o log.
    /// </param>
    public FfmpegCapture(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, VideoConfig config, Action<string> log, IReadOnlyList<TimeSpan>? backoff = null, TimeSpan? maxQueueAge = null, Action? endedWithoutFrames = null)
    {
        var waits = backoff ?? DefaultBackoff;
        if (waits.Count == 0)
        {
            throw new ArgumentException("É preciso ao menos uma espera de reinício.", nameof(backoff));
        }
        _endedWithoutFrames = endedWithoutFrames;
        _log = log;
        Config = config;
        _queue = new FrameQueue(maxQueueAge);
        _queue.DropStarted += () => log($"cabo atrasado: descartando até a próxima imagem completa ({_queue.Dropped} descartadas até agora)");
        _pump = Task.Run(() => PumpAsync(runner, ffmpegPath, arguments, log, waits, _cts.Token));
    }

    public VideoConfig Config { get; }

    public long Dropped => _queue.Dropped;

    /// <summary>Próxima imagem. Termina com <see cref="OperationCanceledException"/> se <c>ct</c> ou o descarte da captura cancelar.</summary>
    public async Task<AccessUnit> NextAsync(CancellationToken ct)
    {
        if (!ct.CanBeCanceled)
        {
            return await _queue.TakeAsync(_cts.Token);
        }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        return await _queue.TakeAsync(linked.Token);
    }

    /// <summary>Nunca lança: com <c>ct</c> cancelado, retorna; qualquer outra falha vai para o log e para a espera de reinício.</summary>
    private async Task PumpAsync(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, Action<string> log, IReadOnlyList<TimeSpan> waits, CancellationToken ct)
    {
        var attempt = 0;
        var runsWithoutFrames = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await RunOnceAsync(runner, ffmpegPath, arguments, log, ct))
                {
                    attempt = 0;
                    runsWithoutFrames = 0;
                }
                else
                {
                    runsWithoutFrames++;
                }
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                log($"FFmpeg falhou: {e.Message}");
                runsWithoutFrames++;
            }
            if (ct.IsCancellationRequested)
            {
                return;
            }
            if (runsWithoutFrames >= 2)
            {
                ReportNoFrames();
            }
            var wait = waits[Math.Min(attempt, waits.Count - 1)];
            attempt++;
            log($"FFmpeg: nova tentativa em {wait.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)} ms");
            try
            {
                await Task.Delay(wait, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Uma execução do FFmpeg, do início ao fim. Retorna se chegaram imagens.</summary>
    private async Task<bool> RunOnceAsync(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, Action<string> log, CancellationToken ct)
    {
        IRunningProcess process;
        try
        {
            process = runner.Start(ffmpegPath, arguments);
        }
        catch (Exception e) when (!ct.IsCancellationRequested)
        {
            log($"FFmpeg não iniciou: {e.Message}");
            return false;
        }
        var stopper = new ProcessStopper(process);
        var sawFrames = false;
        try
        {
            // A leitura de um pipe anônimo pode ignorar o token: no cancelamento, encerrar o processo fecha o pipe e a destrava.
            using (ct.Register(() => _ = stopper.StopAsync()))
            {
                try
                {
                    await foreach (var unit in new AccessUnitReader(process.Output).ReadAllAsync(ct))
                    {
                        sawFrames = true;
                        Volatile.Write(ref _produced, 1);
                        _queue.Add(unit);
                    }
                }
                catch (Exception e) when (!ct.IsCancellationRequested)
                {
                    log($"leitura do FFmpeg falhou: {e.Message}");
                }
            }
            if (!ct.IsCancellationRequested)
            {
                log($"FFmpeg terminou: {process.RecentErrors}");
            }
        }
        finally
        {
            await stopper.StopAsync();
        }
        return sawFrames;
    }

    /// <summary>Descarta o processo uma vez só, seja pelo cancelamento, seja pelo fim normal da leitura.</summary>
    private sealed class ProcessStopper(IRunningProcess process)
    {
        private readonly Lock _gate = new();
        private Task? _stopping;

        public Task StopAsync()
        {
            lock (_gate)
            {
                // Em outra thread: chamado de dentro do Cancel, não roda o descarte (que mata a árvore) nele.
                return _stopping ??= Task.Run(() => process.DisposeAsync().AsTask());
            }
        }
    }

    /// <summary>O prazo da 1ª imagem estourou: se o FFmpeg ainda não entregou nenhuma, avisa (uma vez) que a compressão falhou.</summary>
    public void FirstFrameMissed() => ReportNoFrames();

    /// <summary>Nunca lança: chamado do pump (que não pode cair) e da sessão.</summary>
    private void ReportNoFrames()
    {
        if (Volatile.Read(ref _produced) == 0 && Interlocked.Exchange(ref _reported, 1) == 0)
        {
            try
            {
                _endedWithoutFrames?.Invoke();
            }
            catch (Exception e)
            {
                _log($"aviso de captura sem imagem falhou: {e.Message}");
            }
        }
    }

    /// <summary>Idempotente: pode ser chamado de novo (ou ao mesmo tempo) sem lançar.</summary>
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _pump;
    }
}
