using System.Globalization;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>A linha do registro que resume, a cada segundo de vídeo, o lado do PC e o último STATS do iPhone.</summary>
public static class VideoStatsLine
{
    // Vírgula decimal como no Brasil, sem depender das culturas instaladas no PC.
    private static readonly NumberFormatInfo PtBr = new() { NumberDecimalSeparator = "," };

    /// <param name="pc">Medidores do PC no intervalo.</param>
    /// <param name="interval">Duração medida do intervalo (para o Mbit/s do PC; zero = sem taxa).</param>
    /// <param name="roundTrip">Latência do último PONG, se houver.</param>
    /// <param name="phone">Último STATS do iPhone recebido no intervalo, se houver (o iPhone manda 1 por segundo).</param>
    public static string Format(VideoMetersSnapshot pc, TimeSpan interval, TimeSpan? roundTrip, LinkStats? phone)
    {
        var seconds = interval.TotalSeconds;
        var pcRate = seconds > 0 ? pc.Bytes * 8 / seconds / 1e6 : 0;
        var rtt = roundTrip is { } r ? $"RTT {Ms(r.TotalMilliseconds)} ms" : "RTT sem medida";
        var line =
            $"vídeo/s: lidas {pc.Read} · enviadas {pc.Sent} · {Mbit(pcRate)} Mbit/s · " +
            $"fila méd {Ms(pc.Queue.AvgMs)} / máx {Ms(pc.Queue.MaxMs)} ms · " +
            $"escrita méd {Ms(pc.Write.AvgMs)} / máx {Ms(pc.Write.MaxMs)} ms · " +
            $"descartadas {pc.Dropped} · {rtt} | iPhone: ";
        if (phone is not { } p)
        {
            return line + "sem dados";
        }
        // O STATS cobre ~1 s do iPhone: bytes do intervalo dele viram Mbit/s direto.
        return line +
            $"recebidas {p.FramesReceived} · {Mbit(p.BytesReceived * 8 / 1e6)} Mbit/s · na tela {p.FramesEnqueued} · " +
            $"descartadas {p.FramesDropped} · falhas {p.RendererFailures} · " +
            $"fila principal méd {Ms(p.MainLagAvgMicros / 1000.0)} / máx {Ms(p.MainLagMaxMicros / 1000.0)} ms · " +
            $"receber→decodificar méd {Ms(p.ReceiveToEnqueueAvgMicros / 1000.0)} / máx {Ms(p.ReceiveToEnqueueMaxMicros / 1000.0)} ms";
    }

    private static string Ms(double ms) => ms.ToString("0", PtBr);

    private static string Mbit(double rate) => rate.ToString("0.0", PtBr);
}
