namespace TelaDois.Core.Video;

/// <summary>
/// Fila curta entre o FFmpeg e o cabo. Cheia = o cabo está atrasado: joga fora o que está pendente e
/// as próximas imagens até chegar uma completa, para nunca acumular atraso.
/// </summary>
public sealed class FrameQueue(int capacity = 3)
{
    private readonly Queue<AccessUnit> _items = new();
    private readonly SemaphoreSlim _signal = new(0);
    private bool _waitingForKeyframe;
    private long _dropped;

    public long Dropped => Interlocked.Read(ref _dropped);

    public void Add(AccessUnit unit)
    {
        lock (_items)
        {
            if (_waitingForKeyframe && !unit.IsKeyframe)
            {
                Interlocked.Increment(ref _dropped);
                return;
            }
            _waitingForKeyframe = false;
            if (_items.Count >= capacity)
            {
                Interlocked.Add(ref _dropped, _items.Count);
                _items.Clear();
                if (!unit.IsKeyframe)
                {
                    Interlocked.Increment(ref _dropped);
                    _waitingForKeyframe = true;
                    return;
                }
            }
            _items.Enqueue(unit);
        }
        _signal.Release();
    }

    public async Task<AccessUnit> TakeAsync(CancellationToken ct)
    {
        while (true)
        {
            await _signal.WaitAsync(ct);
            lock (_items)
            {
                if (_items.TryDequeue(out var unit))
                {
                    return unit;
                }
            }
        }
    }
}
