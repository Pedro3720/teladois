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

    public bool IsAttached(string deviceName) => Attached && deviceName == Device;
}
