# Etapa 5d — menos atraso e menos engasgos — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Medir no PC quanto a captura + compressão atrasa (bancada automática), escolher a configuração da Intel QSV com menos atraso e menos engasgos, aplicá-la no TelaDois e confirmar no iPhone com uma foto do cronômetro.

**Architecture:** O `FfmpegArguments` ganha um `EncodeSettings` (GOP, taxas, opções do `ddagrab`, redução de tamanho) com padrões iguais aos de hoje — o app e a bancada montam a linha do FFmpeg pelo mesmo código. A bancada (`windows/tools/TelaDois.LatencyBench`, console WinForms fora do app) desenha um marcador que muda de código a cada imagem, captura com a linha real, descomprime à parte só para ler o marcador e calcula o atraso imagem a imagem. A configuração vencedora vira o padrão do perfil Intel.

**Tech Stack:** C# 14 / .NET 10, WinForms (janelas da bancada), FFmpeg 9.0.2 (`ddagrab`, `h264_qsv`, `vpp_qsv`), xUnit 2.9.3.

**Spec:** `docs/superpowers/specs/2026-10-21-etapa-5d-atraso-e-engasgos-design.md`. **Branch:** `etapa-5b-monitor-virtual`.

## Global Constraints

- Textos para o usuário (registro, relatório da bancada, docs) em português; código e nomes em inglês.
- PC principal do ajuste: **PC do trabalho, Intel QSV**. A AMD só recebe ajustes equivalentes seguros e precisa continuar passando na sondagem; `nvenc`, `mf`, `x264` não mudam de comportamento.
- **Padrões de hoje (base):** `-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M`, `ddagrab=output_idx=<n>:framerate=60:draw_mouse=1`, perfil `qsv-hwmap` = `hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2` + `-low_power 0 -look_ahead 0 -async_depth 1`. Com os padrões, a linha gerada tem de ser **idêntica** à de hoje (os testes atuais de `FfmpegArguments` continuam verdes sem mudar).
- Tamanho codificado sempre **par**; o `CONFIG` leva o tamanho codificado (`EncoderProfile.EncodedSize`).
- Pode ceder um pouco de nitidez e reduzir a resolução de compressão (o monitor do Windows continua 2556×1179; o iPhone amplia). Reduções testadas: **1920×886** e **1704×786** (2/3).
- Metas da bancada (configuração vencedora, Intel): atraso mediano **≤ 35 ms**, p95 **≤ 60 ms**, nenhum intervalo entre imagens **> 50 ms** no cenário rolagem, **≥ 58 imagens/s** nos cenários rolagem e vídeo. Meta ponta a ponta (foto): **≤ ~80 ms**.
- Regra de escolha: menor mediana **sem piorar** p95 nem os buracos; entre parecidas, a de maior nitidez; resolução menor só se ganhar ≥ 5 ms de mediana ou tirar buracos.
- Invariantes já provados (não quebrar): PING sempre sai; feed descartado 1 vez; monitores só por `VirtualDisplay`/`IVirtualMonitor`, nunca num físico; handlers nunca lançam; `FileLog.Write` nunca lança; zero avisos.
- A bancada abre janelas na tela e pode ligar o monitor virtual: só roda quando o controlador combina com o usuário. Testes automáticos nunca abrem a bancada, nunca tocam monitores nem rodam `schtasks`/`pnputil`.
- Commits: `.git/TD_MSG` + `git commit -F`, trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`, arquivos pelo nome, sem amend/force. `windows-test` verde no SHA final de cada tarefa; `ios-test`/`ios-build` também no SHA final da etapa.

## Estrutura de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `windows/src/TelaDois.Core/Video/EncodeSettings.cs` (novo) | GOP, taxas, opções do `ddagrab`, redução de tamanho (`ScaleTo`) — padrões = hoje |
| `windows/src/TelaDois.Core/Video/FfmpegArguments.cs`, `EncoderProfiles.cs` | usam o `EncodeSettings`; `EncodedSize` respeita a redução |
| `windows/tools/TelaDois.LatencyBench/` (novo, console `net10.0-windows`, WinForms) | `MarkerCode`, `LatencyMatcher`, `BenchStats`, `BenchVariants`, janelas do marcador/cenas, `BenchRunner`, `Program` |
| `windows/tests/TelaDois.LatencyBench.Tests/` (novo) | testes das partes puras da bancada |
| `docs/medicoes/2026-10-21-bancada-5d.md` (novo) | resultados das medições e a escolha |

---

### Task 1: `EncodeSettings` no Core (sem mudar o comportamento)

**Files:**
- Create: `windows/src/TelaDois.Core/Video/EncodeSettings.cs`
- Modify: `windows/src/TelaDois.Core/Video/FfmpegArguments.cs`, `windows/src/TelaDois.Core/Video/EncoderProfiles.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/FfmpegArgumentsTests.cs`, `windows/tests/TelaDois.Core.Tests/Video/EncoderProfilesTests.cs` (ou o arquivo onde estão os testes de `EncodedSize`)

**Interfaces:**
- Produces:

```csharp
namespace TelaDois.Core.Video;

