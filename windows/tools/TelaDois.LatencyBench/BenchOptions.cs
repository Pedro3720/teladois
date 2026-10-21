using System.Globalization;

namespace TelaDois.LatencyBench;

/// <summary>Os cenários da bancada (o que a tela faz enquanto o marcador troca).</summary>
public static class BenchScenarios
{
    /// <summary>Tela parada 2 s, depois o marcador muda (o "mouse parado que começa a mexer").</summary>
    public const string Still = "parado";

    /// <summary>Texto rolando 4 px por imagem; o marcador muda a cada imagem.</summary>
    public const string Scroll = "rolagem";

    /// <summary>Ruído aleatório numa janela de 1/2 × 1/2 (vídeo); o marcador muda a cada imagem.</summary>
    public const string Video = "video";

    public static IReadOnlyList<string> All { get; } = [Still, Scroll, Video];
}

/// <summary>A linha de comando da bancada.</summary>
public sealed record BenchOptions(
    string? Monitor,
    IReadOnlyList<BenchVariant> Variants,
    IReadOnlyList<string> Scenarios,
    TimeSpan Duration,
    string OutDir)
{
    /// <summary>Semente do sorteio dos intervalos do parado (<c>--seed</c>); null = a bancada sorteia uma e a escreve no relatório.</summary>
    public int? Seed { get; init; }

    public const string Usage =
        "uso: TelaDois.LatencyBench [--monitor \\\\.\\DISPLAYn] [--variants base,lowpower,…] [--scenarios parado,rolagem,video] [--seconds 15] [--out builds/bancada] [--seed n]";

    /// <summary>
    /// Lê os argumentos; <see cref="ArgumentException"/> com a explicação em português se algum não servir. Sem
    /// <c>--out</c>, a pasta é <c>builds/bancada</c> na raiz do repositório achada a partir de <paramref name="baseDirectory"/>
    /// (padrão: a pasta do executável), para nunca cair numa pasta que o Git versiona; sem repositório e sem
    /// <c>--out</c>, recusa (em vez de gravar no diretório atual).
    /// </summary>
    public static BenchOptions Parse(IReadOnlyList<string> args, string? baseDirectory = null)
    {
        string? monitor = null;
        IReadOnlyList<BenchVariant> variants = BenchVariants.All;
        IReadOnlyList<string> scenarios = BenchScenarios.All;
        var seconds = 15;
        int? seed = null;
        string? outDir = null;

        for (var i = 0; i < args.Count; i++)
        {
            var flag = args[i];
            if (i + 1 >= args.Count)
            {
                throw new ArgumentException($"falta o valor de {flag}");
            }
            var value = args[++i];
            switch (flag)
            {
                case "--monitor":
                    if (!value.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"--monitor espera um nome como \\\\.\\DISPLAY1, não \"{value}\"");
                    }
                    monitor = value;
                    break;
                case "--variants":
                    variants = List(value, "--variants").Select(name =>
                        BenchVariants.All.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException(
                            $"variante desconhecida \"{name}\" (existem: {string.Join(", ", BenchVariants.All.Select(v => v.Name))})")).ToList();
                    break;
                case "--scenarios":
                    scenarios = List(value, "--scenarios").Select(name =>
                        BenchScenarios.All.FirstOrDefault(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException(
                            $"cenário desconhecido \"{name}\" (existem: {string.Join(", ", BenchScenarios.All)})")).ToList();
                    break;
                case "--seconds":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds is < 1 or > 600)
                    {
                        throw new ArgumentException($"--seconds espera um número inteiro de 1 a 600, não \"{value}\"");
                    }
                    break;
                case "--seed":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSeed))
                    {
                        throw new ArgumentException($"--seed espera um número inteiro não negativo, não \"{value}\"");
                    }
                    seed = parsedSeed;
                    break;
                case "--out":
                    outDir = value;
                    break;
                default:
                    throw new ArgumentException($"opção desconhecida {flag}");
            }
        }

        if (outDir is null)
        {
            var start = baseDirectory ?? AppContext.BaseDirectory;
            var root = FindRepoRoot(start) ?? throw new ArgumentException(
                $"sem --out a bancada grava em builds/bancada na raiz do repositório, mas não achou o .git subindo de {start}: " +
                "passe --out <pasta> (fora de pastas versionadas pelo Git)");
            outDir = Path.Combine(root, "builds", "bancada");
        }
        return new BenchOptions(monitor, variants, scenarios, TimeSpan.FromSeconds(seconds), Path.GetFullPath(outDir)) { Seed = seed };
    }

    /// <summary>A primeira pasta, subindo a partir de <paramref name="start"/>, que tem <c>.git</c> (pasta ou arquivo de worktree).</summary>
    public static string? FindRepoRoot(string start)
    {
        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            var git = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return dir.FullName;
            }
        }
        return null;
    }

    private static List<string> List(string value, string flag)
    {
        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (items.Count == 0)
        {
            throw new ArgumentException($"{flag} precisa de pelo menos um nome");
        }
        return items;
    }
}
