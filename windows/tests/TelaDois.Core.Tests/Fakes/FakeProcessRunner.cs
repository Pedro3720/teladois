using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>
/// Processo de mentira: cada <c>Start</c> consome o próximo roteiro (código de saída + bytes no stdout).
/// Se o roteiro lançar uma exceção, o <c>Start</c> falha como um executável que não abre (fica registrado em <c>Started</c>).
/// </summary>
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

/// <summary>
/// Com <c>stream</c>, o stdout é esse stream (ex.: <see cref="BlockingStream"/>); senão, os bytes de <c>output</c>.
/// Descartar o processo fecha o stdout, como o fim do processo fecha o pipe.
/// </summary>
internal sealed class FakeProcess(int exitCode, byte[]? output = null, string errors = "", bool hangs = false, Stream? stream = null) : IRunningProcess
{
    public Action? OnDispose;
    public Stream Output { get; } = stream ?? new MemoryStream(output ?? []);
    public string RecentErrors => errors;

    /// <summary>Com <c>hangs</c>, a espera só termina quando o token é cancelado (processo travado).</summary>
    public Task<int> WaitForExitAsync(CancellationToken ct) =>
        hangs ? Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => exitCode, TaskContinuationOptions.OnlyOnRanToCompletion) : Task.FromResult(exitCode);

    public ValueTask DisposeAsync()
    {
        OnDispose?.Invoke();
        Output.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Base dos streams de leitura de mentira (só leitura assíncrona, sem busca).</summary>
internal abstract class FakeReadStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    public abstract override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// Pipe de um processo travado: a leitura <b>ignora</b> o token de cancelamento (como a leitura síncrona de um pipe
/// anônimo no Windows) e só termina, com fim do fluxo, quando o stream é descartado.
/// </summary>
internal sealed class BlockingStream : FakeReadStream
{
    private readonly TaskCompletionSource<int> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completa quando alguém começa a ler.</summary>
    public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsDisposed => _closed.Task.IsCompleted;

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ReadStarted.TrySetResult();
        return new ValueTask<int>(_closed.Task);
    }

    protected override void Dispose(bool disposing)
    {
        _closed.TrySetResult(0);
        base.Dispose(disposing);
    }
}

/// <summary>Pipe que quebra na primeira leitura com a exceção dada.</summary>
internal sealed class FailingStream(Exception error) : FakeReadStream
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        ValueTask.FromException<int>(error);
}

/// <summary>
/// Entrega <c>first</c>, depois avisa em <see cref="AtGate"/> e só entrega <c>rest</c> quando <see cref="Open"/> completar;
/// então fim do fluxo. Simula o FFmpeg que manda uma imagem e só manda as próximas um tempo depois.
/// </summary>
internal sealed class GatedStream(byte[] first, byte[] rest) : FakeReadStream
{
    private ReadOnlyMemory<byte> _pending = first;
    private bool _gatePassed;

    public TaskCompletionSource AtGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Open { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_pending.IsEmpty && !_gatePassed)
        {
            AtGate.TrySetResult();
            await Open.Task.WaitAsync(cancellationToken);
            _gatePassed = true;
            _pending = rest;
        }
        var count = Math.Min(buffer.Length, _pending.Length);
        _pending[..count].CopyTo(buffer);
        _pending = _pending[count..];
        return count;
    }
}