/// <summary>Como a captura e a compressão são montadas. <see cref="Default"/> = a linha de hoje.</summary>
public sealed record EncodeSettings
{
    public static EncodeSettings Default { get; } = new();
    public int GopSeconds { get; init; } = 2;          // -g = fps * GopSeconds
    public string Bitrate { get; init; } = "12M";
    public string Maxrate { get; init; } = "20M";
    public string Bufsize { get; init; } = "4M";
    /// <summary>Opções extras do ddagrab, ex. ":dup_frames=0" (começa com ':' ou vazio).</summary>
    public string GrabOptions { get; init; } = "";
    /// <summary>Opções extras da compressão, acrescentadas depois das do perfil (ex. ["-int_ref_type","vertical"]).</summary>
    public IReadOnlyList<string> ExtraCodecOptions { get; init; } = [];
    /// <summary>Opções do perfil a trocar: chave → valor novo (ex. {"-low_power":"1"}); chave ausente = acrescenta.</summary>
    public IReadOnlyDictionary<string, string> CodecOptionOverrides { get; init; } = new Dictionary<string, string>();
    /// <summary>Comprimir menor: tamanho alvo (par). null = tamanho do monitor (cortado para par se o perfil corta).</summary>
    public (int Width, int Height)? ScaleTo { get; init; }
}
```
- `FfmpegArguments.ForMonitor(EncoderProfile profile, int outputIndex, ushort fps, EncodeSettings? settings = null)`, `Encode(EncoderProfile profile, string inputGraph, ushort fps, int? frames = null, EncodeSettings? settings = null)`, `Probe(EncoderProfile profile, int outputIndex, EncodeSettings? settings = null)`; `EncoderProfile.EncodedSize(int width, int height, EncodeSettings? settings = null)`.
- Regras:
  - `GrabOptions` vai no fim do grafo do `ddagrab`: `ddagrab=output_idx=<n>:framerate=<fps>:draw_mouse=1<GrabOptions>`.
  - `-g` = `fps * GopSeconds`; taxas de `Bitrate`/`Maxrate`/`Bufsize`.
  - Opções do perfil com `CodecOptionOverrides` aplicados no lugar (mesma posição); chaves que o perfil não tem vão no fim, seguidas de `ExtraCodecOptions`.
  - `ScaleTo`: no perfil `qsv-hwmap` o `vpp_qsv=cw=…:ch=…` vira `vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2:w=<W>:h=<H>`; nos perfis com download, `,scale=<W>:<H>` logo depois do `EvenCrop`; nos perfis sem filtro (`amf-d3d11`, `nvenc-d3d11`) `ScaleTo` é ignorado (registrar isso no comentário). `W`/`H` são arredondados para baixo para par. `EncodedSize` devolve o `ScaleTo` (par) quando ele se aplica ao perfil.
  - `Probe` usa as mesmas opções de compressão do `Encode` (já é assim hoje com as opções do perfil; agora também com o `EncodeSettings`).

- [ ] **Step 1: Testes** — (a) para cada perfil de `EncoderProfiles.Candidates`, `ForMonitor(p, 2, 60)` e `ForMonitor(p, 2, 60, EncodeSettings.Default)` são iguais à linha de hoje (copie a lista esperada de um teste existente ou monte-a com os valores da base); (b) `GopSeconds = 4` → `-g 240`; (c) `Maxrate="12M", Bufsize="200k"`; (d) `GrabOptions=":dup_frames=0"` → `ddagrab=output_idx=2:framerate=60:draw_mouse=1:dup_frames=0`; (e) `CodecOptionOverrides {"-low_power":"1"}` no `qsv-hwmap` troca `0` por `1` no mesmo lugar e `{"-preset":"veryfast"}` acrescenta no fim; (f) `ScaleTo (1920,886)` no `qsv-hwmap` → filtro termina em `vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2:w=1920:h=886`, no `x264` contém `,scale=1920:886`, e `EncodedSize(2556,1179,s)` = `(1920,886)`; `ScaleTo (1705,787)` vira `1704×786`; no `amf-d3d11` o `ScaleTo` não muda nada.
- [ ] **Step 2:** `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~FfmpegArguments|FullyQualifiedName~EncoderProfile"` → falha (compilação).
- [ ] **Step 3: Implementar** conforme as regras; quem chama hoje (`VideoFeedFactory`, `EncoderProbe`, `FfmpegCapture`) continua sem passar `settings` (padrão).
- [ ] **Step 4:** suíte inteira verde, 0 avisos.
- [ ] **Step 5: Commit** — `feat(windows): EncodeSettings monta a linha do FFmpeg (padrões = hoje)`; push; `windows-test` verde.

