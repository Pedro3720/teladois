using TelaDois.Core.Link;

namespace TelaDois.Core.Tests;

public class TunnelStatusTextTests
{
    [Fact]
    public void ShowsStreamingWithMonitorSizeAndLatency()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4), new VideoConfig(1366, 768, 60)));
        Assert.Equal("TelaDois: transmitindo · 1366×768 · 60 fps · 4 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ConnectedWithoutVideoKeepsOldText() =>
        Assert.Equal("TelaDois: conectado · 2556×1179 · 4 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4))));
}
