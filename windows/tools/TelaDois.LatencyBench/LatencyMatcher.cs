namespace TelaDois.LatencyBench;

/// <summary>Um desenho do marcador na tela: código e instante (ticks do <c>Stopwatch</c>).</summary>
public sealed record DrawEvent(int Code, long Ticks);

/// <summary>Uma imagem H.264 que chegou do FFmpeg: instante, se é completa e quantos bytes.</summary>
public sealed record Arrival(long Ticks, bool Keyframe, int Bytes);

public static class LatencyMatcher
{
    /// <summary>
    /// Para cada imagem <c>i</c> (chegada <c>i</c> ↔ código decodificado <c>i</c>) acha o último desenho com aquele
    /// código que veio antes ou junto da chegada e devolve o atraso em ms; null se o código for null ou não houver desenho.
    /// </summary>
    public static IReadOnlyList<double?> Match(
        IReadOnlyList<DrawEvent> draws,
        IReadOnlyList<Arrival> arrivals,
        IReadOnlyList<int?> decodedCodes,
        long ticksPerSecond)
    {
        var result = new double?[arrivals.Count];
        for (var i = 0; i < arrivals.Count; i++)
        {
            if (i >= decodedCodes.Count || decodedCodes[i] is not { } code)
            {
                continue;
            }

            var arrival = arrivals[i].Ticks;
            long? best = null;
            foreach (var draw in draws)
            {
                if (draw.Code == code && draw.Ticks <= arrival && (best is null || draw.Ticks > best))
                {
                    best = draw.Ticks;
                }
            }

            if (best is { } drawTicks)
            {
                result[i] = (arrival - drawTicks) * 1000.0 / ticksPerSecond;
            }
        }
        return result;
    }
}