---

### Task 2: Partes puras da bancada (marcador, casamento, estatística, variantes)

**Files:**
- Create: `windows/tools/TelaDois.LatencyBench/TelaDois.LatencyBench.csproj` (`<OutputType>Exe</OutputType>`, `net10.0-windows`, `<UseWindowsForms>true</UseWindowsForms>`, referência ao `TelaDois.Core`), `MarkerCode.cs`, `LatencyMatcher.cs`, `BenchStats.cs`, `BenchVariants.cs`
- Create: `windows/tests/TelaDois.LatencyBench.Tests/TelaDois.LatencyBench.Tests.csproj` (xUnit 2.9.3, igual ao `TelaDois.Core.Tests`), testes
- Modify: `windows/TelaDois.sln` (os dois projetos novos; pasta de solução `tools`)

**Interfaces:**
- Produces:
  - `public static class MarkerCode { public const int Size = 64; public const int Grid = 4; public static bool[] Encode(int code); public static int? Decode(ReadOnlySpan<byte> gray, int width, int height); }` — 16 blocos 16×16 (4×4) preto/branco = 16 bits (`code & 0xFFFF`, bloco 0 = bit mais alto, linha por linha); `Decode` lê o centro de cada bloco de uma imagem cinza `width×height` (o recorte do marcador, que pode vir reduzido: usa as coordenadas proporcionais) com limiar 128; devolve `null` se algum centro estiver entre 64 e 192 (borrado demais).
  - `public sealed record DrawEvent(int Code, long Ticks);` `public sealed record Arrival(long Ticks, bool Keyframe, int Bytes);`
  - `public static class LatencyMatcher { public static IReadOnlyList<double?> Match(IReadOnlyList<DrawEvent> draws, IReadOnlyList<Arrival> arrivals, IReadOnlyList<int?> decodedCodes, long ticksPerSecond); }` — para cada imagem `i` (chegada `i` ↔ código decodificado `i`, mesma ordem), procura o **último** `DrawEvent` com aquele código e `Ticks ≤ arrivals[i].Ticks` e devolve o atraso em ms; `null` se o código for `null` ou não houver desenho correspondente. (O código dá a volta a cada 65 536 trocas, ~18 min: irrelevante para cenários de 15 s.)
  - `public sealed record BenchResult(string Variant, string Scenario, double MedianMs, double P95Ms, double MaxMs, double Fps, int GapsOver33, int GapsOver50, double AvgKeyframeKB, double MaxKeyframeKB, double Mbps, int Measured, int Missing);` e `public static class BenchStats { public static BenchResult Compute(string variant, string scenario, IReadOnlyList<Arrival> arrivals, IReadOnlyList<double?> latencies, long ticksPerSecond); public static string CsvHeader { get; } public static string ToCsv(BenchResult r); }` — percentis por ordenação (p95 = elemento de índice `ceil(0.95 n) − 1`); fps e taxa pela janela entre a 1ª e a última chegada; buracos = intervalos entre chegadas consecutivas.
  - `public sealed record BenchVariant(string Name, string ProfileId, EncodeSettings Settings);` e `public static class BenchVariants { public static IReadOnlyList<BenchVariant> All { get; } }` com, nesta ordem: `base` (`qsv-hwmap`, `Default`); `lowpower` (`-low_power 1`); `veryfast` (`-preset veryfast`); `cbr` (`Maxrate = Bitrate = "12M"`, `Bufsize = "200k"`); `intrarefresh` (`ExtraCodecOptions = ["-int_ref_type","vertical","-int_ref_cycle_size","60"]`, `GopSeconds = 10`); `gop4` (`GopSeconds = 4`); `nodup` (`GrabOptions = ":dup_frames=0"`); `download` (perfil `qsv-download`, `Default`); `scale1920` (`ScaleTo = (1920, 886)`); `scale2of3` (`ScaleTo = (1704, 786)`).

