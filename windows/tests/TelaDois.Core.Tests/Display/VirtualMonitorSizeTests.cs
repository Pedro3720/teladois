using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

public class VirtualMonitorSizeTests
{
    /// <summary>A tabela do spec da 5d (§7): resolução do iPhone em paisagem ÷ 1,75, cada lado no par mais próximo.</summary>
    [Theory]
    [InlineData(2556, 1179, 1460, 674)]
    [InlineData(2796, 1290, 1598, 738)]
    [InlineData(2622, 1206, 1498, 690)]
    [InlineData(2868, 1320, 1638, 754)]
    [InlineData(2736, 1260, 1564, 720)]
    public void DividesTheIphoneResolutionByOnePointSevenFiveToTheNearestEven(int width, int height, int expectedWidth, int expectedHeight)
    {
        Assert.Equal((expectedWidth, expectedHeight), VirtualMonitorSize.For(width, height));
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1179, 2556)]
    [InlineData(1001, 999)]
    [InlineData(7, 3)]
    public void AlwaysGivesEvenSides(int width, int height)
    {
        var (w, h) = VirtualMonitorSize.For(width, height);

        Assert.Equal(0, w % 2);
        Assert.Equal(0, h % 2);
        Assert.InRange(Math.Abs(w - width / 1.75), 0, 1);
        Assert.InRange(Math.Abs(h - height / 1.75), 0, 1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-5, 3)]
    public void NeverGivesLessThanTwoPixels(int width, int height)
    {
        var (w, h) = VirtualMonitorSize.For(width, height);

        Assert.True(w >= 2 && h >= 2);
    }
}
