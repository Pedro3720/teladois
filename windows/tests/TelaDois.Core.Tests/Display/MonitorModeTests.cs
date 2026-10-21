using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

public class MonitorModeTests
{
    [Fact]
    public void StartsDisconnectedAndOff()
    {
        var mode = new MonitorMode();
        Assert.False(mode.IsConnected);
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void SessionTurnsOnByItselfAndEndTurnsOff()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        Assert.True(mode.IsConnected);
        Assert.True(mode.WantsVideo);
        mode.SessionEnded();
        Assert.False(mode.IsConnected);
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void ToggleWithoutPhoneChangesNothing()
    {
        var mode = new MonitorMode();
        Assert.Equal(ToggleResult.NoPhone, mode.Toggle());
        Assert.False(mode.WantsVideo);
    }

    [Fact]
    public void ToggleAlternatesWhileConnected()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        Assert.False(mode.WantsVideo);
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        Assert.True(mode.WantsVideo);
    }

    [Fact]
    public void VideoEndedTurnsOffButKeepsThePhone()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        mode.VideoEnded();
        Assert.True(mode.IsConnected);
        Assert.False(mode.WantsVideo);
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
    }

    [Fact]
    public void StateChangedFiresOnlyOnRealChanges()
    {
        var mode = new MonitorMode();
        var events = new List<(bool, bool)>();
        mode.StateChanged += (connected, on) => events.Add((connected, on));
        mode.SessionStarted();
        mode.SessionStarted();
        mode.VideoEnded();
        mode.VideoEnded();
        mode.SessionEnded();
        Assert.Equal(new List<(bool, bool)> { (true, true), (true, false), (false, false) }, events);
    }

    [Fact]
    public async Task WaitForChangeReturnsAtOnceWhenTheStateIsAlreadyDifferent()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        await mode.WaitForChangeAsync(seen: false, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForChangeCompletesOnToggle()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        var wait = mode.WaitForChangeAsync(seen: true, CancellationToken.None);
        Assert.False(wait.IsCompleted);
        mode.Toggle();
        await wait.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForChangeHonoursCancellation()
    {
        var mode = new MonitorMode();
        using var cts = new CancellationTokenSource();
        var wait = mode.WaitForChangeAsync(seen: false, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }

    [Fact]
    public void StateChangedIsRaisedOutsideTheLock()
    {
        var mode = new MonitorMode();
        var readFromOtherThread = false;
        mode.StateChanged += (_, _) =>
        {
            // Another thread reading the state blocks forever if the event were raised while holding the lock.
            readFromOtherThread = Task.Run(() => mode.WantsVideo).Wait(TimeSpan.FromSeconds(2));
        };
        mode.SessionStarted();
        Assert.True(readFromOtherThread);
    }

    [Fact]
    public async Task ChangeBetweenReadingTheStateAndWaitingIsNotMissed()
    {
        // The session reads WantsVideo, then waits with that value. Whatever happens in between must wake the wait.
        var mode = new MonitorMode();
        mode.SessionStarted();
        for (var i = 0; i < 2000; i++)
        {
            var seen = mode.WantsVideo;
            var flipper = Task.Run(() => mode.Toggle());
            await mode.WaitForChangeAsync(seen, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            await flipper;
            Assert.NotEqual(seen, mode.WantsVideo);
        }
    }

    [Fact]
    public async Task EverySubscriberOfOneChangeIsWoken()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        var waits = Enumerable.Range(0, 5)
            .Select(_ => mode.WaitForChangeAsync(seen: true, CancellationToken.None))
            .ToArray();
        mode.VideoEnded();
        await Task.WhenAll(waits).WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForChangeIgnoresTogglesThatDoNotChangeWantsVideo()
    {
        // Disconnected and off: Toggle is a no-op and must not wake a waiter that has seen "off".
        var mode = new MonitorMode();
        var wait = mode.WaitForChangeAsync(seen: false, CancellationToken.None);
        mode.Toggle();
        await Task.Delay(50);
        Assert.False(wait.IsCompleted);
        mode.SessionStarted();
        await wait.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToggleDarkWithoutPhoneOrWithTheModeOffChangesNothing()
    {
        var mode = new MonitorMode();
        Assert.Equal(DarkToggleResult.NoVideo, mode.ToggleDark());
        Assert.False(mode.WantsDark);
        mode.SessionStarted();
        mode.Toggle();
        Assert.Equal(DarkToggleResult.NoVideo, mode.ToggleDark());
        Assert.False(mode.WantsDark);
    }

    [Fact]
    public void ToggleDarkAlternatesWhileTheModeIsOn()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        Assert.Equal(DarkToggleResult.TurnedDark, mode.ToggleDark());
        Assert.True(mode.WantsDark);
        Assert.Equal(DarkToggleResult.TurnedLit, mode.ToggleDark());
        Assert.False(mode.WantsDark);
    }

    [Fact]
    public void TurningTheModeOffLightsTheScreen()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        mode.ToggleDark();
        Assert.Equal(ToggleResult.TurnedOff, mode.Toggle());
        Assert.False(mode.WantsDark);
        Assert.Equal(ToggleResult.TurnedOn, mode.Toggle());
        Assert.False(mode.WantsDark);
    }

    [Fact]
    public void VideoEndedSessionEndedAndSessionStartedLightTheScreen()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        mode.ToggleDark();
        mode.VideoEnded();
        Assert.False(mode.WantsDark);

        mode.Toggle();
        mode.ToggleDark();
        mode.SessionEnded();
        Assert.False(mode.WantsDark);

        mode.SessionStarted();
        mode.ToggleDark();
        // A new session (the phone reconnected without SessionEnded in between) starts lit.
        mode.SessionStarted();
        Assert.False(mode.WantsDark);
    }

    [Fact]
    public void PhoneWokeLightsTheScreenAndRaisesDarkChanged()
    {
        var mode = new MonitorMode();
        var events = new List<bool>();
        mode.DarkChanged += events.Add;
        mode.SessionStarted();
        mode.ToggleDark();
        mode.PhoneWoke();
        mode.PhoneWoke();
        Assert.False(mode.WantsDark);
        Assert.True(mode.WantsVideo);
        Assert.Equal(new List<bool> { true, false }, events);
    }

    [Fact]
    public void DarkChangedFiresOnlyOnRealChangesAndOutsideTheLock()
    {
        var mode = new MonitorMode();
        var events = new List<bool>();
        var readFromOtherThread = true;
        mode.DarkChanged += dark =>
        {
            events.Add(dark);
            readFromOtherThread &= Task.Run(() => mode.WantsDark).Wait(TimeSpan.FromSeconds(2));
        };
        mode.SessionStarted();
        mode.VideoEnded(); // already lit: no event
        mode.Toggle();
        mode.ToggleDark();
        mode.SessionEnded();
        Assert.Equal(new List<bool> { true, false }, events);
        Assert.True(readFromOtherThread);
    }

    [Fact]
    public async Task WaitForDarkChangeReturnsAtOnceOrOnTheNextChange()
    {
        var mode = new MonitorMode();
        mode.SessionStarted();
        await mode.WaitForDarkChangeAsync(seen: true, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
        var wait = mode.WaitForDarkChangeAsync(seen: false, CancellationToken.None);
        Assert.False(wait.IsCompleted);
        mode.Toggle(); // video off, already lit: WantsDark did not change
        await Task.Delay(50);
        Assert.False(wait.IsCompleted);
        mode.Toggle();
        mode.ToggleDark();
        await wait.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForDarkChangeHonoursCancellation()
    {
        var mode = new MonitorMode();
        using var cts = new CancellationTokenSource();
        var wait = mode.WaitForDarkChangeAsync(seen: false, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }

    [Fact]
    public async Task DarkChangeBetweenReadingAndWaitingIsNotMissed()
    {
        // The session reads WantsDark, then waits with that value. Whatever happens in between must wake the wait.
        var mode = new MonitorMode();
        mode.SessionStarted();
        for (var i = 0; i < 2000; i++)
        {
            var seen = mode.WantsDark;
            var flipper = Task.Run(() => mode.ToggleDark());
            await mode.WaitForDarkChangeAsync(seen, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            await flipper;
            Assert.NotEqual(seen, mode.WantsDark);
        }
    }

    [Fact]
    public async Task WaitForChangeIgnoresDarkChanges()
    {
        // The video wait must not wake for the dark screen (the session would reread WantsVideo for nothing).
        var mode = new MonitorMode();
        mode.SessionStarted();
        var wait = mode.WaitForChangeAsync(seen: true, CancellationToken.None);
        mode.ToggleDark();
        mode.PhoneWoke();
        await Task.Delay(50);
        Assert.False(wait.IsCompleted);
    }
}