- [ ] **Step 1: Testes** (no projeto novo):
  - `MarkerCode`: `Encode` de 0, 1, 0xFFFF, 0xA5A5 (padrões esperados); ida e volta `Decode(Render(Encode(c)))` para 200 códigos aleatórios, com a imagem 64×64 **e** reduzida para 48×48 (renderize no teste um array de bytes cinza); centro cinza 128 → `null`.
  - `LatencyMatcher`: desenhos em t = 0, 16, 33 ms com códigos 1, 2, 3; chegadas em 40, 50 e 60 ms com códigos 2, 3, `null` → `[24, 27, null]`; código repetido mais tarde usa o desenho mais recente antes da chegada; código sem desenho → `null`.
  - `BenchStats`: 10 atrasos conhecidos → mediana/p95/máx exatos; chegadas a cada 16,7 ms com um buraco de 60 ms → `GapsOver33 = 1`, `GapsOver50 = 1`; fps ≈ 60; `ToCsv` com ponto decimal (cultura invariante).
  - `BenchVariants.All`: nomes únicos; `base` usa `EncodeSettings.Default`; `scale2of3` com tamanho par.
- [ ] **Step 2:** vermelho (compilação). **Step 3:** implementar. **Step 4:** `dotnet test windows/TelaDois.sln` verde, 0 avisos (a bancada compila no CI).
- [ ] **Step 5: Commit** — `feat(bancada): marcador, casamento de atrasos, estatística e variantes da bancada de latência`; push; `windows-test` verde.

---

### Task 3: A bancada que mede (janelas, captura real, relatório)

**Files:**
- Create: `windows/tools/TelaDois.LatencyBench/MarkerWindow.cs`, `SceneWindows.cs`, `BenchRunner.cs`, `Program.cs`
- Modify: `.gitignore` (garantir `builds/` já ignorado — `builds/bancada/` vai para lá)

**Interfaces:**
- Consumes: Task 1 (`EncodeSettings`, `FfmpegArguments.ForMonitor(..., settings)`), Task 2 (todas as peças puras), Core (`AccessUnitReader`, `DisplayCatalog.Enumerate()`, `SystemProcessRunner`, `FfmpegLocator`, `VirtualDisplay`, `Win32DisplayApi.ForVirtualMonitor()`, `DriverPipe`, `ScheduledTaskSwitch`).
- Produces: `TelaDois.LatencyBench.exe [--monitor \\.\DISPLAYn] [--variants base,lowpower,…] [--scenarios parado,rolagem,video] [--seconds 15] [--out builds/bancada]`.

