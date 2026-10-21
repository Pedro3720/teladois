using TelaDois.Core.Link;

namespace TelaDois.Core.Display;

public enum ToggleResult
{
    TurnedOn,
    TurnedOff,
    NoPhone,
}

public enum DarkToggleResult
{
    TurnedDark,
    TurnedLit,
    NoVideo,
}

/// <summary>
/// Modo monitor: liga sozinho quando o iPhone conecta, alterna no atalho/bandeja enquanto conectado e desliga quando
/// o iPhone sai ou o vídeo para. Se estende ou duplica é decidido por quem abre o vídeo.
/// Com o modo ligado, a tela do iPhone pode ficar preta (o monitor continua no Windows); ela volta a acesa sozinha
/// quando o modo desliga, o vídeo para, a sessão começa ou acaba, ou o iPhone avisa que foi tocado.
/// </summary>
public sealed class MonitorMode : IVideoSwitch, IScreenSwitch
{
    private readonly object _lock = new();
    private bool _connected;
    private bool _on;
    private bool _dark;
    private TaskCompletionSource _changed = NewSignal();
    private TaskCompletionSource _darkChanged = NewSignal();

    /// <summary>(conectado, ligado) a cada mudança real; chamado fora da trava, na thread de quem mudou.</summary>
    public event Action<bool, bool>? StateChanged;

    /// <summary>Tela preta ligada/desligada a cada mudança real; chamado fora da trava (quem assina relê <see cref="WantsDark"/>).</summary>
    public event Action<bool>? DarkChanged;

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

    public bool WantsDark
    {
        get
        {
            lock (_lock)
            {
                return _dark;
            }
        }
    }

    public void SessionStarted() => Change(_ => new State(true, true, false));

    public void SessionEnded() => Change(_ => new State(false, false, false));

    public void VideoEnded() => Change(s => s with { On = false, Dark = false });

    public void PhoneWoke() => Change(s => s with { Dark = false });

    public ToggleResult Toggle()
    {
        var noPhone = false;
        var now = Change(s =>
        {
            noPhone = !s.Connected;
            // Desligar o modo acende a tela; ligar de novo começa acesa.
            return s.Connected ? s with { On = !s.On, Dark = false } : s;
        });
        return noPhone ? ToggleResult.NoPhone : now.On ? ToggleResult.TurnedOn : ToggleResult.TurnedOff;
    }

    /// <summary>Só com o iPhone conectado e o modo ligado; senão <see cref="DarkToggleResult.NoVideo"/> e nada muda.</summary>
    public DarkToggleResult ToggleDark()
    {
        var noVideo = false;
        var now = Change(s =>
        {
            noVideo = !s.Connected || !s.On;
            return noVideo ? s : s with { Dark = !s.Dark };
        });
        return noVideo ? DarkToggleResult.NoVideo : now.Dark ? DarkToggleResult.TurnedDark : DarkToggleResult.TurnedLit;
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

    public Task WaitForDarkChangeAsync(bool seen, CancellationToken ct)
    {
        Task wait;
        lock (_lock)
        {
            if (_dark != seen)
            {
                return Task.CompletedTask;
            }
            wait = _darkChanged.Task;
        }
        return wait.WaitAsync(ct);
    }

    private State Change(Func<State, State> next)
    {
        TaskCompletionSource? signal = null;
        TaskCompletionSource? darkSignal = null;
        State now;
        lock (_lock)
        {
            now = next(new State(_connected, _on, _dark));
            if (now.Connected != _connected || now.On != _on)
            {
                _connected = now.Connected;
                _on = now.On;
                signal = _changed;
                _changed = NewSignal();
            }
            if (now.Dark != _dark)
            {
                _dark = now.Dark;
                darkSignal = _darkChanged;
                _darkChanged = NewSignal();
            }
        }
        // Os dois sinais antes de qualquer evento: um assinante que lança não pode esconder uma mudança da sessão.
        signal?.TrySetResult();
        darkSignal?.TrySetResult();
        if (signal is not null)
        {
            StateChanged?.Invoke(now.Connected, now.On);
        }
        if (darkSignal is not null)
        {
            DarkChanged?.Invoke(now.Dark);
        }
        return now;
    }

    private readonly record struct State(bool Connected, bool On, bool Dark);

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
