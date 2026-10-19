namespace TelaDois.Core.Video;

/// <summary>Testa as compressões na ordem da spec e guarda a primeira que codifica neste PC.</summary>
public sealed class EncoderProbe(IProcessRunner runner, string ffmpegPath, Action<string> log)
{
    private EncoderProfile? _chosen;

    public TimeSpan ProbeTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public async Task<EncoderProfile> ChooseAsync(int outputIndex, CancellationToken ct)
    {
        if (_chosen is not null)
        {
            return _chosen;
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
                return _chosen = profile;
            }
            log($"compressão {profile.Id} indisponível: {LastLine(process.RecentErrors)}");
        }
        throw new InvalidOperationException("nenhuma compressão H.264 funcionou neste PC");
    }

    private static string LastLine(string text) =>
        text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "(sem mensagem)";
}
