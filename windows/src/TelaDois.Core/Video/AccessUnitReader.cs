using System.Runtime.CompilerServices;

namespace TelaDois.Core.Video;

/// <summary>
/// Corta o H.264 que sai do FFmpeg em imagens pelos AUD (inseridos com <c>h264_metadata=aud=insert</c>).
/// Uma imagem só sai quando o próximo AUD chega (ou o fluxo termina); imagens sem fatias são descartadas.
/// Toda imagem com IDR sai com SPS e PPS (os últimos vistos são reinseridos logo depois do AUD: o <c>h264_amf</c> só os manda no começo).
/// </summary>
public sealed class AccessUnitReader(Stream stream)
{
    private byte[] _buffer = new byte[256 * 1024];
    private int _count;
    private byte[]? _sps;
    private byte[]? _pps;

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
                units.Add(Complete(_buffer.AsSpan(from, to - from).ToArray()));
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

    /// <summary>Guarda o SPS/PPS vistos e reinsere, logo depois do AUD, os que faltarem numa imagem com IDR.</summary>
    private AccessUnit Complete(byte[] data)
    {
        var nals = H264.ScanNals(data, data.Length);
        bool hasSps = false, hasPps = false, idr = false;
        for (var i = 0; i < nals.Count; i++)
        {
            var end = i + 1 < nals.Count ? nals[i + 1].Start : data.Length;
            switch (nals[i].Type)
            {
                case H264.Sps:
                    _sps = data[nals[i].Start..end];
                    hasSps = true;
                    break;
                case H264.Pps:
                    _pps = data[nals[i].Start..end];
                    hasPps = true;
                    break;
                case H264.Idr:
                    idr = true;
                    break;
            }
        }
        if (idr && (!hasSps || !hasPps) && _sps is not null && _pps is not null)
        {
            var at = nals.Count > 1 ? nals[1].Start : data.Length;
            data = [.. data[..at], .. hasSps ? Array.Empty<byte>() : _sps, .. hasPps ? Array.Empty<byte>() : _pps, .. data[at..]];
        }
        return new AccessUnit(data, idr);
    }
}
