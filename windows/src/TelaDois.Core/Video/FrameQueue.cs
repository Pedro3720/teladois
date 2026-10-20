using System.Diagnostics;

namespace TelaDois.Core.Video;

/// <summary>
/// Fila entre o FFmpeg e o cabo, limitada por <b>tempo</b>. Se a imagem mais antiga esperando passou de
/// <c>maxAge</c> (padrão 100 ms), o cabo está atrasado: joga fora o que está pendente e as próximas imagens até
/// chegar uma completa, para nunca acumular atraso. Rajadas curtas (várias imagens de uma leitura só, depois de um
/// engasgo do PC) passam inteiras. Com <see cref="MaxPending"/> imagens esperando, descarta do mesmo jeito (teto de memória).
/// </summary>
public sealed class FrameQueue
{
    /// <summary>Teto de memória: com tantas imagens esperando, descarta mesmo que ainda sejam recentes.</summary>
    public const int MaxPending = 240;

    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMilliseconds(100);

    private readonly Queue<(AccessUnit Unit, long AddedMs)> _items = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly double _maxAgeMs;
    private readonly Func<long> _nowMs;
    private readonly VideoMeters? _meters;
    private bool _waitingForKeyframe;
    private long _dropped;

    /// <param name="maxAge">Idade máxima da imagem mais antiga esperando (padrão 100 ms; precisa ser positiva).</param>
    /// <param name="nowMs">Relógio em milissegundos (padrão: <see cref="Stopwatch"/>); os testes passam um relógio falso.</param>
    /// <param name="meters">Se houver, recebe cada descarte e a idade de cada imagem ao sair (só medição).</param>
    public FrameQueue(TimeSpan? maxAge = null, Func<long>? nowMs = null, VideoMeters? meters = null)
    {
        _meters = meters;
        var age = maxAge ?? DefaultMaxAge;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(age, TimeSpan.Zero, nameof(maxAge));
        _maxAgeMs = age.TotalMilliseconds;
        _nowMs = nowMs ?? (() => Stopwatch.GetElapsedTime(0).Ticks / TimeSpan.TicksPerMillisecond);
    }

    /// <summary>Total de imagens descartadas.</summary>
    public long Dropped => Interlocked.Read(ref _dropped);

    /// <summary>
    /// Avisa o <b>início</b> de cada episódio de descarte (uma vez por episódio, não por imagem), já com
    /// <see cref="Dropped"/> atualizado. Chamado fora da trava, na thread de quem chamou <see cref="Add"/>.
    /// </summary>
    public event Action? DropStarted;

    public void Add(AccessUnit unit)
    {
        var dropStarted = false;
        var enqueued = false;
        lock (_items)
        {
            if (_waitingForKeyframe && !unit.IsKeyframe)
            {
                Interlocked.Increment(ref _dropped);
                _meters?.FramesDropped(1);
                return;
            }
            _waitingForKeyframe = false;
            var now = _nowMs();
            if (_items.Count > 0 && (now - _items.Peek().AddedMs > _maxAgeMs || _items.Count >= MaxPending))
            {
                dropStarted = true;
                Interlocked.Add(ref _dropped, _items.Count);
                _meters?.FramesDropped(_items.Count);
                _items.Clear();
                if (!unit.IsKeyframe)
                {
                    Interlocked.Increment(ref _dropped);
                    _meters?.FramesDropped(1);
                    _waitingForKeyframe = true;
                }
            }
            if (!_waitingForKeyframe)
            {
                _items.Enqueue((unit, now));
                enqueued = true;
            }
        }
        if (dropStarted)
        {
            DropStarted?.Invoke();
        }
        if (enqueued)
        {
            _signal.Release();
        }
    }

    /// <summary>
    /// Próxima imagem, esperando se preciso. Cancelado o token, lança <see cref="OperationCanceledException"/>
    /// mesmo que haja imagens na fila.
    /// </summary>
    public async Task<AccessUnit> TakeAsync(CancellationToken ct)
    {
        while (true)
        {
            // Um sinal pode sobrar de uma imagem descartada: aí a fila está vazia e espera o próximo.
            await _signal.WaitAsync(ct);
            lock (_items)
            {
                if (_items.TryDequeue(out var item))
                {
                    _meters?.QueueAge(TimeSpan.FromMilliseconds(_nowMs() - item.AddedMs));
                    return item.Unit;
                }
            }
        }
    }
}
