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

    /// <summary>
    /// Só as amostras de atraso que valem: o <c>ddagrab</c> repete a imagem com a tela parada, e uma repetição casaria
    /// com um desenho cada vez mais velho. Fica só a 1ª imagem de cada código novo (diferente do último código lido) e as
    /// imagens ilegíveis (null = falta) depois do 1º código lido; a 1ª imagem lida nunca conta (a troca dela não foi vista).
    /// A troca logo depois de uma imagem ilegível também não conta: a ilegível pode ter sido a da troca (borrada), e a
    /// seguinte chegaria com 1 imagem a mais de atraso. As repetições continuam contando para imagens/s e buracos (isso
    /// usa as chegadas, não esta lista).
    /// </summary>
    public static IReadOnlyList<double?> FirstChangesOnly(IReadOnlyList<int?> decodedCodes, IReadOnlyList<double?> latencies)
    {
        var count = Math.Min(decodedCodes.Count, latencies.Count);
        return FirstChangeIndices(decodedCodes, count).Select(i => latencies[i]).ToList();
    }

    /// <summary>Os índices que <see cref="FirstChangesOnly"/> mantém, olhando só as <paramref name="count"/> primeiras imagens.</summary>
    public static IReadOnlyList<int> FirstChangeIndices(IReadOnlyList<int?> decodedCodes, int count)
    {
        var kept = new List<int>();
        int? last = null;
        var previousUnreadable = false;
        for (var i = 0; i < Math.Min(count, decodedCodes.Count); i++)
        {
            if (decodedCodes[i] is not { } code)
            {
                if (last is not null)
                {
                    kept.Add(i);
                }
                previousUnreadable = true;
                continue;
            }
            if (last is not null && code != last && !previousUnreadable)
            {
                kept.Add(i);
            }
            last = code;
            previousUnreadable = false;
        }
        return kept;
    }
}
