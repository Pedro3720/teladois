namespace TelaDois.LatencyBench;

/// <summary>Uma leitura do stdout do FFmpeg: até onde (bytes acumulados) e quando voltou (ticks do <c>Stopwatch</c>).</summary>
public sealed record ReadStamp(long EndOffset, long Ticks);

/// <summary>
/// Repassa as leituras de outro <see cref="Stream"/> e anota cada uma (<see cref="Reads"/>) e os bytes lidos
/// (<see cref="RawBytes"/>): permite saber quando chegou o último byte de cada imagem, antes do
/// <c>AccessUnitReader</c> entregá-la (ele espera o AUD da imagem seguinte).
/// </summary>
public sealed class TimestampedStream(Stream inner, Func<long> clock) : Stream
{
    private readonly List<ReadStamp> _reads = [];
    private readonly List<byte[]> _chunks = [];
    private long _position;

    public IReadOnlyList<ReadStamp> Reads => _reads;

    /// <summary>Tudo o que foi lido, na ordem.</summary>
    public byte[] RawBytes()
    {
        var all = new byte[_position];
        var at = 0;
        foreach (var chunk in _chunks)
        {
            chunk.CopyTo(all, at);
            at += chunk.Length;
        }
        return all;
    }

    private int Record(ReadOnlySpan<byte> read)
    {
        if (read.Length > 0)
        {
            var ticks = clock();
            _chunks.Add(read.ToArray());
            _position += read.Length;
            _reads.Add(new ReadStamp(_position, ticks));
        }
        return read.Length;
    }

    public override int Read(byte[] buffer, int offset, int count) => Record(buffer.AsSpan(offset, inner.Read(buffer, offset, count)));

    public override int Read(Span<byte> buffer)
    {
        var n = inner.Read(buffer);
        return Record(buffer[..n]);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await inner.ReadAsync(buffer, cancellationToken);
        return Record(buffer.Span[..n]);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

public static class ReaderTiming
{
    private const int Aud = 9;

    /// <summary>
    /// Fim (exclusivo, em bytes desde o começo do fluxo) de cada imagem que o <c>AccessUnitReader</c> entrega, pela mesma
    /// regra dele: a imagem vai de um AUD até o próximo (ou o fim do fluxo) e só conta se tiver fatia (NAL 1–5). O
    /// código de início de 4 bytes começa no zero extra.
    /// </summary>
    public static IReadOnlyList<long> UnitEnds(ReadOnlySpan<byte> data)
    {
        var ends = new List<long>();
        bool inUnit = false, hasSlice = false;
        for (var i = 0; i + 3 < data.Length; i++)
        {
            if (data[i] != 0 || data[i + 1] != 0 || data[i + 2] != 1)
            {
                continue;
            }
            var start = i > 0 && data[i - 1] == 0 ? i - 1 : i;
            var type = data[i + 3] & 0x1F;
            if (type == Aud)
            {
                if (inUnit && hasSlice)
                {
                    ends.Add(start);
                }
                inUnit = true;
                hasSlice = false;
            }
            else if (inUnit && type is >= 1 and <= 5)
            {
                hasSlice = true;
            }
            i += 2;
        }
        if (inUnit && hasSlice)
        {
            ends.Add(data.Length);
        }
        return ends;
    }

    /// <summary>
    /// Para cada imagem (fim exclusivo em <paramref name="unitEnds"/>), o instante da leitura que trouxe o último byte
    /// dela: a primeira com <see cref="ReadStamp.EndOffset"/> ≥ fim. Null se nenhuma leitura chegou lá.
    /// </summary>
    public static IReadOnlyList<long?> LastByteTicks(IReadOnlyList<ReadStamp> reads, IReadOnlyList<long> unitEnds)
    {
        var result = new long?[unitEnds.Count];
        var r = 0;
        for (var u = 0; u < unitEnds.Count; u++)
        {
            while (r < reads.Count && reads[r].EndOffset < unitEnds[u])
            {
                r++;
            }
            result[u] = r < reads.Count ? reads[r].Ticks : null;
        }
        return result;
    }
}
