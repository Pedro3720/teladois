using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class BenchLayoutTests
{
    [Fact]
    public void MarkerAndScenesSitInTheTopLeftCorner()
    {
        Assert.Equal(new PixelRect(16, 16, 64, 64), BenchLayout.Marker);
        Assert.Equal(96, BenchLayout.SceneLeft);
        Assert.Equal(new PixelRect(96, 0, 852, 1179), BenchLayout.Scroll(2556, 1179));
        Assert.Equal(new PixelRect(96, 16, 1278, 589), BenchLayout.Noise(2556, 1179));
    }

    [Fact]
    public void ScaleIsProportionalAndStaysInsideTheImage()
    {
        // 2/3: o marcador cai em 11,11 com 43×43; a rolagem ocupa a altura toda
        Assert.Equal(new PixelRect(11, 11, 43, 43), BenchLayout.Scale(BenchLayout.Marker, 2556, 1179, 1704, 786));
        Assert.Equal(new PixelRect(64, 0, 568, 786), BenchLayout.Scale(BenchLayout.Scroll(2556, 1179), 2556, 1179, 1704, 786));
        // altura cortada para par: a rolagem não passa da última linha
        Assert.Equal(new PixelRect(96, 0, 852, 1178), BenchLayout.Scale(BenchLayout.Scroll(2556, 1179), 2556, 1179, 2556, 1178));
    }

    [Fact]
    public void CropInIsTheScaledMarker()
    {
        var m = BenchLayout.Scale(BenchLayout.Marker, 2556, 1179, 1920, 886);
        Assert.Equal((m.X, m.Y, m.Width, m.Height), MarkerCode.CropIn(2556, 1179, 1920, 886));
    }

    [Fact]
    public void ScrollPngShowsOnlyTheMarkerBesideTheScrollWindow()
    {
        var filter = BenchLayout.ScrollPngFilter(150, 1366, 768, 1366, 768);

        Assert.Equal(
            "[0:v]select=eq(n\\,150),format=rgb24,split=2[a][b];" +
            "[a]crop=64:64:16:16,pad=64:768:0:0:black[m];" +
            "[b]crop=455:768:96:0[s];" +
            "[m][s]hstack=inputs=2",
            filter);
    }
}
