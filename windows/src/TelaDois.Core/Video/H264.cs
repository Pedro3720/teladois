namespace TelaDois.Core.Video;

/// <summary>Uma imagem H.264 em Annex-B; <c>IsKeyframe</c> quando contém um NAL IDR.</summary>
public sealed record AccessUnit(byte[] Data, bool IsKeyframe);

internal static class H264
{
    public const int Idr = 5;
    public const int Aud = 9;

    /// <summary>Posição de cada código de início (contando o zero extra do código de 4 bytes) e o tipo do NAL.</summary>
    public static List<(int Start, int Type)> ScanNals(byte[] data, int count)
    {
        var nals = new List<(int, int)>();
        for (var i = 0; i + 3 < count; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1)
            {
                var start = i > 0 && data[i - 1] == 0 ? i - 1 : i;
                nals.Add((start, data[i + 3] & 0x1F));
                i += 2;
            }
        }
        return nals;
    }
}
