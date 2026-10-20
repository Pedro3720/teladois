using TelaDois.Core.Link;

namespace TelaDois.Core.Display;

public enum ToggleResult
{
    TurnedOn,
    TurnedOff,
    NoPhone,
}

/// <summary>
/// Modo monitor: liga sozinho quando o iPhone conecta, alterna no atalho/bandeja enquanto conectado e desliga quando
/// o iPhone sai ou o vídeo para. Se estende ou duplica é decidido por quem abre o vídeo.
/// </summary>
public sealed class MonitorMode : IVideoSwitch
{
    private readonly object _lock = new();
    private bool _connected;
    private bool _on;
    private TaskCompletionSource _changed = NewSignal();

    /// <summary>(conectado, ligado) a cada mudança real; chamado fora da trava, na thread de quem mudou.</summary>
    public event Action<bool, bool>? StateChanged;

    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _connected;
            }
        }
    }

    public bool WantsVideo
    {
        get
        {
            lock (_lock)
            {
                return _on;
            }
        }
    }

    public void SessionStarted() => Change((_, _) => (true, true));

    public void SessionEnded() => Change((_, _) => (false, false));

    public void VideoEnded() => Change((connected, _) => (connected, false));

    public ToggleResult Toggle()
    {
        var noPhone = false;
        var (_, on) = Change((connected, current) =>
        {
            noPhone = !connected;
            return connected ? (connected, !current) : (connected, current);
        });
        return noPhone ? ToggleResult.NoPhone : on ? ToggleResult.TurnedOn : ToggleResult.TurnedOff;
    }

    public Task WaitForChangeAsync(bool seen, CancellationToken ct)
    {
        Task wait;
        lock (_lock)
        {
            if (_on != seen)
            {
                return Task.CompletedTask;
            }
            wait = _changed.Task;
        }
        return wait.WaitAsync(ct);
    }

    private (bool Connected, bool On) Change(Func<bool, bool, (bool, bool)> next)
    {
        TaskCompletionSource? signal = null;
        bool connected, on;
        lock (_lock)
        {
            (connected, on) = next(_connected, _on);
            if (connected != _connected || on != _on)
            {
                _connected = connected;
                _on = on;
                signal = _changed;
                _changed = NewSignal();
            }
        }
        if (signal is not null)
        {
            signal.TrySetResult();
            StateChanged?.Invoke(connected, on);
        }
        return (connected, on);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
