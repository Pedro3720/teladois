using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Processo de mentira: cada <c>Start</c> consome o próximo roteiro (código de saída + bytes no stdout).</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<IReadOnlyList<string>, FakeProcess> _script;
    public List<IReadOnlyList<string>> Started { get; } = [];
    public int Disposed;

    public FakeProcessRunner(Func<IReadOnlyList<string>, FakeProcess> script) => _script = script;

    public IRunningProcess Start(string fileName, IReadOnlyList<string> arguments)
    {
        lock (Started)
        {
            Started.Add(arguments);
        }
        var process = _script(arguments);
        process.OnDispose = () => Interlocked.Increment(ref Disposed);
        return process;
    }
}

internal sealed class FakeProcess(int exitCode, byte[]? output = null, string errors = "", bool hangs = false) : IRunningProcess
{
    public Action? OnDispose;
    public Stream Output { get; } = new MemoryStream(output ?? []);
    public string RecentErrors => errors;

    /// <summary>Com <c>hangs</c>, a espera só termina quando o token é cancelado (processo travado).</summary>
    public Task<int> WaitForExitAsync(CancellationToken ct) =>
        hangs ? Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => exitCode, TaskContinuationOptions.OnlyOnRanToCompletion) : Task.FromResult(exitCode);

    public ValueTask DisposeAsync()
    {
        OnDispose?.Invoke();
        return ValueTask.CompletedTask;
    }
}
