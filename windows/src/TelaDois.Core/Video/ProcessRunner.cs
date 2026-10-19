using System.Diagnostics;

namespace TelaDois.Core.Video;

public interface IProcessRunner
{
    IRunningProcess Start(string fileName, IReadOnlyList<string> arguments);
}

/// <summary>Processo em execução. Descartar encerra a árvore inteira.</summary>
public interface IRunningProcess : IAsyncDisposable
{
    Stream Output { get; }
    /// <summary>Últimas linhas do stderr (para o registro).</summary>
    string RecentErrors { get; }
    Task<int> WaitForExitAsync(CancellationToken ct);
}

public sealed class SystemProcessRunner : IProcessRunner
{
    public IRunningProcess Start(string fileName, IReadOnlyList<string> arguments) => new SystemProcess(fileName, arguments);

    private sealed class SystemProcess : IRunningProcess
    {
        private readonly Process _process;
        private readonly Queue<string> _errors = new();

        public SystemProcess(string fileName, IReadOnlyList<string> arguments)
        {
            var info = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }
            _process = new Process { StartInfo = info };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null)
                {
                    return;
                }
                lock (_errors)
                {
                    _errors.Enqueue(e.Data);
                    if (_errors.Count > 20)
                    {
                        _errors.Dequeue();
                    }
                }
            };
            _process.Start();
            _process.BeginErrorReadLine();
        }

        public Stream Output => _process.StandardOutput.BaseStream;

        public string RecentErrors
        {
            get
            {
                lock (_errors)
                {
                    return string.Join(Environment.NewLine, _errors);
                }
            }
        }

        public async Task<int> WaitForExitAsync(CancellationToken ct)
        {
            await _process.WaitForExitAsync(ct);
            return _process.ExitCode;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            try
            {
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException)
            {
            }
            _process.Dispose();
        }
    }
}
