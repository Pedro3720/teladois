using System.IO.Pipes;
using System.Text;

namespace TelaDois.Core.Display;

public interface IDriverPipe
{
    /// <summary>true se o driver respondeu <c>PONG</c>; false se não está instalado, parado ou não respondeu a tempo.</summary>
    Task<bool> PingAsync(CancellationToken ct);
}

/// <summary>
/// Pipe do Virtual Display Driver (<c>\\.\pipe\MTTVirtualDisplayPipe</c>): comandos em UTF-16, respostas em ASCII.
/// O TelaDois só manda <c>PING</c> (o pipe aceita qualquer usuário do PC — ver revisão de segurança da Etapa 6).
/// <para>
/// O comando PRECISA terminar em NUL (<c>"PING\0"</c>, bytes <c>50 00 49 00 4E 00 47 00 00 00</c>). Medido no
/// Virtual Display Driver 25.7.23: sem o NUL o driver fecha o pipe sem responder; com o NUL responde 4 bytes ASCII
/// <c>PONG</c>; um <c>PING</c> em ASCII não recebe resposta.
/// </para>
/// </summary>
public sealed class DriverPipe(string name = "MTTVirtualDisplayPipe") : IDriverPipe
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(Timeout);
        try
        {
            // Medido no driver real: de vez em quando ele fecha a conexão sem responder (ou o pipe quebra
            // durante connect/write/read) e a conexão seguinte responde. Até 3 tentativas, com uma pausa curta,
            // todas dentro do mesmo limite de tempo.
            bool? answer = null;
            for (var attempt = 0; attempt < 3 && answer is null; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(RetryDelay, limit.Token);
                }
                answer = await TryOnceAsync(limit.Token);
            }
            return answer == true;
        }
        catch (Exception e) when (e is OperationCanceledException && !ct.IsCancellationRequested || e is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>true = PONG; false = respondeu outra coisa; null = o driver fechou/quebrou o pipe sem responder nada.</summary>
    private async Task<bool?> TryOnceAsync(CancellationToken token)
    {
        var reply = new StringBuilder();
        try
        {
            await using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(token);
            await pipe.WriteAsync(Encoding.Unicode.GetBytes("PING\0"), token); // o NUL final é obrigatório
            var buffer = new byte[256];
            int read;
            while ((read = await pipe.ReadAsync(buffer, token)) > 0)
            {
                reply.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (reply.ToString().Contains("PONG", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch (IOException) when (reply.Length == 0)
        {
            return null; // pipe quebrado/fechando antes de qualquer resposta
        }
        return reply.Length == 0 ? null : false;
    }
}
