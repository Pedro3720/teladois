using System.Collections.Concurrent;
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Monitor virtual de mentira: liga/desliga um booleano e anota cada desligamento.</summary>
internal sealed class FakeVirtualMonitor : IVirtualMonitor
{
    private int _attached;

    public bool Installed { get; set; } = true;
    public string Device { get; set; } = @"\\.\DISPLAY3";
    public DisplayModeInfo Mode { get; set; } = new(2556, 1179, 60);
    public Exception? AttachError { get; set; }

    /// <summary>Com <c>true</c>, <see cref="Detach"/> anota a chamada mas o monitor continua no desktop (desligamento silencioso que falhou).</summary>
    public bool StaysAttached { get; set; }

    /// <summary>Com <c>false</c>, <see cref="Attach"/> "dá certo" mas o monitor não entra no desktop (o Windows aceitou e ignorou).</summary>
    public bool AttachTakes { get; set; } = true;

    public ConcurrentQueue<string> Detached { get; } = new();

    public bool Attached
    {
        get => Volatile.Read(ref _attached) == 1;
        set => Volatile.Write(ref _attached, value ? 1 : 0);
    }

    public Task<bool> IsInstalledAsync(CancellationToken ct) => Task.FromResult(Installed);

    private int _powerOns;
    private int _powerOffs;

    /// <summary>Resposta do <see cref="PowerOnAsync"/> (o monitor apareceu).</summary>
    public bool PowersOn { get; set; } = true;

    /// <summary>Resposta do <see cref="CanPowerOffAsync"/> (as tarefas do Windows existem).</summary>
    public bool CanPowerOff { get; set; } = true;

    public int PowerOns => Volatile.Read(ref _powerOns);
    public int PowerOffs => Volatile.Read(ref _powerOffs);

    public Task<bool> PowerOnAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _powerOns);
        return Task.FromResult(PowersOn);
    }

    public Task PowerOffAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _powerOffs);
        return Task.CompletedTask;
    }

    public Task<bool> CanPowerOffAsync(CancellationToken ct) => Task.FromResult(CanPowerOff);

    public string? Find() => Device;

    public (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height)
    {
        if (AttachError is not null)
        {
            throw AttachError;
        }
        Attached = AttachTakes;
        return (Device, Mode);
    }

    public void Detach(string deviceName)
    {
        if (!StaysAttached)
        {
            Attached = false;
        }
        Detached.Enqueue(deviceName);
    }

    /// <summary>Se definido, <see cref="IsAttached"/> lança esta exceção (a API do Windows falhou).</summary>
    public Exception? IsAttachedError { get; set; }

    public bool IsAttached(string deviceName) =>
        IsAttachedError is { } error ? throw error : Attached && deviceName == Device;
}
