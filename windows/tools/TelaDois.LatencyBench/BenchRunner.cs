using System.Diagnostics;
using System.Text;
using TelaDois.Core.Display;
using TelaDois.Core.Video;

namespace TelaDois.LatencyBench;

/// <summary>Uma rodada medida: o resultado (null se não deu para medir) e as observações para o relatório.</summary>
public sealed record BenchRun(BenchResult? Result, IReadOnlyList<string> Notes);

/// <summary>
/// Uma rodada da bancada: abre o marcador e a cena, captura com a linha real do FFmpeg (a mesma do app), anota a chegada
/// de cada imagem como o app a recebe (<see cref="AccessUnitReader"/>), descomprime à parte só para ler o marcador e
/// calcula o atraso imagem a imagem.
/// </summary>
public sealed class BenchRunner(string ffmpegPath, Action<string> log)
{
    private const ushort Fps = 60;
    private static readonly TimeSpan WindowsSettle = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FirstFrameLimit = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(3);

    /// <summary>Tempo depois da 1ª imagem que não entra na estatística (a medida dura <c>duration</c> depois dele).</summary>
    public static readonly TimeSpan Warmup = TimeSpan.FromSeconds(1);

    private readonly SystemProcessRunner _runner = new();

    public async Task<BenchRun> RunAsync(
        BenchVariant variant, string scenario, DisplayOutput output, TimeSpan duration, string outDir, CancellationToken ct)
    {
        var notes = new List<string>();
        var profile = EncoderProfiles.Candidates.FirstOrDefault(p => p.Id == variant.ProfileId)
            ?? throw new InvalidOperationException($"perfil de compressão desconhecido: {variant.ProfileId}");
        var bounds = new Win32DisplayApi(persist: false).Bounds(output.DeviceName)
            ?? throw new InvalidOperationException($"não deu para saber a posição de {output.DeviceName} no desktop");
        var monitor = new Rectangle(bounds.X, bounds.Y, output.Width, output.Height);

        // 1–3: janelas, captura e encerramento.
        var arrivals = new List<Arrival>();
        var h264 = new MemoryStream();
        IReadOnlyList<DrawEvent> draws;
        using (var screen = BenchScreen.Start(scenario, monitor))
        {
            try
            {
                await Task.Delay(WindowsSettle, ct);
                notes.Add($"{variant.Name}/{scenario}: janelas: {screen.Describe()}");
                var args = FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps, variant.Settings);
                var errors = await CaptureAsync(args, duration, arrivals, h264, ct);
                notes.Add($"{variant.Name}/{scenario}: janelas no fim: {screen.Describe()}");
                if (arrivals.Count == 0)
                {
                    notes.Add($"o FFmpeg não mandou nenhuma imagem{(errors.Length > 0 ? $": {errors}" : "")}");
                }
            }
            finally
            {
                draws = screen.Stop();
            }
        }
        if (arrivals.Count == 0)
        {
            return new BenchRun(null, notes);
        }

        // 4: descomprime só o recorte do marcador, no tamanho codificado.
        var (encodedWidth, encodedHeight) = profile.EncodedSize(output.Width, output.Height, variant.Settings);
        var crop = MarkerCode.CropIn(output.Width, output.Height, encodedWidth, encodedHeight);
        var bytes = h264.ToArray();
        var (codes, decodeErrors) = await DecodeMarkersAsync(bytes, crop, ct);
        if (decodeErrors.Length > 0)
        {
            notes.Add($"descompressão para ler o marcador: {decodeErrors}");
        }

        // 6: imagens decodificadas ≠ chegadas → avisa e casa pelo menor dos dois.
        if (codes.Count != arrivals.Count)
        {
            notes.Add($"{variant.Name}/{scenario}: {codes.Count} imagens decodificadas para {arrivals.Count} chegadas; casando as {Math.Min(codes.Count, arrivals.Count)} primeiras");
        }
        var count = Math.Min(codes.Count, arrivals.Count);

        // O 1º segundo fica fora da conta: enquanto a compressão abre, o FFmpeg acumula imagens e depois as solta de uma
        // vez (medido: ~0,6 s de atraso e mais de 60 imagens/s no começo). Ele continua sendo descomprimido (tem a 1ª IDR).
        var warmupEnd = arrivals[0].Ticks + (long)(Warmup.TotalSeconds * Stopwatch.Frequency);
        var skip = Math.Min(arrivals.Count(a => a.Ticks < warmupEnd), count);
        var measured = arrivals.Skip(skip).ToList();
        var matchedCodes = codes.Take(count).Skip(skip).ToList();

