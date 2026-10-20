using System.Collections.Concurrent;
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Tarefas do Windows de mentira: anota cada disparo e chama <see cref="OnRun"/> (que faz o monitor aparecer/sumir).</summary>
internal sealed class FakeDeviceSwitch : IDeviceSwitch
{
    /// <summary>As duas tarefas existem.</summary>
    public bool Available { get; set; } = true;

    /// <summary>O <c>schtasks</c> aceitou o disparo.</summary>
    public bool Accepts { get; set; } = true;

    public ConcurrentQueue<bool> Runs { get; } = new();

    public Action<bool>? OnRun { get; set; }

    public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(Available);

    public Task<bool> RunAsync(bool enable, CancellationToken ct)
    {
        Runs.Enqueue(enable);
        OnRun?.Invoke(enable);
        return Task.FromResult(Accepts);
    }
}
