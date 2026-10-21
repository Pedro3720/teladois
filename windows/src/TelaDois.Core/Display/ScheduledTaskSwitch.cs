using TelaDois.Core.Video;

namespace TelaDois.Core.Display;

/// <summary>
/// <see cref="IDeviceSwitch"/> pelas tarefas agendadas <see cref="EnableTask"/>/<see cref="DisableTask"/> (criadas pelo
/// <c>tools\vdd\instalar-vdd.ps1</c>; rodam <c>pnputil /enable-device</c>/<c>/disable-device</c> como SYSTEM e o grupo
/// Usuários pode dispará-las), consultadas e disparadas pelo <c>schtasks.exe</c>.
/// </summary>
public sealed class ScheduledTaskSwitch(IProcessRunner runner, Action<string> log) : IDeviceSwitch
{
    public const string EnableTask = @"\TelaDois\Ligar monitor virtual";
    public const string DisableTask = @"\TelaDois\Desligar monitor virtual";

    // Caminho completo: só o nome faria o Windows procurar primeiro na pasta do TelaDois.exe.
    private static readonly string Schtasks = Path.Combine(Environment.SystemDirectory, "schtasks.exe");

    /// <summary>Prazo de cada <c>schtasks</c>; depois dele o processo é encerrado e a resposta é "não".</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    public async Task<bool> IsAvailableAsync(CancellationToken ct) =>
        await RunSchtasksAsync(["/query", "/tn", EnableTask], ct) == 0
        && await RunSchtasksAsync(["/query", "/tn", DisableTask], ct) == 0;

    public async Task<bool> RunAsync(bool enable, CancellationToken ct)
    {
        var task = enable ? EnableTask : DisableTask;
        var code = await RunSchtasksAsync(["/run", "/tn", task], ct, failure => Log($"tarefa \"{task}\" não rodou ({failure})"));
        return code == 0;
    }

    /// <summary>Código de saída do <c>schtasks</c>, ou <c>null</c> se não abriu, quebrou ou passou do prazo.</summary>
    private async Task<int?> RunSchtasksAsync(IReadOnlyList<string> arguments, CancellationToken ct, Action<string>? onFailure = null)
    {
        IRunningProcess process;
        try
        {
            process = runner.Start(Schtasks, arguments);
        }
        catch (Exception ex)
        {
            onFailure?.Invoke($"o schtasks não abriu: {ex.Message}");
            return null;
        }
        await using (process)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(Timeout);
            try
            {
                // O stdout é redirecionado: lê tudo para o processo nunca travar com o pipe cheio.
                var drain = process.Output.CopyToAsync(Stream.Null, timeout.Token);
                var code = await process.WaitForExitAsync(timeout.Token);
                await IgnoreFailures(drain);
                if (code != 0)
                {
                    var errors = process.RecentErrors.Trim();
                    onFailure?.Invoke(errors.Length == 0 ? $"schtasks código {code}" : $"schtasks código {code}: {errors}");
                }
                return code;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                onFailure?.Invoke($"o schtasks não respondeu em {Timeout.TotalSeconds:0.#} s");
                return null;
            }
            catch (Exception ex)
            {
                onFailure?.Invoke($"o schtasks falhou: {ex.Message}");
                return null;
            }
        }
    }

    private static async Task IgnoreFailures(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
        }
    }

    private void Log(string line)
    {
        try
        {
            log(line);
        }
        catch (Exception)
        {
        }
    }
}
