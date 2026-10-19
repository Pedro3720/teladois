namespace TelaDois.Core.Video;

/// <summary>
/// Separa o H.264 Annex-B do FFmpeg em access units (um quadro cada), cortando antes de cada AUD.
/// Um AUD sem fatia (o AMF duplica quando se pede outro) é juntado ao quadro seguinte.
/// </summary>
public sealed class H264AccessUnitSplitter
{
    private const byte AudType = 9;
    private readonly List<byte> _buffer = [];
    private int _scanFrom = 1;

    public IReadOnlyList<byte[]> Append(ReadOnlySpan<byte> data)
    {
        _buffer.AddRange(data);
        var units = new List<byte[]>();
        while (true)
        {
            var next = FindAud(_scanFrom);
            if (next < 0)
            {
                // Volta 4 bytes: um código de início pode estar cortado entre dois pedaços.
                _scanFrom = Math.Max(_scanFrom, Math.Max(1, _buffer.Count - 4));
                return units;
            }
            if (HasSlice(0, next))
            {
                units.Add(_buffer.GetRange(0, next).ToArray());
                _buffer.RemoveRange(0, next);
                _scanFrom = 1;
            }
            else
            {
                _scanFrom = next + 4;
            }
        }
    }

    /// <summary>Início (incluindo o zero extra do código de 4 bytes) do próximo AUD a partir de <paramref name="from"/>.</summary>
    private int FindAud(int from)
    {
        for (var i = from; i + 3 < _buffer.Count; i++)
        {
            if (_buffer[i] == 0 && _buffer[i + 1] == 0 && _buffer[i + 2] == 1 && (_buffer[i + 3] & 0x1F) == AudType)
            {
                var start = i > 0 && _buffer[i - 1] == 0 ? i - 1 : i;
                if (start > 0)
                {
                    return start;
                }
            }
        }
        return -1;
    }

    private bool HasSlice(int start, int end)
    {
        for (var i = start; i + 3 < end; i++)
        {
            if (_buffer[i] == 0 && _buffer[i + 1] == 0 && _buffer[i + 2] == 1 && (_buffer[i + 3] & 0x1F) is 1 or 5)
            {
                return true;
            }
        }
        return false;
    }
}