- [ ] **Step 1: `MarkerWindow`** — `Form` sem borda, `TopMost`, `ShowInTaskbar = false`, 64×64, posicionada no canto superior esquerdo do monitor alvo + (16, 16) (múltiplos de 16), `DoubleBuffered`. Uma thread de fundo faz, a cada volta: `DwmFlush()` (P/Invoke `dwmapi.dll`), troca o código (`code++`) se o cenário pede mudança, `Invalidate()` + `Update()` pela thread da UI (`Invoke`), e grava `DrawEvent(code, Stopwatch.GetTimestamp())` **depois** do `DwmFlush` seguinte (a imagem foi composta). Cenário `parado`: o código fica parado 2 s e muda 1 vez, repetindo 6 vezes; `rolagem` e `video`: muda a cada volta.
- [ ] **Step 2: `SceneWindows`** — `rolagem`: janela `TopMost` de 1/3 da largura × altura do monitor ao lado do marcador, com texto (Segoe UI 16, linhas "Linha N — o rato roeu a roupa do rei de Roma") rolando 4 px por volta (mesma thread/`DwmFlush`); `video`: janela de 1/2 × 1/2 com ruído aleatório (um `Bitmap` 1/4 do tamanho, esticado) trocado a cada volta. `parado`: nenhuma cena.
- [ ] **Step 3: `BenchRunner.RunAsync(BenchVariant, string scenario, DisplayOutput output, TimeSpan duration, string outDir, CancellationToken)`** —
  1. abre as janelas e espera 1 s;
  2. inicia o FFmpeg com `FfmpegArguments.ForMonitor(profile, output.OutputIndex, 60, variant.Settings)` pelo `SystemProcessRunner`; lê `AccessUnitReader.ReadAllAsync` e grava `Arrival(Stopwatch.GetTimestamp(), au.IsKeyframe, au.Data.Length)` e os bytes num `MemoryStream`;
  3. ao fim do tempo encerra o FFmpeg e fecha as janelas;
  4. descomprime os bytes gravados com um segundo FFmpeg: `-f h264 -i pipe:0 -fps_mode passthrough -vf crop=<w>:<h>:<x>:<y>,format=gray -f rawvideo pipe:1`, onde o recorte é a posição do marcador **no tamanho codificado** (`EncodedSize`, proporcional ao monitor), e lê `w×h` bytes por imagem → `MarkerCode.Decode`;
  5. `LatencyMatcher.Match` + `BenchStats.Compute`; acrescenta a linha no `outDir/resultados.csv` (cria com `CsvHeader`) e grava `outDir/<variante>-rolagem.png` (uma imagem do meio, decodificada em RGB pelo mesmo FFmpeg com `-frames:v 1`) só no cenário rolagem;
  6. se o número de imagens decodificadas ≠ chegadas, registra no relatório e casa pelo menor dos dois.
- [ ] **Step 4: `Program`** — sem `--monitor`: liga o monitor virtual como o app (`VirtualDisplay` com `ScheduledTaskSwitch`, `PowerOnAsync`, `Attach(2556, 1179)`, espera a saída aparecer na DXGI) e **sempre** `Detach` + `PowerOffAsync` no `finally`; com `--monitor`: usa aquela saída e não mexe em monitores. Roda variantes × cenários, imprime uma tabela (`variante | cenário | mediana | p95 | máx | fps | >33 | >50 | IDR KB | Mbit/s`) em português, e escreve no fim o caminho dos arquivos.
- [ ] **Step 5: Teste rápido real (combinar com o usuário antes — abre janelas e, sem `--monitor`, liga o monitor virtual):** `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- --variants base --scenarios rolagem --seconds 5`; esperado: uma linha com `Measured` > 200 e `Missing` < 5%, e o `.png` legível. Guardar a saída no relatório.
- [ ] **Step 6:** `dotnet test windows/TelaDois.sln` verde (as partes novas não têm teste automático: dependem de tela; as puras já estão cobertas), 0 avisos. **Commit** — `feat(bancada): bancada de latência mede captura + compressão com a linha real do FFmpeg`; push; `windows-test` verde.

