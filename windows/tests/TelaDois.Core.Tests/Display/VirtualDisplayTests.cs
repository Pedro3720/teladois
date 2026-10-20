using TelaDois.Core.Display;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests.Display;

public class VirtualDisplayTests
{
    private const string Virtual = @"\\.\DISPLAY3";

    private sealed class NoPipe : IDriverPipe
    {
        public Task<bool> PingAsync(CancellationToken ct) => Task.FromResult(false);
    }

    /// <summary>Principal 1600×900 em (0,0), outro 1920×1080 à direita, e o do driver desligado.</summary>
    private static FakeDisplayApi TwoMonitorsAndDriver()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY2", "AMD Radeon(TM) Graphics", true, false));
        api.DeviceList.Add(new DisplayDeviceInfo(Virtual, "Virtual Display Driver", false, false));
        api.BoundsList[@"\\.\DISPLAY1"] = new DisplayBounds(0, 0, 1600, 900);
        api.BoundsList[@"\\.\DISPLAY2"] = new DisplayBounds(1600, 0, 1920, 1080);
        api.ModeList[Virtual] = [new(1920, 1080, 60), new(2556, 1179, 60), new(2622, 1206, 60)];
        return api;
    }

    /// <summary>Outro adaptador com o nome dentro do dele (ex.: de outro driver virtual) não é o monitor do TelaDois.</summary>
    [Fact]
    public void AdapterNameMustMatchExactly()
    {
        var api = new FakeDisplayApi();
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY1", "Intel(R) UHD Graphics", true, true));
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY2", "Other Virtual Display Driver", true, false));
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY5", "virtual display driver", false, false));
        api.ModeList[@"\\.\DISPLAY5"] = [new(2556, 1179, 60)];
        var display = new VirtualDisplay(api, new NoPipe());

        Assert.Equal(@"\\.\DISPLAY5", display.Find()); // ignora maiúsculas
        Assert.Null(display.DetachLeftover()); // o DISPLAY2 está ligado, mas não é o do driver
        display.Detach(@"\\.\DISPLAY2");
        Assert.Empty(api.Applied);
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
        api.DeviceList.Add(new DisplayDeviceInfo(@"\\.\DISPLAY1", "AMD Radeon(TM) Graphics", true, true));
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
    public void DetachIgnoresPhysicalMonitor()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());

        display.Detach(@"\\.\DISPLAY2");
        display.Detach(@"\\.\DISPLAY1");

        Assert.Empty(api.Applied);
        Assert.True(display.IsAttached(@"\\.\DISPLAY1"));
        Assert.True(display.IsAttached(@"\\.\DISPLAY2"));
    }

    [Fact]
    public void DetachWithAnUnknownNameDoesNothing()
    {
        var api = TwoMonitorsAndDriver();
        var display = new VirtualDisplay(api, new NoPipe());

        display.Detach(@"\\.\DISPLAY99");
        display.Detach("");

        Assert.Empty(api.Applied);
    }

    [Fact]
    public void AttachTouchesOnlyTheDriverMonitor()
    {
        var api = TwoMonitorsAndDriver();
        new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179);
        Assert.All(api.Applied, call => Assert.Equal(Virtual, call.Device));
    }

    [Fact]
    public void AttachAlignsTheTopWithTheRightmostMonitor()
    {
        var api = TwoMonitorsAndDriver();
        api.BoundsList[@"\\.\DISPLAY2"] = new DisplayBounds(1600, -180, 1920, 1080); // o da direita está mais alto
        new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179);
        Assert.Equal((Virtual, 3520, -180, 2556, 1179, 60), Assert.Single(api.Applied));
    }

    [Fact]
    public void RestartRequestHasItsOwnMessage()
    {
        var api = TwoMonitorsAndDriver();
        api.Result = 1; // DISP_CHANGE_RESTART
        var error = Assert.Throws<DisplayChangeException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
        Assert.Equal(1, error.Code);
        Assert.Contains("reinício", error.Message);
        Assert.DoesNotContain("não deixou", error.Message);
    }

    [Fact]
    public void RefusalMessageNamesTheAction()
    {
        var api = TwoMonitorsAndDriver();
        api.Result = -2;
        var error = Assert.Throws<DisplayChangeException>(() => new VirtualDisplay(api, new NoPipe()).Attach(2556, 1179));
        Assert.Contains("não deixou ligar", error.Message);
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

    /// <summary>Medido no driver 25.7.23: sem gravar no registro, ligar falha com DISP_CHANGE_FAILED (-1).</summary>
    [Fact]
    public void TheVirtualMonitorApiWritesTheRegistry() => Assert.True(Win32DisplayApi.ForVirtualMonitor().Persist);
}