        // 5: atrasos (só a imagem onde cada código novo aparece), estatística, CSV e print.
        var latencies = LatencyMatcher.Match(draws, measured.Take(matchedCodes.Count).ToList(), matchedCodes, Stopwatch.Frequency);
        var samples = LatencyMatcher.FirstChangesOnly(matchedCodes, latencies);
        var result = BenchStats.Compute(variant.Name, scenario, measured, samples, Stopwatch.Frequency);

        Directory.CreateDirectory(outDir);
        var csv = Path.Combine(outDir, "resultados.csv");
        var line = BenchStats.ToCsv(result) + Environment.NewLine;
        await File.AppendAllTextAsync(csv, File.Exists(csv) ? line : BenchStats.CsvHeader + Environment.NewLine + line, ct);

        await WriteFramesAsync(Path.Combine(outDir, $"quadros-{variant.Name}-{scenario}.csv"), arrivals, codes, draws, count, skip, ct);

        if (scenario == BenchScenarios.Scroll && count > 0)
        {
            var png = Path.Combine(outDir, $"{variant.Name}-rolagem.png");
            var pngErrors = await SaveFrameAsync(bytes, count / 2, png, ct);
            notes.Add(File.Exists(png) ? $"imagem do meio da rolagem: {png}" : $"não deu para gravar {png}: {pngErrors}");
        }