---

### Task 3b: Monitor do iPhone em 100% (resolução ÷ 1,75) — antes da rodada 2

**Files:** `tools/vdd/vdd_settings.xml`; `windows/src/TelaDois.Core/Display/VirtualDisplay.cs` (ou um helper puro novo `VirtualMonitorSize`); `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs`; `windows/tools/TelaDois.LatencyBench/` (opção `--monitor-size`); testes.

**Interfaces:**
- Produces: `public static (int Width, int Height) VirtualMonitorSize.For(int iphoneWidth, int iphoneHeight)` = (w ÷ 1,75, h ÷ 1,75), cada um arredondado para o **par mais próximo** (2556×1179 → 1460×674; 2796×1290 → 1598×738; 2622×1206 → 1498×690; 2868×1320 → 1638×754; 2736×1260 → 1564×720).

- [ ] **Step 1:** `vdd_settings.xml`: acrescentar os 5 modos da tabela acima a 60 Hz (manter os antigos). Conferir XML.
- [ ] **Step 2:** testes do `VirtualMonitorSize.For` com a tabela; `VideoFeedFactory` passa a chamar `Attach(VirtualMonitorSize.For(hello))` (teste com o `FakeVirtualMonitor`: o `Attach` recebe 1460×674 para o HELLO 2556×1179; o `CONFIG` sai com o tamanho do modo escolhido, par).
- [ ] **Step 3:** ao ligar, registrar a escala efetiva do monitor virtual (`GetDpiForMonitor` com `MDT_EFFECTIVE_DPI` pelo `HMONITOR` do monitor; 96 = 100%) e, se ≠ 100%, um evento `ScaleNotNative` que a bandeja mostra num balão uma vez ("Para o programa do trabalho funcionar em todas as telas, deixe o monitor do iPhone em 100%: Configurações → Tela → monitor do iPhone → Escala 100%"). Não mudar a escala por conta própria.
- [ ] **Step 4:** bancada: opção `--monitor-size LxA` (padrão: `VirtualMonitorSize.For(2556,1179)` = 1460×674, igual ao app); o relatório mostra o tamanho usado.
- [ ] **Step 5:** `dotnet test` verde, 0 avisos; commit `feat: monitor do iPhone em 100% com a resolução ÷ 1,75 (programas antigos e menos atraso)`; push; `windows-test` verde.
- [ ] **Step 6 (usuário):** rodar `tools\vdd\instalar-vdd.ps1` como administrador de novo (modos novos no driver).

---

### Task 4: Medir e escolher (PC do trabalho, Intel)

**Files:**
- Create: `docs/medicoes/2026-10-21-bancada-5d.md`

- [ ] **Rodada 2 (depois da Task 3b e do instalador):** `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- --variants base,veryfast-scale1920,veryfast-scale2of3,x264,mf,fps30 --out builds/bancada-rodada-2` no monitor 1460×674 (padrão novo); comparar com a matriz 1 (2556×1178).
- [ ] **Step 1 (combinar com o usuário: ~10 min sem mexer no PC):** `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- --seconds 15` (todas as variantes × 3 cenários, monitor virtual).
- [ ] **Step 2:** Escrever no `docs/medicoes/2026-10-21-bancada-5d.md` a tabela completa (do `resultados.csv`), as condições (PC, placa, driver, FFmpeg, resolução) e a comparação de cada variante com a `base`.
- [ ] **Step 3:** Montar 1–3 combinações com as variantes que ajudaram (ex.: `lowpower+cbr+nodup`) como variantes novas em `BenchVariants` (commit pequeno), rodar só elas + `base` de novo, e registrar.
- [ ] **Step 4:** Aplicar a **regra de escolha** (Global Constraints) e escrever a decisão com os números e o `.png` da rolagem conferido (texto legível com a escala de 175%). Se nenhuma atingir as metas da bancada, registrar a melhor e o quanto falta, e seguir (a foto do Task 6 decide).
- [ ] **Step 5: Commit** — `docs: medições da bancada da Etapa 5d e a configuração escolhida`; push.

