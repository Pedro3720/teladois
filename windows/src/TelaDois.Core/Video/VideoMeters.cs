namespace TelaDois.Core.Video;

/// <summary>Quantas medidas, média e máximo (em ms) de uma duração num intervalo.</summary>
public readonly record struct DurationSummary(int Count, double AvgMs, double MaxMs);

/// <summary>O que o vídeo do PC fez num intervalo (ver <see cref="VideoMeters"/>).</summary>
public readonly record struct VideoMetersSnapshot(int Read, int Sent, long Bytes, long Dropped, DurationSummary Queue, DurationSummary Write);

/// <summary>
/// Medidores do vídeo do PC (diagnóstico, não mudam o vídeo): imagens lidas do FFmpeg, descartadas pela
/// <see cref="FrameQueue"/>, idade de cada imagem ao sair da fila, e a duração e o tamanho de cada escrita do VIDEO.
/// Seguro entre threads; <see cref="Take"/> devolve o intervalo e começa outro.
/// </summary>
public sealed class VideoMeters
{
    private readonly Lock _gate = new();
    private int _read;
    private int _sent;
    private long _bytes;
    private long _dropped;
    private Tally _queue;
    private Tally _write;

    public void FrameRead()
    {
        lock (_gate)
        {
            _read++;
        }
    }

    public void FramesDropped(long count)
    {
        lock (_gate)
        {
            _dropped += count;
        }
    }

    /// <summary>Quanto uma imagem esperou na fila até sair para o cabo.</summary>
    public void QueueAge(TimeSpan age)
    {
        lock (_gate)
        {
            _queue.Add(age);
        }
    }

    /// <summary>Uma mensagem VIDEO escrita: <paramref name="bytes"/> no cabo, em <paramref name="duration"/> (inclui a espera pela vez).</summary>
    public void FrameSent(int bytes, TimeSpan duration)
    {
        lock (_gate)
        {
            _sent++;
            _bytes += bytes;
            _write.Add(duration);
        }
    }

    public VideoMetersSnapshot Take()
    {
        lock (_gate)
        {
            var snapshot = new VideoMetersSnapshot(_read, _sent, _bytes, _dropped, _queue.Summary(), _write.Summary());
            _read = 0;
            _sent = 0;
            _bytes = 0;
            _dropped = 0;
            _queue = default;
            _write = default;
            return snapshot;
        }
    }

    private struct Tally
    {
        private int _count;
        private double _sumMs;
        private double _maxMs;

        public void Add(TimeSpan duration)
        {
            var ms = duration.TotalMilliseconds;
            _count++;
            _sumMs += ms;
            _maxMs = Math.Max(_maxMs, ms);
        }

        public readonly DurationSummary Summary() => _count == 0 ? default : new DurationSummary(_count, _sumMs / _count, _maxMs);
    }
}