        notes.Add($"{variant.Name}/{scenario}: {encodedWidth}×{encodedHeight}, marcador em {crop.X},{crop.Y} ({crop.Width}×{crop.Height}), {draws.Count} trocas do marcador, {arrivals.Count} imagens ({skip} no aquecimento de {Warmup.TotalSeconds:0} s), {bytes.Length / 1024} KB");
        return new BenchRun(result, notes);
    }

    /// <summary>Uma linha por imagem (para investigar picos): instante desde a 1ª, completa, bytes, código lido, atraso.</summary>
    private static async Task WriteFramesAsync(
        string path, List<Arrival> arrivals, List<int?> codes, IReadOnlyList<DrawEvent> draws, int count, int skip, CancellationToken ct)
    {
        var c = System.Globalization.CultureInfo.InvariantCulture;
        var latencies = LatencyMatcher.Match(draws, arrivals.Take(count).ToList(), codes.Take(count).ToList(), Stopwatch.Frequency);
        var text = new StringBuilder("index,ms,keyframe,bytes,code,latency_ms,warmup\n");
        for (var i = 0; i < count; i++)
        {
            var ms = (arrivals[i].Ticks - arrivals[0].Ticks) * 1000.0 / Stopwatch.Frequency;
            text.Append(c, $"{i},{ms:0.0},{(arrivals[i].Keyframe ? 1 : 0)},{arrivals[i].Bytes},{codes[i]},{latencies[i]:0.0},{(i < skip ? 1 : 0)}\n");
        }
        await File.WriteAllTextAsync(path, text.ToString(), ct);
    }

    /// <summary>
    /// Roda o FFmpeg da captura e anota cada imagem até <paramref name="duration"/> depois da primeira. Um vigia encerra o
    /// processo se a primeira imagem não vier em 10 s ou se o fluxo parar depois do prazo. Devolve o fim do stderr.
    /// </summary>
    private async Task<string> CaptureAsync(
        IReadOnlyList<string> args, TimeSpan duration, List<Arrival> arrivals, MemoryStream h264, CancellationToken ct)
    {
        var process = _runner.Start(ffmpegPath, args);
        await using (process)
        {
            var started = Stopwatch.GetTimestamp();
            long deadline = long.MaxValue;
            using var done = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var watchdog = WatchAsync();
            try
            {
                await foreach (var unit in new AccessUnitReader(process.Output).ReadAllAsync(CancellationToken.None))
                {
                    var now = Stopwatch.GetTimestamp();
                    if (now >= Interlocked.Read(ref deadline) || ct.IsCancellationRequested)
                    {
                        break;
                    }
                    arrivals.Add(new Arrival(now, unit.IsKeyframe, unit.Data.Length));
                    h264.Write(unit.Data);
                    if (arrivals.Count == 1)
                    {
                        Interlocked.Exchange(ref deadline, now + (long)((Warmup + duration).TotalSeconds * Stopwatch.Frequency));
                    }
                }
            }
            finally
            {
                done.Cancel();
                await watchdog;
            }
            var errors = process.RecentErrors.Trim();
            ct.ThrowIfCancellationRequested();
            return errors;

            async Task WatchAsync()
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(100, done.Token);
                        var elapsed = Stopwatch.GetElapsedTime(started);
                        var limit = Interlocked.Read(ref deadline);
                        var late = limit == long.MaxValue
                            ? elapsed > FirstFrameLimit
                            : Stopwatch.GetTimestamp() > limit + (long)(StopGrace.TotalSeconds * Stopwatch.Frequency);
                        if (late)
                        {
                            log(limit == long.MaxValue
                                ? "o FFmpeg não mandou a primeira imagem em 10 s: encerrando"
                                : "o FFmpeg parou de mandar imagens: encerrando");
                            await process.DisposeAsync();
                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Fim normal: quem encerra o processo é o await using. Ctrl+C: encerra já (a leitura pode estar parada).
                    if (ct.IsCancellationRequested)
                    {
                        await process.DisposeAsync();
                    }
                }
            }
        }
    }

    /// <summary>Descomprime o H.264 gravado e lê o código do marcador em cada imagem, na ordem.</summary>
    private async Task<(List<int?> Codes, string Errors)> DecodeMarkersAsync(
        byte[] h264, (int X, int Y, int Width, int Height) crop, CancellationToken ct)
    {
        var frameSize = crop.Width * crop.Height;
        var codes = new List<int?>();
        string[] args =
        [
            "-nostdin", "-hide_banner", "-loglevel", "error",
            "-f", "h264", "-i", "pipe:0",
            "-fps_mode", "passthrough",
            // Cinza antes do recorte: o recorte em 4:2:0 arredondaria x/y ímpares (o 2/3 cai em 11,11).
            "-vf", $"format=gray,crop={crop.Width}:{crop.Height}:{crop.X}:{crop.Y}",
            "-f", "rawvideo", "pipe:1",
        ];
        var errors = await RunPipeAsync(args, h264, async output =>
        {
            var frame = new byte[frameSize];
            while (await output.ReadAtLeastAsync(frame, frameSize, throwOnEndOfStream: false, ct) == frameSize)
            {
                codes.Add(MarkerCode.Decode(frame, crop.Width, crop.Height));
            }
        }, ct);
        return (codes, errors);
    }

    /// <summary>Grava a imagem <paramref name="index"/> (cores, tamanho codificado) em PNG.</summary>
    private Task<string> SaveFrameAsync(byte[] h264, int index, string path, CancellationToken ct)
    {
        string[] args =
        [
            "-nostdin", "-hide_banner", "-loglevel", "error",
            "-f", "h264", "-i", "pipe:0",
            "-vf", $"select=eq(n\\,{index})",
            "-fps_mode", "passthrough", "-frames:v", "1", "-update", "1", "-y", path,
        ];
        return RunPipeAsync(args, h264, output => output.CopyToAsync(Stream.Null, ct), ct);
    }

    /// <summary>Roda o FFmpeg com <paramref name="input"/> no stdin e o stdout para <paramref name="readOutput"/>; devolve o stderr.</summary>
    private async Task<string> RunPipeAsync(IReadOnlyList<string> args, byte[] input, Func<Stream, Task> readOutput, CancellationToken ct)
    {
        var info = new ProcessStartInfo(ffmpegPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }
        var errors = new StringBuilder();
        using var process = new Process { StartInfo = info };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (errors)
                {
                    if (errors.Length < 4000)
                    {
                        errors.AppendLine(e.Data);
                    }
                }
            }
        };
        process.Start();
        try
        {
            process.BeginErrorReadLine();
            var write = Task.Run(async () =>
            {
                try
                {
                    await process.StandardInput.BaseStream.WriteAsync(input, ct);
                }
                catch (IOException)
                {
                    // O FFmpeg fechou o stdin antes (erro): o stderr explica.
                }
                finally
                {
                    try
                    {
                        process.StandardInput.Close();
                    }
                    catch (IOException)
                    {
                    }
                }
            }, ct);
            await readOutput(process.StandardOutput.BaseStream);
            await write;
            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0)
            {
                lock (errors)
                {
                    errors.Insert(0, $"FFmpeg terminou com código {process.ExitCode}. ");
                }
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        lock (errors)
        {
            return errors.ToString().Trim();
        }
    }
}
