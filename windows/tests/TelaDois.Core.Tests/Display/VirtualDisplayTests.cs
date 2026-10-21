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

    // ---- Ligar/desligar o dispositivo do driver (Etapa 5c) ----

    private static readonly DisplayDeviceInfo DriverDevice = new(Virtual, "Virtual Display Driver", false, false);

    /// <summary>Só os dois monitores físicos: o dispositivo do driver está desativado e o Windows não o lista.</summary>
    private static FakeDisplayApi TwoMonitorsDriverDisabled()
    {
        var api = TwoMonitorsAndDriver();
        api.DeviceList.RemoveAll(d => d.DeviceName == Virtual);
        return api;
    }

    /// <summary>Depois de <paramref name="delayMs"/>, o Windows passa a listar (ou deixa de listar) o monitor do driver.</summary>
    private static void ChangeDriverPresenceLater(FakeDisplayApi api, bool present, int delayMs) =>
        _ = Task.Delay(delayMs).ContinueWith(_ =>
        {
            lock (api.DeviceList)
            {
                api.DeviceList.RemoveAll(d => d.DeviceName == Virtual);
                if (present)
                {
                    api.DeviceList.Add(DriverDevice);
                }
            }
        }, TaskScheduler.Default);

    private static (VirtualDisplay Display, List<string> Log) Create(FakeDisplayApi api, IDeviceSwitch? device,
        TimeSpan? powerOnWait = null, TimeSpan? powerOffWait = null)
    {
        var log = new List<string>();
        var display = new VirtualDisplay(api, new NoPipe(), device, line => { lock (log) { log.Add(line); } })
        {
            PowerOnWait = powerOnWait ?? TimeSpan.FromSeconds(8),
            PowerOffWait = powerOffWait ?? TimeSpan.FromSeconds(5),
            PollInterval = TimeSpan.FromMilliseconds(20),
        };
        return (display, log);
    }

    [Fact]
    public void PowerWaitsMatchTheSpec()
    {
        var display = new VirtualDisplay(new FakeDisplayApi(), new NoPipe());
        Assert.Equal(TimeSpan.FromSeconds(20), display.PowerOnWait);
        Assert.Equal(TimeSpan.FromSeconds(5), display.PowerOffWait);
        Assert.Equal(TimeSpan.FromMilliseconds(100), display.PollInterval);
    }

    [Fact]
    public async Task PowerOnWithTheMonitorAlreadyPresentDoesNotRunTheTask()
    {
        var api = TwoMonitorsAndDriver();
        var device = new FakeDeviceSwitch();
        var (display, _) = Create(api, device);

        Assert.True(await display.PowerOnAsync(CancellationToken.None));

        Assert.Empty(device.Runs);
        Assert.Empty(api.Applied);
    }

    [Fact]
    public async Task PowerOnRunsTheTaskAndWaitsForTheMonitor()
    {
        var api = TwoMonitorsDriverDisabled();
        var device = new FakeDeviceSwitch { OnRun = enable => ChangeDriverPresenceLater(api, enable, 300) };
        var (display, log) = Create(api, device);

        Assert.True(await display.PowerOnAsync(CancellationToken.None));

        Assert.Equal(new[] { true }, device.Runs.ToArray());
        Assert.Equal(Virtual, display.Find());
        Assert.Contains(log, l => l.Contains("monitor virtual ativado em") && l.Contains("ms"));
        Assert.Empty(api.Applied); // ligar o dispositivo não mexe em nenhum monitor
    }

    [Fact]
    public async Task PowerOnGivesUpWhenTheMonitorNeverAppears()
    {
        var api = TwoMonitorsDriverDisabled();
        var device = new FakeDeviceSwitch();
        var (display, log) = Create(api, device, powerOnWait: TimeSpan.FromMilliseconds(500));

        Assert.False(await display.PowerOnAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(new[] { true }, device.Runs.ToArray());
        Assert.Contains(log, l => l.Contains("não apareceu"));
        Assert.Empty(api.Applied);
    }

    [Fact]
    public async Task PowerOnWhenTheTaskIsRefusedIsFalseWithoutWaiting()
    {
        var api = TwoMonitorsDriverDisabled();
        var device = new FakeDeviceSwitch { Accepts = false };
        var (display, _) = Create(api, device); // espera de 8 s: recusar não pode esperar

        Assert.False(await display.PowerOnAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(new[] { true }, device.Runs.ToArray());
    }

    [Fact]
    public async Task PowerOnWithoutTheSwitchAndWithoutTheMonitorIsFalse()
    {
        var api = TwoMonitorsDriverDisabled();
        var (display, log) = Create(api, device: null);

        Assert.False(await display.PowerOnAsync(CancellationToken.None));
        Assert.NotEmpty(log);
        Assert.Empty(api.Applied);
    }

    [Fact]
    public async Task PowerOnWithTheTasksMissingIsFalseWithoutRunning()
    {
        var api = TwoMonitorsDriverDisabled();
        var device = new FakeDeviceSwitch { Available = false };
        var (display, _) = Create(api, device);

        Assert.False(await display.PowerOnAsync(CancellationToken.None));
        Assert.Empty(device.Runs);
    }

    [Fact]
    public async Task PowerOffRunsTheTaskAndWaitsForTheMonitorToGo()
    {
        var api = TwoMonitorsAndDriver();
        var device = new FakeDeviceSwitch { OnRun = enable => ChangeDriverPresenceLater(api, enable, 300) };
        var (display, log) = Create(api, device);

        await display.PowerOffAsync(CancellationToken.None);

        Assert.Equal(new[] { false }, device.Runs.ToArray());
        Assert.Null(display.Find());
        Assert.Contains(log, l => l.Contains("monitor virtual desativado em"));
        Assert.Empty(api.Applied); // desligar o dispositivo nunca mexe num monitor físico
    }

    [Fact]
    public async Task PowerOffThatDoesNotTakeLogsAndReturns()
    {
        var api = TwoMonitorsAndDriver();
        var device = new FakeDeviceSwitch();
        var (display, log) = Create(api, device, powerOffWait: TimeSpan.FromMilliseconds(300));

        await display.PowerOffAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { false }, device.Runs.ToArray());
        Assert.Contains(log, l => l.Contains("não desativou"));
    }

    [Fact]
    public async Task PowerOffWithTheTasksMissingRunsNothing()
    {
        var api = TwoMonitorsAndDriver();
        var device = new FakeDeviceSwitch { Available = false };
        var (display, _) = Create(api, device);

        await display.PowerOffAsync(CancellationToken.None);
        await Create(api, device: null).Display.PowerOffAsync(CancellationToken.None);

        Assert.Empty(device.Runs);
        Assert.Equal(Virtual, display.Find());
        Assert.Empty(api.Applied);
    }

    [Fact]
    public async Task CanPowerOffOnlyWithTheTasks()
    {
        var api = TwoMonitorsAndDriver();
        Assert.True(await Create(api, new FakeDeviceSwitch()).Display.CanPowerOffAsync(CancellationToken.None));
        Assert.False(await Create(api, new FakeDeviceSwitch { Available = false }).Display.CanPowerOffAsync(CancellationToken.None));
        Assert.False(await Create(api, device: null).Display.CanPowerOffAsync(CancellationToken.None));
    }

    [Fact]
    public async Task PowerOnHonoursTheCallerCancellation()
    {
        var api = TwoMonitorsDriverDisabled();
        var (display, _) = Create(api, new FakeDeviceSwitch());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => display.PowerOnAsync(cts.Token));
    }

    [Fact]
    public async Task PowerOnDuringAPowerOffWaitsForItAndThenRunsTheTask()
    {
        // Um feed antigo ainda desativando: o ligar do feed novo não pode achar o monitor "presente" e seguir sem a tarefa,
        // senão a desativação atrasada tira o monitor dele logo depois do Attach.
        var api = TwoMonitorsAndDriver();
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var device = new FakeDeviceSwitch { HoldDisable = hold.Task, OnRun = enable => ChangeDriverPresenceLater(api, enable, 50) };
        var (display, _) = Create(api, device);

        var off = display.PowerOffAsync(CancellationToken.None);
        await WaitForRunsAsync(device, 1);
        var on = display.PowerOnAsync(CancellationToken.None);
        await Task.Delay(200);
        Assert.False(on.IsCompleted);
        Assert.Equal(new[] { false }, device.Runs.ToArray());

        hold.SetResult();
        await off.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(await on.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(new[] { false, true }, device.Runs.ToArray());
        Assert.Equal(Virtual, display.Find());
    }

    [Fact]
    public async Task PowerOnWaitingForAPowerOffHonoursTheCallerCancellation()
    {
        var api = TwoMonitorsAndDriver();
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var device = new FakeDeviceSwitch { HoldDisable = hold.Task, OnRun = enable => ChangeDriverPresenceLater(api, enable, 0) };
        var (display, _) = Create(api, device);
        using var cts = new CancellationTokenSource();

        var off = display.PowerOffAsync(CancellationToken.None);
        await WaitForRunsAsync(device, 1);
        var on = display.PowerOnAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => on.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(off.IsCompleted); // o desligar não é cancelado por quem liga
        hold.SetResult();
        await off.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { false }, device.Runs.ToArray());
    }

    [Fact]
    public async Task PowerOffDuringAPowerOnWaitsForIt()
    {
        var api = TwoMonitorsDriverDisabled();
        var device = new FakeDeviceSwitch { OnRun = enable => ChangeDriverPresenceLater(api, enable, 300) };
        var (display, _) = Create(api, device);

        var on = display.PowerOnAsync(CancellationToken.None);
        await WaitForRunsAsync(device, 1);
        var off = display.PowerOffAsync(CancellationToken.None);

        Assert.True(await on.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(off.IsCompleted);
        await off.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { true, false }, device.Runs.ToArray());
        Assert.Null(display.Find());
    }

    private static async Task WaitForRunsAsync(FakeDeviceSwitch device, int count)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (device.Runs.Count < count)
        {
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), "a tarefa do Windows não foi disparada");
            await Task.Delay(10);
        }
    }
}
