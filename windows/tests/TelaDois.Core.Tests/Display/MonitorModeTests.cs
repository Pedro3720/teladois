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
}
