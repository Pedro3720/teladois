using System.Runtime.CompilerServices;

namespace TelaDois.Core.Video;

/// <summary>
/// Corta em imagens o H.264 que sai do FFmpeg. Dois formatos, decididos pelo 1º byte:
/// <list type="bullet">
/// <item><b>Emoldurado</b> (o do app, ver <see cref="FfmpegArguments"/>): antes de cada pacote o FFmpeg escreve a linha
/// <c>"TD &lt;tamanho&gt;\n"</c> (<c>-stats_mux_pre pipe:1</c>), então a imagem sai assim que o último byte dela chega. O
/// pacote é exatamente o que o modo cru entregaria (AUD inserido pelo <c>h264_metadata</c> + NALs).</item>
/// <item><b>Cru</b> (Annex B, começa por 0x00): pelos AUD; uma imagem só sai quando o próximo AUD chega (ou o fluxo
/// termina).</item>
/// </list>
/// Nos dois, imagens sem fatias são descartadas e toda imagem com IDR sai com SPS e PPS (os últimos vistos são
/// reinseridos logo depois do AUD: o <c>h264_amf</c> só os manda no começo). No emoldurado, um pacote cortado pelo fim do
/// fluxo é descartado e uma moldura inválida lança <see cref="InvalidDataException"/>.
/// </summary>
public sealed class AccessUnitReader(Stream stream)
{
    /// <summary>Começo da linha que o FFmpeg escreve antes de cada pacote (<c>-stats_mux_pre_fmt "TD {size}"</c>).</summary>
    public const string FramePrefix = "TD ";

    /// <summary>"TD " + até 10 dígitos + '\n' cabem folgados.</summary>
    private const int MaxHeaderLength = 24;

    private const int MaxFrameSize = 64 * 1024 * 1024;

    private static readonly byte[] StartCode3 = [0, 0, 1];

    private static readonly byte[] StartCode4 = [0, 0, 0, 1];

    private static readonly byte[] Prefix = System.Text.Encoding.ASCII.GetBytes(FramePrefix);

    private byte[] _buffer = new byte[256 * 1024];
    private int _count;
    private int _need;
    private byte[]? _sps;
    private byte[]? _pps;

    public async IAsyncEnumerable<AccessUnit> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        bool? framed = null;
        while (true)
        {
            if (framed is { } isFramed)
            {
                foreach (var unit in isFramed ? TakeFramed() : TakeComplete(endOfStream: false))
                {
                    yield return unit;
                }
            }
            if (_count == _buffer.Length || _need > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _need));
            }
            var read = await stream.ReadAsync(_buffer.AsMemory(_count), ct);
            if (read == 0)
            {
                if (framed != true)
                {
                    foreach (var unit in TakeComplete(endOfStream: true))
                    {
                        yield return unit;
                    }
                }
                yield break;
            }
            _count += read;
            framed ??= _buffer[0] == (byte)FramePrefix[0];
        }
    }

    /// <summary>Tira do buffer cada moldura completa (linha + pacote); guarda em <c>_need</c> o tamanho da incompleta.</summary>
    private List<AccessUnit> TakeFramed()
    {
        var units = new List<AccessUnit>();
        var at = 0;
        _need = 0;
        while (true)
        {
            var span = _buffer.AsSpan(at, _count - at);
            var prefix = Math.Min(span.Length, FramePrefix.Length);
            if (!span[..prefix].SequenceEqual(Prefix.AsSpan(0, prefix)))
            {
                throw new InvalidDataException("vídeo do FFmpeg: esperava a linha \"TD <tamanho>\" antes do pacote");
            }
            var newline = span[..Math.Min(span.Length, MaxHeaderLength)].IndexOf((byte)'\n');
            if (newline < 0)
            {
                if (span.Length >= MaxHeaderLength)
                {
                    throw new InvalidDataException("vídeo do FFmpeg: linha do pacote sem fim");
                }
                break;
            }
            var size = ParseSize(span[FramePrefix.Length..newline]);
            var total = newline + 1 + size;
            if (span.Length < total)
            {
                _need = total;
                break;
            }
            var packet = span.Slice(newline + 1, size).ToArray();
            if (!packet.AsSpan().StartsWith(StartCode3) && !packet.AsSpan().StartsWith(StartCode4))
            {
                throw new InvalidDataException("vídeo do FFmpeg: o pacote não começa por um código de início do H.264");
            }
            if (H264.ScanNals(packet, packet.Length).Any(n => n.Type is >= 1 and <= 5))
            {
                units.Add(Complete(packet));
            }
            at += total;
        }
        if (at > 0)
        {
            Buffer.BlockCopy(_buffer, at, _buffer, 0, _count - at);
            _count -= at;
        }
        return units;
    }

    private static int ParseSize(ReadOnlySpan<byte> digits)
    {
        if (digits.Length is 0 or > 10)
        {
            throw new InvalidDataException("vídeo do FFmpeg: tamanho do pacote inválido");
        }
        long size = 0;
        foreach (var d in digits)
        {
            if (d is < (byte)'0' or > (byte)'9')
            {
                throw new InvalidDataException("vídeo do FFmpeg: tamanho do pacote inválido");
            }
            size = size * 10 + (d - '0');
        }
        if (size is <= 0 or > MaxFrameSize)
        {
            throw new InvalidDataException($"vídeo do FFmpeg: tamanho do pacote fora do limite ({size})");
        }
        return (int)size;
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
