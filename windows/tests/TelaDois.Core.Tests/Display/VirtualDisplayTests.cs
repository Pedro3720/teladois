using TelaDois.Core.Display;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests.Display;

public class VirtualDisplayTests
{
    private const string Virtual = @"\.\DISPLAY3";

    private sealed class NoPipe : IDriverPipe
    {
        public Task<bool> PingAsync(CancellationToken ct) => Task.FromResult(false);
    }

    /// <summary>Principal 1600×900 em (0,0), outro 1920×1080 à direita, e o do driver desligado.</summary>
    private static FakeDisplayApi TwoMonitorsAndDriver()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
        api.DeviceList.Add(new DisplayDeviceInfo(@"\.\DISPLAY2", "AMD Radeon(TM) Graphics", true, false));
        api.DeviceList.Add(new DisplayDeviceInfo(Virtual, "Virtual Display Driver", false, false));
        api.BoundsList[@"\.\DISPLAY1"] = new DisplayBounds(0, 0, 1600, 900);
        api.BoundsList[@"\.\DISPLAY2"] = new DisplayBounds(1600, 0, 1920, 1080);
        api.ModeList[Virtual] = [new(1920, 1080, 60), new(2556, 1179, 60), new(2622, 1206, 60)];
        return api;
    }

    [Fact]
    public void FindsTheDriverMonitorByAdapterName() =>
        Assert.Equal(Virtual, new VirtualDisplay(TwoMonitorsAndDriver(), new NoPipe()).Find());

    [Fact]
    public void AttachPutsTheMonitorRightOfTheRightmostOneWithTheIphoneMode()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());

        var (device, mode) = display.Attach(2556, 1179);

        Assert.Equal(Virtual, device);
        Assert.Equal(new DisplayModeInfo(2556, 1179, 60), mode);
        Assert.Equal((Virtual, 3520, 0, 2556, 1179, 60), Assert.Single(api.Applied));
        Assert.True(display.IsAttached(Virtual));
    }

    [Fact]
    public void AttachFailureCarriesTheWindowsCode()
    {
        var api = TwoMonitorsAndDriver();
        api.Result = -2; // DISP_CHANGE_BADMODE
        var error = Assert.Throws<DisplayChangeException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
        Assert.Equal(-2, error.Code);
    }

    [Fact]
    public void AttachWithoutTheDriverMonitorThrows()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
        Assert.Throws<InvalidOperationException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
    }

    [Fact]
    public void DetachSendsSizeZero()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());
        display.Attach(2556, 1179);
        display.Detach(Virtual);
        Assert.Equal((Virtual, 0, 0, 0, 0, 0), api.Applied.Last());
        Assert.False(display.IsAttached(Virtual));
    }

    [Fact]
    public void DetachLeftoverOnlyActsWhenTheMonitorIsOnTheDesktop()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());
        Assert.Null(display.DetachLeftover());
        Assert.Empty(api.Applied);

        display.Attach(2556, 1179);
        Assert.Equal(Virtual, display.DetachLeftover());
        Assert.False(display.IsAttached(Virtual));
    }

    [Fact]
    public void ChooseModePrefersExactSizeAt60Hz() =>
        Assert.Equal(new DisplayModeInfo(2556, 1179, 60),
            VirtualDisplay.ChooseMode([new(2556, 1179, 30), new(2556, 1179, 60), new(1920, 1080, 60)], 2556, 1179));

    [Fact]
    public void ChooseModeFallsBackToTheClosestAspectRatio() =>
        Assert.Equal(new DisplayModeInfo(2622, 1206, 60),
            VirtualDisplay.ChooseMode([new(1920, 1080, 60), new(2622, 1206, 60), new(3840, 2160, 60)], 2556, 1179));

    [Fact]
    public void ChooseModeWithoutModesIsNull() => Assert.Null(VirtualDisplay.ChooseMode([], 2556, 1179));
}
