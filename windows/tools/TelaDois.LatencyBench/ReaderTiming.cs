namespace TelaDois.LatencyBench;

/// <summary>Uma leitura do stdout do FFmpeg: até onde (bytes acumulados) e quando voltou (ticks do <c>Stopwatch</c>).</summary>
public sealed record ReadStamp(long EndOffset, long Ticks);

/// <summary>
/// Repassa as leituras de outro <see cref="Stream"/> e anota cada uma (<see cref="Reads"/>) e os bytes lidos
/// (<see cref="RawBytes"/>): permite saber quando chegou o último byte de cada imagem e comparar com quando o
/// <c>AccessUnitReader</c> a entregou (no fluxo emoldurado do app, no mesmo instante; no cru, só no AUD da seguinte).
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
    /// regra dele: no fluxo emoldurado (começa por "TD "), o fim do pacote; no cru, de um AUD até o próximo (ou o fim do
    /// fluxo). Só conta imagem com fatia (NAL 1–5). O código de início de 4 bytes começa no zero extra.
    /// </summary>
    public static IReadOnlyList<long> UnitEnds(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith("TD "u8))
        {
            return FramedUnitEnds(data);
        }
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
    /// Fluxo emoldurado ("TD &lt;tamanho&gt;" + quebra de linha + pacote, ver <c>FfmpegArguments</c>): a imagem termina no
    /// último byte do pacote e só conta se tiver fatia; um pacote cortado pelo fim (ou uma moldura inválida) encerra a
    /// lista, como no <c>AccessUnitReader</c>.
    /// </summary>
    private static List<long> FramedUnitEnds(ReadOnlySpan<byte> data)
    {
        var ends = new List<long>();
        var at = 0;
        while (at < data.Length && data[at..].StartsWith("TD "u8))
        {
            var newline = data[at..].IndexOf((byte)'\n');
            if (newline < 0 || !int.TryParse(data.Slice(at + 3, newline - 3), System.Globalization.NumberStyles.None, null, out var size))
            {
                break;
            }
            var packet = at + newline + 1;
            if (packet + size > data.Length)
            {
                break;
            }
            if (HasSlice(data.Slice(packet, size)))
            {
                ends.Add(packet + size);
            }
            at = packet + size;
        }
        return ends;
    }

    private static bool HasSlice(ReadOnlySpan<byte> packet)
    {
        for (var i = 0; i + 3 < packet.Length; i++)
        {
            if (packet[i] == 0 && packet[i + 1] == 0 && packet[i + 2] == 1)
            {
                if ((packet[i + 3] & 0x1F) is >= 1 and <= 5)
                {
                    return true;
                }
                i += 2;
            }
        }
        return false;
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