---

### Task 5: Aplicar a configuração vencedora no TelaDois

**Files:**
- Modify: `windows/src/TelaDois.Core/Video/EncoderProfiles.cs` e/ou o `EncodeSettings` usado pela `VideoFeedFactory`/`EncoderProbe`/`FfmpegCapture`; testes correspondentes.

**Interfaces:**
- Consumes: a decisão do Task 4 (`docs/medicoes/2026-10-21-bancada-5d.md`).

- [ ] **Step 1: Testes** que fixam a linha nova do perfil Intel (`qsv-hwmap`, e o `qsv-download` se mudar) exatamente como a variante vencedora; se a vencedora reduz o tamanho, a redução é **proporcional ao HELLO** (`ScaleTo` calculado da resolução do iPhone na mesma razão da vencedora, sempre par — ex. 2/3 de 2556×1179 = 1704×786) e o `CONFIG` sai com esse tamanho (teste na `VideoFeedFactory`: o `VideoConfig` do feed = tamanho reduzido).
- [ ] **Step 2: AMD (seguro):** nos perfis `amf-d3d11`/`amf-download`, só os ajustes de taxa/GOP equivalentes da vencedora (ex.: taxa constante com folga pequena), sem redução de tamanho; os outros perfis ficam como estão. Teste da linha AMF.
- [ ] **Step 3:** vermelho → implementar → suíte verde, 0 avisos; o `EncoderProbe` sonda com a linha nova.
- [ ] **Step 4:** rodar a bancada de novo só com a configuração do app (`--variants base` agora deve refletir o novo padrão) e anotar no `docs/medicoes/…` que o app produz os mesmos números.
- [ ] **Step 5: Commit** — `perf(windows): perfil Intel com a configuração de menor atraso da bancada`; push; `windows-test` verde.

---

### Task 6: Confirmação no iPhone e documentação

**Files:**
- Modify: `CLAUDE.md` (Estado atual, Decisões, Como rodar — bancada), `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8), `docs/medicoes/2026-10-21-bancada-5d.md`

- [ ] **Step 1:** Publicar o PC (`dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows-5d`); conferir se o iPhone já está com a versão do último `ios-build` verde (0.5.0, `a63bed0`) — se não, baixar, inspecionar (Python isolado) e instalar pelo xtool numa aba do Terminal.
- [ ] **Step 2 (com o usuário):** abrir um cronômetro com milissegundos (ex.: https://www.online-stopwatch.com/full-screen-stopwatch/) **no monitor do iPhone**, deixar uma cópia visível no monitor principal, e o usuário tira 2–3 fotos com outra câmera pegando os dois; anotar a diferença de cada foto. Depois o uso: mouse parado → mexer, rolagem de site, podcast (boca × som). Guardar no registro do PC a linha `vídeo/s` do período.
- [ ] **Step 3:** No `docs/medicoes/…`: ponta a ponta medido, conta do iPhone (**ponta a ponta − bancada − ~3 ms**) e a recomendação para a 5e (atraso do som a aplicar, ou "não precisa" se ≤ ~45 ms) e para a troca da exibição no iPhone (se a parte do iPhone > ~35 ms).
- [ ] **Step 4:** Atualizar `CLAUDE.md` (5d feita, números, como rodar a bancada: `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- [--monitor …]`, decisão nova na tabela) e o §8 da spec geral; `graphify update .`; commit e push; `ios-test`, `ios-build` e `windows-test` verdes no SHA final.
