using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class DisplayCatalogTests
{
    private static readonly DisplayOutput Main = new(0, @"\\.\DISPLAY2", 1920, 1080, true);
    private static readonly DisplayOutput Side = new(1, @"\\.\DISPLAY1", 1366, 768, false);

    [Fact]
    public void PrefersSavedMonitor() => Assert.Equal(Main, DisplayCatalog.Choose([Main, Side], @"\\.\DISPLAY2"));

    [Fact]
    public void DefaultsToFirstNonPrimary() => Assert.Equal(Side, DisplayCatalog.Choose([Main, Side], null));

    [Fact]
    public void FallsBackWhenSavedMonitorIsGone() => Assert.Equal(Side, DisplayCatalog.Choose([Main, Side], @"\\.\DISPLAY9"));

    [Fact]
    public void UsesPrimaryWhenItIsTheOnlyOne() => Assert.Equal(Main, DisplayCatalog.Choose([Main], null));

    [Fact]
    public void NoMonitorsGivesNull() => Assert.Null(DisplayCatalog.Choose([], null));

    [Fact]
    public void EnumerateDoesNotThrowAndReportsSaneMonitors()
    {
        // No runner do CI pode não haver monitor; no PC real aparece pelo menos o principal.
        var outputs = DisplayCatalog.Enumerate();
        Assert.All(outputs, o =>
        {
            Assert.StartsWith(@"\\.\DISPLAY", o.DeviceName);
            Assert.True(o.Width > 0 && o.Height > 0);
        });
        Assert.Equal(outputs.Count, outputs.Select(o => o.OutputIndex).Distinct().Count());
    }
}
