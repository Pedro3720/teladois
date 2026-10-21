using System.Globalization;
using TelaDois.Core.Display;

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

    /// <summary>
    /// Tamanho em que o monitor virtual é ligado (<c>--monitor-size LxA</c>); padrão igual ao app (Etapa 5d): iPhone
    /// 2556×1179 ÷ 1,75 = 1460×674. Não vale com <c>--monitor</c>.
    /// </summary>
    public (int Width, int Height) MonitorSize { get; init; } = DefaultMonitorSize;

    public static readonly (int Width, int Height) DefaultMonitorSize = VirtualMonitorSize.For(2556, 1179);

    /// <summary>Duração padrão do parado (1 amostra a cada 2–2,5 s); os outros cenários usam <see cref="Duration"/>.</summary>
    public static readonly TimeSpan StillDefault = TimeSpan.FromSeconds(60);

    /// <summary>O usuário passou <c>--seconds</c>: vale para todos os cenários.</summary>
    public bool DurationGiven { get; init; }

    /// <summary>Quanto medir no cenário: <c>--seconds</c> se dado; senão 60 s no parado e 15 s nos outros.</summary>
    public TimeSpan DurationFor(string scenario) =>
        !DurationGiven && scenario == BenchScenarios.Still ? StillDefault : Duration;

    public const string Usage =
        "uso: TelaDois.LatencyBench [--monitor \\\\.\\DISPLAYn | --monitor-size LxA (padrão: 1460x674)] [--variants base,lowpower,…] [--scenarios parado,rolagem,video] [--seconds n (padrão: parado 60, outros 15)] [--out builds/bancada] [--seed n]";

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
        var secondsGiven = false;
        int? seed = null;
        string? outDir = null;
        (int Width, int Height)? monitorSize = null;

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
                case "--monitor-size":
                    monitorSize = Size(value);
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
                    secondsGiven = true;
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

        if (monitor is not null && monitorSize is not null)
        {
            throw new ArgumentException("--monitor-size só vale para o monitor virtual (sem --monitor): um monitor físico fica no tamanho dele");
        }
        if (outDir is null)
        {
            var start = baseDirectory ?? AppContext.BaseDirectory;
            var root = FindRepoRoot(start) ?? throw new ArgumentException(
                $"sem --out a bancada grava em builds/bancada na raiz do repositório, mas não achou o .git subindo de {start}: " +
                "passe --out <pasta> (fora de pastas versionadas pelo Git)");
            outDir = Path.Combine(root, "builds", "bancada");
        }
        return new BenchOptions(monitor, variants, scenarios, TimeSpan.FromSeconds(seconds), Path.GetFullPath(outDir)) {
            Seed = seed,
            DurationGiven = secondsGiven,
            MonitorSize = monitorSize ?? DefaultMonitorSize,
        };
    }

    /// <summary><c>LxA</c> (aceita x, X ou ×), cada lado de 2 a 8192.</summary>
    private static (int Width, int Height) Size(string value)
    {
        var parts = value.Split(['x', 'X', '×'], StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height)
            && width is >= 2 and <= 8192 && height is >= 2 and <= 8192)
        {
            return (width, height);
        }
        throw new ArgumentException($"--monitor-size espera largura x altura, como 1460x674 (cada lado de 2 a 8192), não \"{value}\"");
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
