namespace TelaDois.Core.Video;

/// <summary>
/// Testa as compressões na ordem da spec e guarda a primeira que codifica neste PC. Uma sondagem por vez; o perfil
/// guardado pode ser esquecido (<see cref="Forget"/>) quando a captura com ele não entrega nenhuma imagem.
/// </summary>
public sealed class EncoderProbe(IProcessRunner runner, string ffmpegPath, Action<string> log)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private EncoderProfile? _chosen;

    public TimeSpan ProbeTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public async Task<EncoderProfile> ChooseAsync(int outputIndex, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (Volatile.Read(ref _chosen) is { } chosen)
            {
                return chosen;
            }
            foreach (var profile in EncoderProfiles.Candidates)
            {
                ct.ThrowIfCancellationRequested();
                await using var process = runner.Start(ffmpegPath, FfmpegArguments.Probe(profile, outputIndex));
                int code;
                try
                {
                    code = await process.WaitForExitAsync(ct).WaitAsync(ProbeTimeout, ct);
                }
                catch (TimeoutException)
                {
                    log($"compressão {profile.Id}: sem resposta em {ProbeTimeout.TotalSeconds:0} s");
                    continue;
                }
                if (code == 0)
                {
                    log($"compressão escolhida: {profile.Id} ({profile.Codec})");
                    Volatile.Write(ref _chosen, profile);
                    return profile;
                }
                log($"compressão {profile.Id} indisponível: {LastLine(process.RecentErrors)}");
            }
            throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Esquece <paramref name="profile"/> se for o guardado: a próxima abertura sonda de novo. Devolve se esqueceu.</summary>
    public bool Forget(EncoderProfile profile) => ReferenceEquals(Interlocked.CompareExchange(ref _chosen, null, profile), profile);

    private static string LastLine(string text) =>
        text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "(sem mensagem)";
}
