using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using TelaDois.Core.Display;
using TelaDois.Core.Video;

namespace TelaDois.LatencyBench;

/// <summary>
/// Bancada de latência: mede captura + compressão com a linha real do FFmpeg. Sem <c>--monitor</c> liga o monitor virtual
/// como o app (2556×1179) e sempre o desliga no fim; com <c>--monitor</c> usa aquele monitor e não mexe em nenhum.
/// </summary>
internal static class Program
{
    private const int IphoneWidth = 2556;
    private const int IphoneHeight = 1179;
    private static readonly TimeSpan OutputWait = TimeSpan.FromSeconds(10);
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        BenchOptions options;
        try
        {
            options = BenchOptions.Parse(args);
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(BenchOptions.Usage);
            return 2;
        }
        // Sem --seed, sorteia uma e a escreve no relatório: a rodada pode ser repetida com os mesmos intervalos.
        options = options with { Seed = options.Seed ?? Random.Shared.Next() };

        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            Console.Error.WriteLine("ffmpeg.exe não está no PATH (winget install Gyan.FFmpeg)");
            return 1;
        }
        // Antes de abrir janelas ou mexer em monitor: não mistura linhas com colunas de outra versão no mesmo CSV.
        if (BenchStats.HeaderProblem(Path.Combine(options.OutDir, "resultados.csv")) is { } problem)
        {
            Console.Error.WriteLine(problem);
            return 2;
        }

        using var cts = new CancellationTokenSource();
        using var cleanedUp = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // deixa o finally desligar o monitor virtual
            Console.WriteLine("interrompendo…");
            cts.Cancel();
        };
        // Fechar o console pelo X (SIGHUP) ou desligar/sair da sessão (SIGTERM): mesmo cancelamento do Ctrl+C. O Windows
        // encerra o processo quando o tratador volta, então ele espera a limpeza (desligar o monitor virtual) por até 4 s.
        void OnSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cts.Cancel();
            cleanedUp.Wait(TimeSpan.FromSeconds(4));
        }
        using var hangup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnSignal);
        using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);

        var runs = new List<BenchRun>();
        try
        {
            return options.Monitor is { } name
                ? await RunOnPhysicalAsync(name, options, ffmpeg, runs, cts.Token)
                : await RunOnVirtualAsync(options, ffmpeg, runs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("interrompido");
            return 130;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"a bancada parou: {e.Message}");
            runs.Add(new BenchRun(null, [$"a bancada parou: {e.Message}"]));
            return 1;
        }
        finally
        {
            // O relatório sempre é gravado, com o que já foi medido.
            Report(options, runs);
            cleanedUp.Set();
        }
    }

    private static async Task<int> RunOnPhysicalAsync(string name, BenchOptions options, string ffmpeg, List<BenchRun> runs, CancellationToken ct)
    {
        var outputs = DisplayCatalog.Enumerate();
        var output = outputs.FirstOrDefault(o => string.Equals(o.DeviceName, name, StringComparison.OrdinalIgnoreCase));
        if (output is null)
        {
            Fail(runs, $"{name} não está na DXGI (adaptador 0). Monitores: {string.Join(", ", outputs.Select(Describe))}");
            return 1;
        }
        await RunAllAsync(output, options, ffmpeg, runs, ct);
        return 0;
    }

    private static async Task<int> RunOnVirtualAsync(BenchOptions options, string ffmpeg, List<BenchRun> runs, CancellationToken ct)
    {
        static void Log(string line) => Console.WriteLine($"  [monitor virtual] {line}");
        var display = new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe(),
            new ScheduledTaskSwitch(new SystemProcessRunner(), Log), Log);

        if (display.Find() is { } existing && display.IsAttached(existing))
        {
            Fail(runs, $"o monitor virtual {existing} já está no desktop (o TelaDois está estendendo?): desligue a Segunda Tela antes de medir");
            return 1;
        }

        string? device = null;
        try
        {
            if (!await display.PowerOnAsync(ct))
            {
                Fail(runs, "não deu para ativar o monitor virtual (instale com tools\\vdd\\instalar-vdd.ps1)");
                return 1;
            }
            (device, var mode) = display.Attach(IphoneWidth, IphoneHeight);
            Log($"{device} ligado em {mode.Width}×{mode.Height}@{mode.Frequency}");
            var output = await WaitForOutputAsync(device, ct);
            if (output is null)
            {
                Fail(runs, $"o monitor virtual {device} não apareceu na DXGI em {OutputWait.TotalSeconds:0} s");
                return 1;
            }
            await RunAllAsync(output, options, ffmpeg, runs, ct);
            return 0;
        }
        finally
        {
            if (device is not null)
            {
                try
                {
                    display.Detach(device);
                }
                catch (Exception e)
                {
                    Log($"não deu para tirar do desktop: {e.Message}");
                }
            }
            await display.PowerOffAsync(CancellationToken.None);
        }
    }

    private static async Task<DisplayOutput?> WaitForOutputAsync(string device, CancellationToken ct)
    {
        var waited = Stopwatch.StartNew();
        while (waited.Elapsed < OutputWait)
        {
            if (DisplayCatalog.Enumerate().FirstOrDefault(o => string.Equals(o.DeviceName, device, StringComparison.OrdinalIgnoreCase)) is { } output)
            {
                return output;
            }
            await Task.Delay(100, ct);
        }
        return null;
    }

    private static async Task RunAllAsync(DisplayOutput output, BenchOptions options, string ffmpeg, List<BenchRun> runs, CancellationToken ct)
    {
        Console.WriteLine($"medindo {Describe(output)}: {options.Variants.Count} variante(s) × {options.Scenarios.Count} cenário(s), {Durations(options)}");
        if (options.Scenarios.Contains(BenchScenarios.Still) && options.DurationFor(BenchScenarios.Still) < BenchOptions.StillDefault)
        {
            Console.WriteLine("  aviso: o cenário parado dá 1 amostra a cada 2–2,5 s; para números firmes use --seconds 60");
        }
        var runner = new BenchRunner(ffmpeg, line => Console.WriteLine($"  {line}"));
        foreach (var variant in options.Variants)
        {
            foreach (var scenario in options.Scenarios)
            {
                ct.ThrowIfCancellationRequested();
                Console.WriteLine($"→ {variant.Name} / {scenario}");
                BenchRun run;
                try
                {
                    run = await runner.RunAsync(variant, scenario, output, options.DurationFor(scenario), options.OutDir, ct, options.Seed ?? 0);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // Uma rodada que falha vira observação; as outras seguem.
                    run = new BenchRun(null, [$"{variant.Name}/{scenario}: falhou: {e.Message}"]);
                }
                runs.Add(run);
                foreach (var note in run.Notes)
                {
                    Console.WriteLine($"  {note}");
                }
                if (run.Result is { } r)
                {
                    Console.WriteLine($"  mediana {Ms(r.MedianMs)}, p95 {Ms(r.P95Ms)}, {r.Fps.ToString("0.0", Pt)} imagens/s, medidas {r.Measured}, faltas {r.Missing}");
                    Console.WriteLine($"  atraso com leitor ideal: mediana {Ms(r.IdealMedianMs)}, p95 {Ms(r.IdealP95Ms)}");
                    if (r.Still is { } s)
                    {
                        Console.WriteLine(s.HasIdleReference
                            ? $"  ociosas {s.Idle} (mediana {Ms(s.IdleMedianMs)}, p95 {Ms(s.IdleP95Ms)}), com atividade {s.Active} (mediana {Ms(s.ActiveMedianMs)})"
                            : "  sem referência ociosa: nenhuma imagem repetida pequena na rodada");
                    }
                }
            }
        }
    }

    /// <summary>Erro que impede medir: vai para o console e para o relatório.</summary>
    private static void Fail(List<BenchRun> runs, string message)
    {
        Console.Error.WriteLine(message);
        runs.Add(new BenchRun(null, [message]));
    }

    private static void Report(BenchOptions options, List<BenchRun> runs)
    {
        var text = new StringBuilder();
        text.AppendLine($"Bancada de latência — {DateTime.Now.ToString("yyyy-MM-dd HH:mm", Pt)} — {Durations(options)}");
        text.AppendLine("(\"ideal\" = atraso com leitor ideal: a imagem contada no último byte lido do FFmpeg, sem esperar o AUD da seguinte)");
        string[] header = ["variante", "cenário", "mediana", "p95", "máx", "med. ideal", "p95 ideal", "fps", ">33", ">50", "IDR KB", "Mbit/s", "medidas", "faltas", "suspeita"];
        var rows = new List<string[]> { header };
        foreach (var r in runs.Select(run => run.Result).OfType<BenchResult>())
        {
            rows.Add([
                r.Variant, r.Scenario, Ms(r.MedianMs), Ms(r.P95Ms), Ms(r.MaxMs), Ms(r.IdealMedianMs), Ms(r.IdealP95Ms), r.Fps.ToString("0.0", Pt),
                r.GapsOver33.ToString(Pt), r.GapsOver50.ToString(Pt), r.AvgKeyframeKB.ToString("0", Pt), r.Mbps.ToString("0.0", Pt),
                r.Measured.ToString(Pt), r.Missing.ToString(Pt), r.Suspect ? "SIM" : "",
            ]);
        }
        var widths = Enumerable.Range(0, header.Length).Select(c => rows.Max(row => row[c].Length)).ToArray();
        foreach (var row in rows)
        {
            text.AppendLine(string.Join(" | ", row.Select((cell, c) => c < 2 ? cell.PadRight(widths[c]) : cell.PadLeft(widths[c]))));
        }
        var failed = runs.Count(run => run.Result is null);
        if (failed > 0)
        {
            text.AppendLine($"{failed} rodada(s) sem medida (ver as observações abaixo).");
        }
        var still = runs.Select(run => run.Result).OfType<BenchResult>().Where(r => r.Still is not null).ToList();
        if (still.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Parado — trocas a cada {StillSchedule.Base.TotalSeconds:0} s + 0–{StillSchedule.MaxJitterMs} ms (semente {options.Seed}); " +
                $"ociosa = tela parada por ≥ {StillSamples.IdleWindow.TotalSeconds:0} s antes da troca, × com atividade naquele monitor:");
            foreach (var r in still)
            {
                var s = r.Still!;
                if (!s.HasIdleReference)
                {
                    text.AppendLine($"  {r.Variant}: sem referência ociosa (nenhuma imagem repetida ≤ {s.LimitBytes} B na rodada) · " +
                        $"geral mediana {Ms(r.MedianMs)}, p95 {Ms(r.P95Ms)}");
                    continue;
                }
                text.AppendLine($"  {r.Variant}: ociosas {s.Idle} (mediana {Ms(s.IdleMedianMs)}, p95 {Ms(s.IdleP95Ms)}) · " +
                    $"com atividade {s.Active} (mediana {Ms(s.ActiveMedianMs)}, p95 {Ms(s.ActiveP95Ms)}) · geral mediana {Ms(r.MedianMs)}, p95 {Ms(r.P95Ms)}");
                if (r.IdealStill is { HasIdleReference: true } ideal)
                {
                    text.AppendLine($"    com leitor ideal: ociosas mediana {Ms(ideal.IdleMedianMs)}, p95 {Ms(ideal.IdleP95Ms)} · " +
                        $"com atividade mediana {Ms(ideal.ActiveMedianMs)}");
                }
                if (s.Phases.Count > 0)
                {
                    text.AppendLine("    ociosas por imagens desde a última completa: " +
                        string.Join(" · ", s.Phases.Select(p => $"{p.Label}: {p.Count} (mediana {Ms(p.MedianMs)})")));
                }
            }
            if (options.Monitor is not null)
            {
                text.AppendLine($"  Atenção: no {options.Monitor} (monitor físico) o parado depende de ninguém mexer naquela tela; o número que vale é o das ociosas.");
            }
        }
        text.AppendLine("Observações:");
        foreach (var note in runs.SelectMany(run => run.Notes))
        {
            text.AppendLine($"- {note}");
        }

        Console.WriteLine();
        Console.Write(text);
        try
        {
            Directory.CreateDirectory(options.OutDir);
            var report = Path.Combine(options.OutDir, "relatorio.txt");
            File.AppendAllText(report, text + Environment.NewLine);
            Console.WriteLine();
            Console.WriteLine($"arquivos: {Path.Combine(options.OutDir, "resultados.csv")}, {report} e as imagens *-rolagem.png em {options.OutDir}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"não deu para gravar o relatório em {options.OutDir}: {e.Message}");
        }
    }

    private static string Ms(double value) => value.ToString("0.0", Pt) + " ms";

    private static string Ms(double? value) => value is { } v ? Ms(v) : "—";

    private static string Durations(BenchOptions options) =>
        options.DurationGiven || !options.Scenarios.Contains(BenchScenarios.Still)
            ? $"{options.Duration.TotalSeconds:0} s por rodada"
            : $"{options.DurationFor(BenchScenarios.Still).TotalSeconds:0} s no parado, {options.Duration.TotalSeconds:0} s nos outros";

    private static string Describe(DisplayOutput o) => $"{o.DeviceName} {o.Width}×{o.Height} (saída {o.OutputIndex})";
}
