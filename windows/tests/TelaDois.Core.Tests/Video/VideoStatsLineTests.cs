using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VideoStatsLineTests
{
    private static readonly VideoMetersSnapshot Pc = new(
        Read: 60, Sent: 60, Bytes: 387_500, Dropped: 0,
        Queue: new DurationSummary(60, 4.2, 38.4), Write: new DurationSummary(60, 1.1, 25));

    private static readonly LinkStats Phone = new(
        FramesReceived: 60, FramesEnqueued: 60, FramesDropped: 0, BytesReceived: 387_500,
        MainLagAvgMicros: 2_000, MainLagMaxMicros: 40_000, ReceiveToEnqueueAvgMicros: 1_000, ReceiveToEnqueueMaxMicros: 9_000,
        RendererFailures: 0);

    [Fact]
    public void FormatsBothSidesInOneLine() =>
        Assert.Equal(
            "vídeo/s: lidas 60 · enviadas 60 · 3,1 Mbit/s · fila méd 4 / máx 38 ms · escrita méd 1 / máx 25 ms · descartadas 0 · RTT 12 ms"
            + " | iPhone: recebidas 60 · 3,1 Mbit/s · na tela 60 · descartadas 0 · falhas 0 · fila principal méd 2 / máx 40 ms · receber→decodificar méd 1 / máx 9 ms",
            VideoStatsLine.Format(Pc, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(12), Phone));

    [Fact]
    public void SaysWhenThePhoneSentNothing() =>
        Assert.EndsWith(" | iPhone: sem dados", VideoStatsLine.Format(Pc, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(12), null));

    [Fact]
    public void RateUsesTheMeasuredIntervalAndRoundTripMayBeMissing()
    {
        var line = VideoStatsLine.Format(Pc, TimeSpan.FromSeconds(2), null, null);
        Assert.Contains(" · 1,6 Mbit/s · ", line);
        Assert.Contains(" · RTT sem medida | ", line);
    }

    [Fact]
    public void AnEmptyIntervalStillFormats() =>
        Assert.StartsWith(
            "vídeo/s: lidas 0 · enviadas 0 · 0,0 Mbit/s · fila méd 0 / máx 0 ms · escrita méd 0 / máx 0 ms · descartadas 0 · RTT 5 ms",
            VideoStatsLine.Format(new VideoMetersSnapshot(0, 0, 0, 0, default, default), TimeSpan.Zero, TimeSpan.FromMilliseconds(5), null));
}
