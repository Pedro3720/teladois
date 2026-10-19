using System.Runtime.CompilerServices;

namespace TelaDois.Core.Video;

/// <summary>
/// Corta o H.264 que sai do FFmpeg em imagens pelos AUD (inseridos com <c>h264_metadata=aud=insert</c>).
/// Uma imagem só sai quando o próximo AUD chega (ou o fluxo termina); imagens sem fatias são descartadas.
/// </summary>
public sealed class AccessUnitReader(Stream stream)
{
    private byte[] _buffer = new byte[256 * 1024];
    private int _count;

    public async IAsyncEnumerable<AccessUnit> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (true)
        {
            foreach (var unit in TakeComplete(endOfStream: false))
            {
                yield return unit;
            }
            if (_count == _buffer.Length)
            {
                Array.Resize(ref _buffer, _buffer.Length * 2);
            }
            var read = await stream.ReadAsync(_buffer.AsMemory(_count), ct);
            if (read == 0)
            {
                foreach (var unit in TakeComplete(endOfStream: true))
                {
                    yield return unit;
                }
                yield break;
            }
            _count += read;
        }
    }

    private List<AccessUnit> TakeComplete(bool endOfStream)
    {
        var units = new List<AccessUnit>();
        var nals = H264.ScanNals(_buffer, _count);
        var bounds = nals.Where(n => n.Type == H264.Aud).Select(n => n.Start).ToList();
        if (endOfStream && bounds.Count > 0)
        {
            bounds.Add(_count);
        }
        var consumed = 0;
        for (var k = 0; k + 1 < bounds.Count; k++)
        {
            var (from, to) = (bounds[k], bounds[k + 1]);
            var types = nals.Where(n => n.Start >= from && n.Start < to).Select(n => n.Type).ToList();
            if (types.Any(t => t is >= 1 and <= 5))
            {
                units.Add(new AccessUnit(_buffer.AsSpan(from, to - from).ToArray(), types.Contains(H264.Idr)));
            }
            consumed = to;
        }
        if (consumed > 0)
        {
            Buffer.BlockCopy(_buffer, consumed, _buffer, 0, _count - consumed);
            _count -= consumed;
        }
        return units;
    }
}
