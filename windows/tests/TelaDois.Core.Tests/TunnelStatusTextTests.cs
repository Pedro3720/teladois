using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class TunnelStatusTextTests
{
    [Fact]
    public void ShowsMirroringWithMonitorSizeAndLatency()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4), new VideoConfig(1366, 768, 60), VideoKind.Mirror));
        Assert.Equal("TelaDois: duplicando · 1366×768 · 60 fps · 4 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ShowsExtendedWithinTheTooltipLimit()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2868, 1320), TimeSpan.FromMilliseconds(123), new VideoConfig(2868, 1320, 60), VideoKind.Extend));
        Assert.Equal("TelaDois: estendido · 2868×1320 · 60 fps · 123 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ConnectedWithoutVideoKeepsOldText() =>
        Assert.Equal("TelaDois: conectado · 2556×1179 · 4 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4))));
}
