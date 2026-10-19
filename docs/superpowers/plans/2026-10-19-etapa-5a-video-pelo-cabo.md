# Etapa 5a — Vídeo pelo cabo: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O PC transmite um monitor existente para o iPhone pelo túnel USB (FFmpeg `ddagrab` → H.264 → `VIDEO`), o iPhone mostra em tela cheia e volta ao Modo Descanso ao desconectar.

**Architecture:** No PC, `TelaDois.Core/Video/` escolhe o monitor (DXGI), testa as compressões (`EncoderProbe`), roda o FFmpeg (`FfmpegCapture`), corta o fluxo em imagens (`AccessUnitReader`) e a `LinkSession` manda `CONFIG` + `VIDEO`. No iPhone, `TelaDoisCore.H264Parser` (puro, testado) prepara as imagens e o `VideoRenderer` entrega ao `AVSampleBufferDisplayLayer`; a tela raiz troca para a Segunda Tela ao receber `CONFIG`.

**Tech Stack:** C# / .NET 10 + xUnit (PC), FFmpeg 9 (`ddagrab`, `h264_amf`/`h264_qsv`/`h264_nvenc`/`h264_mf`/`libx264`), Swift 6 (pacote) / Swift 5 + concorrência estrita (app), AVFoundation/CoreMedia, XCUITest, GitHub Actions.

Especificação: `docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md`. Antes de grep/leitura de código, consulte o grafo: `uv tool run --from graphifyy python -m graphify query "<pergunta>"`.

## Global Constraints

- Branch **`etapa-5a-video`**; só juntar ao `main` (fast-forward) com `ios-test`, `ios-build` **e** `windows-test` verdes no SHA final.
- Não existe Mac: nunca rodar `xcodebuild`/`xcodegen`/`swift` localmente; código iOS verificado só pelo CI. Código do PC: `dotnet test windows/TelaDois.sln` local **e** `windows-test` no CI.
- Protocolo mudou nos dois lados ⇒ bytes de referência iguais em Swift e C#: `CONFIG(1366, 768, 60)` = `020000000A0000055600000300003C`; `VIDEO(completa, 1000 µs, 00 00 00 01 09 F0)` = `030000000F0100000000000003E80000000109F0`. Referências antigas continuam: `HELLO(2556,1179)` = `010000000A0001000009FC0000049B`; `PING(1234567890123)` = `05000000080000011F71FB04CB`.
- Qualidade: até **60 fps**, `-bf 0`, `-g 120` (imagem completa a cada 2 s), `-b:v 12M -maxrate 20M -bufsize 4M`, `-bsf:v h264_metadata=aud=insert`, saída `-f h264 pipe:1`.
- Ordem de compressões: `h264_amf` → `h264_qsv` → `h264_nvenc` → `h264_mf` → `libx264` (primeira que codificar 1 imagem vence).
- Índice do `ddagrab` = índice de saída DXGI do adaptador 0 (neste PC: 0 = `\\.\DISPLAY2` 1920×1080 principal, 1 = `\\.\DISPLAY1` 1366×768). Padrão: primeiro monitor **não principal**, senão o principal.
- Fila de envio limitada por tempo: descarta quando a imagem mais antiga esperando passa de 100 ms (rajadas curtas passam inteiras; teto de 240 imagens) e então descarta até a próxima completa; uma linha no log por episódio. FFmpeg morreu, não iniciou ou a leitura falhou ⇒ registra e reinicia após 1 s, 2 s, 5 s (zera ao voltar a receber imagens). Encerrar a captura sempre termina: mata o processo (destrava a leitura do pipe) e cancela quem espera imagem. *(Correção da Task 5, aprovada pelo usuário em 2026-10-19; substitui "3 imagens". O código da Task 5 abaixo é o original — o atual está em `FrameQueue.cs`/`FfmpegCapture.cs`.)*
- iPhone: Segunda Tela em tela cheia, aspecto preservado, fundo preto, **sem escurecer** enquanto ativa; desconectar ⇒ Descanso em < 1 s.
- Zero avisos: C# (`TreatWarningsAsErrors`) e Swift (`SWIFT_STRICT_CONCURRENCY: complete`).
- Textos para o usuário em português; identificadores em inglês. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- No Windows não edite arquivos com PowerShell `Get-Content`/`Set-Content` (corrompe UTF-8). gh: `"/c/Program Files/GitHub CLI/gh.exe"`.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `windows/src/TelaDois.Core/Link/LinkMessages.cs` | + `VideoConfig`, `Config`/`TryParseConfig`, `Video`/`TryParseVideo` |
| `windows/src/TelaDois.Core/Link/LinkSession.cs` | + envio de `CONFIG`/`VIDEO` a partir de um `IVideoFeed` |
| `windows/src/TelaDois.Core/Video/H264.cs` | Tipos de NAL e varredura de códigos de início |
| `windows/src/TelaDois.Core/Video/AccessUnitReader.cs` | Stream Annex-B → imagens (`AccessUnit`) |
| `windows/src/TelaDois.Core/Video/FrameQueue.cs` | Fila limitada por tempo (100 ms) com descarte até a próxima completa |
| `windows/src/TelaDois.Core/Video/IVideoFeed.cs` | Contrato da fonte de vídeo da sessão |
| `windows/src/TelaDois.Core/Video/EncoderProfiles.cs` | Perfis de compressão (filtro + opções) |
| `windows/src/TelaDois.Core/Video/FfmpegArguments.cs` | Linha de comando do FFmpeg |
| `windows/src/TelaDois.Core/Video/ProcessRunner.cs` | Abstração de processo + implementação real |
| `windows/src/TelaDois.Core/Video/FfmpegLocator.cs` | Acha o `ffmpeg.exe` |
| `windows/src/TelaDois.Core/Video/EncoderProbe.cs` | Escolhe a compressão |
| `windows/src/TelaDois.Core/Video/DisplayCatalog.cs` | Monitores via DXGI + escolha |
| `windows/src/TelaDois.Core/Video/FfmpegCapture.cs` | Processo do FFmpeg → `FrameQueue` (é o `IVideoFeed`) |
| `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs` | Junta tudo ao abrir uma sessão |
| `windows/src/TelaDois.Core/UsbTunnelService.cs`, `TunnelStatusText.cs` | Repassa o vídeo e mostra "transmitindo" |
| `windows/src/TelaDois.Tray/TrayContext.cs`, `TraySettings.cs` | Menu "Monitor transmitido", aviso de FFmpeg ausente |
| `windows/tests/TelaDois.Core.Tests/Video/*.cs`, `Fakes/FakeProcessRunner.cs` | Testes do vídeo do PC |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift` | + `LinkVideoConfig`, `LinkVideoFrame`, `config`/`parseConfig`, `video`/`parseVideo` |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift` | NALs, imagens, AVCC |
| `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/Fixtures/bars-64x64.h264` | Vídeo de teste (4 imagens) — também usado pelo C# |
| `ios/TelaDois/VideoRenderer.swift`, `SecondScreenView.swift` | Decodificação/exibição |
| `ios/TelaDois/UsbLink.swift`, `RestScreenView.swift` | `CONFIG`/`VIDEO` e troca de tela |
| `ios/TelaDoisUITests/Fixtures/bars-640x360.h264`, `RestScreenUITests.swift` | Teste de UI da Segunda Tela |

---

### Task 1: Protocolo `CONFIG` e `VIDEO` nos dois lados

**Files:**
- Modify: `windows/src/TelaDois.Core/Link/LinkMessages.cs`, `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs`
- Modify: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift`, `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`

**Interfaces:**
- Produces (C#): `public readonly record struct VideoConfig(uint Width, uint Height, ushort Fps)`; `LinkMessages.Config(VideoConfig)`, `LinkMessages.TryParseConfig(LinkFrame, out VideoConfig)`, `LinkMessages.Video(bool keyframe, ulong timestampMicros, ReadOnlySpan<byte> accessUnit)`, `LinkMessages.TryParseVideo(LinkFrame, out bool keyframe, out ulong timestampMicros, out ReadOnlyMemory<byte> accessUnit)`.
- Produces (Swift): `public struct LinkVideoConfig: Equatable, Sendable { width: UInt32, height: UInt32, fps: UInt16 }`, `public struct LinkVideoFrame: Equatable, Sendable { isKeyframe: Bool, timestamp: UInt64, accessUnit: [UInt8] }`, `LinkProtocol.config(width:height:fps:)`, `LinkProtocol.parseConfig(_:) -> LinkVideoConfig?`, `LinkProtocol.video(keyframe:timestamp:accessUnit:)`, `LinkProtocol.parseVideo(_:) -> LinkVideoFrame?`.

- [ ] **Step 1: Criar o branch**

```bash
cd "/c/Pedro/Arquivos Pedro/Aplicativo celular" && git switch main && git pull -q && git switch -c etapa-5a-video
```

- [ ] **Step 2: Testes C# (vão falhar na compilação)** — acrescente a `LinkProtocolTests`:

```csharp
    [Fact]
    public void ConfigMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("020000000A0000055600000300003C"), LinkMessages.Config(new VideoConfig(1366, 768, 60)).Encode());

    [Fact]
    public void VideoMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("030000000F0100000000000003E80000000109F0"),
            LinkMessages.Video(true, 1000, Convert.FromHexString("0000000109F0")).Encode());

    [Fact]
    public void ParsesConfigAndVideo()
    {
        Assert.True(LinkMessages.TryParseConfig(LinkMessages.Config(new VideoConfig(2556, 1179, 30)), out var config));
        Assert.Equal(new VideoConfig(2556, 1179, 30), config);
        Assert.True(LinkMessages.TryParseVideo(LinkMessages.Video(false, 42, [1, 2, 3]), out var keyframe, out var timestamp, out var data));
        Assert.False(keyframe);
        Assert.Equal(42UL, timestamp);
        Assert.Equal(new byte[] { 1, 2, 3 }, data.ToArray());
    }

    [Fact]
    public void RejectsMalformedConfigAndVideo()
    {
        Assert.False(LinkMessages.TryParseConfig(new LinkFrame(LinkMessageType.Config, [0, 1]), out _));
        Assert.False(LinkMessages.TryParseVideo(new LinkFrame(LinkMessageType.Video, [1, 0, 0]), out _, out _, out _));
        Assert.False(LinkMessages.TryParseVideo(LinkMessages.Ping(1), out _, out _, out _));
    }
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação (`VideoConfig`/`Config` não existem).

- [ ] **Step 3: Implementar em `LinkMessages.cs`** — acrescente depois de `HelloInfo`:

```csharp
/// <summary>Formato do vídeo transmitido: resolução do monitor capturado e quadros por segundo.</summary>
public readonly record struct VideoConfig(uint Width, uint Height, ushort Fps);
```

e dentro de `LinkMessages`:

```csharp
    public static LinkFrame Config(VideoConfig config)
    {
        var payload = new byte[10];
        BinaryPrimitives.WriteUInt32BigEndian(payload, config.Width);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4), config.Height);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(8), config.Fps);
        return new LinkFrame(LinkMessageType.Config, payload);
    }

    public static bool TryParseConfig(LinkFrame frame, out VideoConfig config)
    {
        config = default;
        if (frame.MessageType != LinkMessageType.Config || frame.Payload.Length != 10)
        {
            return false;
        }
        var payload = frame.Payload.AsSpan();
        config = new VideoConfig(
            BinaryPrimitives.ReadUInt32BigEndian(payload),
            BinaryPrimitives.ReadUInt32BigEndian(payload[4..]),
            BinaryPrimitives.ReadUInt16BigEndian(payload[8..]));
        return true;
    }

    /// <summary>Uma imagem H.264 (Annex-B). <paramref name="timestampMicros"/> = µs desde o início da transmissão.</summary>
    public static LinkFrame Video(bool keyframe, ulong timestampMicros, ReadOnlySpan<byte> accessUnit)
    {
        var payload = new byte[9 + accessUnit.Length];
        payload[0] = keyframe ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(1), timestampMicros);
        accessUnit.CopyTo(payload.AsSpan(9));
        return new LinkFrame(LinkMessageType.Video, payload);
    }

    public static bool TryParseVideo(LinkFrame frame, out bool keyframe, out ulong timestampMicros, out ReadOnlyMemory<byte> accessUnit)
    {
        keyframe = false;
        timestampMicros = 0;
        accessUnit = ReadOnlyMemory<byte>.Empty;
        if (frame.MessageType != LinkMessageType.Video || frame.Payload.Length < 9)
        {
            return false;
        }
        keyframe = (frame.Payload[0] & 1) != 0;
        timestampMicros = BinaryPrimitives.ReadUInt64BigEndian(frame.Payload.AsSpan(1));
        accessUnit = frame.Payload.AsMemory(9);
        return true;
    }
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS (todos os testes, incluindo os 4 novos).

- [ ] **Step 4: Testes Swift** — acrescente a `LinkProtocolTests.swift`:

```swift
    func testConfigMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.config(width: 1366, height: 768, fps: 60).encoded(), hex("020000000A0000055600000300003C"))
    }

    func testVideoMatchesGoldenBytes() {
        XCTAssertEqual(
            LinkProtocol.video(keyframe: true, timestamp: 1000, accessUnit: hex("0000000109F0")).encoded(),
            hex("030000000F0100000000000003E80000000109F0")
        )
    }

    func testParsesConfigAndVideo() {
        XCTAssertEqual(
            LinkProtocol.parseConfig(LinkProtocol.config(width: 2556, height: 1179, fps: 30)),
            LinkVideoConfig(width: 2556, height: 1179, fps: 30)
        )
        XCTAssertEqual(
            LinkProtocol.parseVideo(LinkProtocol.video(keyframe: false, timestamp: 42, accessUnit: [1, 2, 3])),
            LinkVideoFrame(isKeyframe: false, timestamp: 42, accessUnit: [1, 2, 3])
        )
    }

    func testRejectsMalformedConfigAndVideo() {
        XCTAssertNil(LinkProtocol.parseConfig(LinkFrame(.config, payload: [0, 1])))
        XCTAssertNil(LinkProtocol.parseVideo(LinkFrame(.video, payload: [1, 0, 0])))
        XCTAssertNil(LinkProtocol.parseVideo(LinkProtocol.ping(1)))
    }
```

- [ ] **Step 5: Implementar em `LinkProtocol.swift`** — acrescente depois de `LinkHello`:

```swift
/// Formato do vídeo transmitido pelo PC.
public struct LinkVideoConfig: Equatable, Sendable {
    public let width: UInt32
    public let height: UInt32
    public let fps: UInt16

    public init(width: UInt32, height: UInt32, fps: UInt16) {
        self.width = width
        self.height = height
        self.fps = fps
    }
}

/// Uma imagem H.264 (Annex-B); `timestamp` em µs desde o início da transmissão.
public struct LinkVideoFrame: Equatable, Sendable {
    public let isKeyframe: Bool
    public let timestamp: UInt64
    public let accessUnit: [UInt8]

    public init(isKeyframe: Bool, timestamp: UInt64, accessUnit: [UInt8]) {
        self.isKeyframe = isKeyframe
        self.timestamp = timestamp
        self.accessUnit = accessUnit
    }
}
```

e dentro de `public enum LinkProtocol`:

```swift
    public static func config(width: UInt32, height: UInt32, fps: UInt16) -> LinkFrame {
        LinkFrame(.config, payload: bigEndianBytes(width) + bigEndianBytes(height) + bigEndianBytes(fps))
    }

    public static func parseConfig(_ frame: LinkFrame) -> LinkVideoConfig? {
        guard frame.messageType == .config, frame.payload.count == 10 else { return nil }
        let bytes = frame.payload[...]
        return LinkVideoConfig(
            width: readBigEndian(bytes[0..<4]),
            height: readBigEndian(bytes[4..<8]),
            fps: readBigEndian(bytes[8..<10])
        )
    }

    public static func video(keyframe: Bool, timestamp: UInt64, accessUnit: [UInt8]) -> LinkFrame {
        LinkFrame(.video, payload: [keyframe ? 1 : 0] + bigEndianBytes(timestamp) + accessUnit)
    }

    public static func parseVideo(_ frame: LinkFrame) -> LinkVideoFrame? {
        guard frame.messageType == .video, frame.payload.count >= 9 else { return nil }
        let bytes = frame.payload[...]
        return LinkVideoFrame(
            isKeyframe: bytes[0] & 1 != 0,
            timestamp: readBigEndian(bytes[1..<9]),
            accessUnit: Array(bytes[9...])
        )
    }
```

- [ ] **Step 6: Commit, push e CI**

```bash
git add windows ios/Packages
git commit -m "feat(protocolo): CONFIG e VIDEO nos dois lados com bytes de referência"
git push -u origin etapa-5a-video
```

Expected: `windows-test` e `ios-test` (passo "Testes do pacote") verdes no SHA. Acompanhe pelo headSha com `gh run list --branch etapa-5a-video --json databaseId,workflowName,headSha,conclusion` e `gh run watch <id> --exit-status`.

---

### Task 2: Vídeo de teste e leitura de H.264 nos dois lados

**Files:**
- Create: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/Fixtures/bars-64x64.h264`, `ios/TelaDoisUITests/Fixtures/bars-640x360.h264`
- Modify: `ios/Packages/TelaDoisCore/Package.swift`
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift`, `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/H264ParserTests.swift`
- Create: `windows/src/TelaDois.Core/Video/H264.cs`, `windows/src/TelaDois.Core/Video/AccessUnitReader.cs`, `windows/tests/TelaDois.Core.Tests/Video/AccessUnitReaderTests.cs`
- Modify: `windows/tests/TelaDois.Core.Tests/TelaDois.Core.Tests.csproj`

**Interfaces:**
- Produces (Swift): `public struct H264NALUnit: Equatable, Sendable { public let type: UInt8; public let bytes: [UInt8] }` (bytes sem código de início; `isVCL` = tipos 1…5); `public enum H264NALType { aud = 9, sps = 7, pps = 8, idr = 5 }` (constantes `UInt8`); `H264Parser.nalUnits(in: [UInt8]) -> [H264NALUnit]`, `H264Parser.accessUnits(in: [UInt8]) -> [[UInt8]]`, `H264Parser.avcc(_ nals: [H264NALUnit]) -> [UInt8]`, `H264Parser.containsIDR(_ nals: [H264NALUnit]) -> Bool`.
- Produces (C#): `public sealed record AccessUnit(byte[] Data, bool IsKeyframe)`; `public sealed class AccessUnitReader(Stream stream)` com `IAsyncEnumerable<AccessUnit> ReadAllAsync(CancellationToken ct = default)`; `H264.ScanNals(byte[] data, int count)` (interno).

- [ ] **Step 1: Gerar os vídeos de teste** (FFmpeg instalado pelo winget; estrutura conferida em 2026-10-19)

```bash
cd "/c/Pedro/Arquivos Pedro/Aplicativo celular"
mkdir -p ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/Fixtures ios/TelaDoisUITests/Fixtures
ffmpeg -hide_banner -loglevel error -y -f lavfi -i "testsrc2=size=64x64:rate=30" -frames:v 4 -c:v libx264 -preset ultrafast -tune zerolatency -x264-params "sliced-threads=0:threads=1" -bf 0 -g 120 -pix_fmt yuv420p -bsf:v h264_metadata=aud=insert -f h264 ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/Fixtures/bars-64x64.h264
ffmpeg -hide_banner -loglevel error -y -f lavfi -i "smptehdbars=size=640x360:rate=30" -frames:v 30 -c:v libx264 -preset ultrafast -tune zerolatency -x264-params "sliced-threads=0:threads=1" -bf 0 -g 120 -pix_fmt yuv420p -bsf:v h264_metadata=aud=insert -f h264 ios/TelaDoisUITests/Fixtures/bars-640x360.h264
```

Expected (confira com Python isolado): `bars-64x64.h264` tem os tipos de NAL `[9, 7, 8, 6, 5, 9, 1, 9, 1, 9, 1]` (4 imagens; só a primeira com IDR, SPS e PPS); `bars-640x360.h264` tem 30 AUD e 1 IDR. Os dois arquivos têm ~4 KB.

- [ ] **Step 2: Testes Swift do parser (vão falhar)** — `Package.swift`, troque o `testTarget` por:

```swift
        .testTarget(name: "TelaDoisCoreTests", dependencies: ["TelaDoisCore"], resources: [.copy("Fixtures")]),
```

e crie `H264ParserTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class H264ParserTests: XCTestCase {
    private func fixture() throws -> [UInt8] {
        let url = try XCTUnwrap(Bundle.module.url(forResource: "bars-64x64", withExtension: "h264", subdirectory: "Fixtures"))
        return try [UInt8](Data(contentsOf: url))
    }

    func testSplitsThreeAndFourByteStartCodes() {
        let stream: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0, 0, 0, 1, 0x65, 0xAA, 0xBB]
        XCTAssertEqual(H264Parser.nalUnits(in: stream), [
            H264NALUnit(type: 9, bytes: [0x09, 0xF0]),
            H264NALUnit(type: 5, bytes: [0x65, 0xAA, 0xBB]),
        ])
    }

    func testFixtureNalTypes() throws {
        XCTAssertEqual(H264Parser.nalUnits(in: try fixture()).map(\.type), [9, 7, 8, 6, 5, 9, 1, 9, 1, 9, 1])
    }

    func testAccessUnitsSkipEmptyOnesAndKeepOnlyFirstAsKeyframe() throws {
        // AUD duplicado (como o h264_mf gera) não vira imagem vazia.
        let doubledAud = [UInt8]([0, 0, 0, 1, 0x09, 0xF0]) + (try fixture())
        let units = H264Parser.accessUnits(in: doubledAud)
        XCTAssertEqual(units.count, 4)
        XCTAssertEqual(units.map { H264Parser.containsIDR(H264Parser.nalUnits(in: $0)) }, [true, false, false, false])
    }

    func testAvccDropsParameterSetsAndPrefixesLengths() throws {
        let first = H264Parser.nalUnits(in: H264Parser.accessUnits(in: try fixture())[0])
        let avcc = H264Parser.avcc(first)
        let kept = first.filter { ![H264NALType.aud, H264NALType.sps, H264NALType.pps].contains($0.type) }
        XCTAssertEqual(kept.map(\.type), [6, 5])
        XCTAssertEqual(avcc.count, kept.reduce(0) { $0 + 4 + $1.bytes.count })
        let firstLength = Int(avcc[0]) << 24 | Int(avcc[1]) << 16 | Int(avcc[2]) << 8 | Int(avcc[3])
        XCTAssertEqual(firstLength, kept[0].bytes.count)
    }
}
```

- [ ] **Step 3: Implementar `H264Parser.swift`**

```swift
/// Tipos de NAL do H.264 usados na transmissão.
public enum H264NALType {
    public static let idr: UInt8 = 5
    public static let sps: UInt8 = 7
    public static let pps: UInt8 = 8
    public static let aud: UInt8 = 9
}

/// Uma unidade NAL sem o código de início.
public struct H264NALUnit: Equatable, Sendable {
    public let type: UInt8
    public let bytes: [UInt8]

    public init(type: UInt8, bytes: [UInt8]) {
        self.type = type
        self.bytes = bytes
    }

    /// NAL com dados de imagem (fatias 1…5).
    public var isVCL: Bool { (1...5).contains(type) }
}

/// Lê H.264 Annex-B (códigos de início `00 00 01` / `00 00 00 01`) como o FFmpeg do PC entrega.
public enum H264Parser {
    public static func nalUnits(in stream: [UInt8]) -> [H264NALUnit] {
        var starts: [(code: Int, payload: Int)] = []
        var index = 0
        while index + 2 < stream.count {
            if stream[index] == 0, stream[index + 1] == 0, stream[index + 2] == 1 {
                let code = index > 0 && stream[index - 1] == 0 ? index - 1 : index
                starts.append((code, index + 3))
                index += 3
            } else {
                index += 1
            }
        }
        var units: [H264NALUnit] = []
        for (position, start) in starts.enumerated() {
            let end = position + 1 < starts.count ? starts[position + 1].code : stream.count
            guard start.payload < end else { continue }
            let bytes = Array(stream[start.payload..<end])
            units.append(H264NALUnit(type: bytes[0] & 0x1F, bytes: bytes))
        }
        return units
    }

    /// Imagens (unidades de acesso) separadas pelos AUD, em Annex-B; descarta as que não têm dados de imagem.
    public static func accessUnits(in stream: [UInt8]) -> [[UInt8]] {
        var result: [[UInt8]] = []
        var current: [H264NALUnit] = []
        func close() {
            if current.contains(where: \.isVCL) { result.append(annexB(current)) }
            current = []
        }
        for nal in nalUnits(in: stream) {
            if nal.type == H264NALType.aud { close() }
            current.append(nal)
        }
        close()
        return result
    }

    /// Formato do `CMSampleBuffer`: cada NAL com tamanho de 4 bytes; sem AUD, SPS e PPS (vão no formato).
    public static func avcc(_ nals: [H264NALUnit]) -> [UInt8] {
        var bytes: [UInt8] = []
        for nal in nals where ![H264NALType.aud, H264NALType.sps, H264NALType.pps].contains(nal.type) {
            bytes += bigEndianBytes(UInt32(nal.bytes.count)) + nal.bytes
        }
        return bytes
    }

    public static func containsIDR(_ nals: [H264NALUnit]) -> Bool {
        nals.contains { $0.type == H264NALType.idr }
    }

    private static func annexB(_ nals: [H264NALUnit]) -> [UInt8] {
        nals.flatMap { [0, 0, 0, 1] + $0.bytes }
    }
}
```

- [ ] **Step 4: Testes C# do leitor (vão falhar)** — no `TelaDois.Core.Tests.csproj`, acrescente:

```xml
  <ItemGroup>
    <None Include="..\..\..\ios\Packages\TelaDoisCore\Tests\TelaDoisCoreTests\Fixtures\bars-64x64.h264" Link="Fixtures\bars-64x64.h264" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

e crie `windows/tests/TelaDois.Core.Tests/Video/AccessUnitReaderTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class AccessUnitReaderTests
{
    internal static byte[] Fixture() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bars-64x64.h264"));

    private static async Task<List<AccessUnit>> ReadAll(Stream stream)
    {
        var units = new List<AccessUnit>();
        await foreach (var unit in new AccessUnitReader(stream).ReadAllAsync())
        {
            units.Add(unit);
        }
        return units;
    }

    [Fact]
    public async Task SplitsFixtureIntoFourFramesWithOnlyFirstKeyframe()
    {
        var units = await ReadAll(new MemoryStream(Fixture()));
        Assert.Equal(4, units.Count);
        Assert.Equal(new[] { true, false, false, false }, units.Select(u => u.IsKeyframe));
        Assert.Equal(Fixture().Length, units.Sum(u => u.Data.Length));
    }

    [Fact]
    public async Task HandlesTinyReadsAndDuplicatedAud()
    {
        var bytes = Convert.FromHexString("0000000109F0").Concat(Fixture()).ToArray();
        var units = await ReadAll(new TrickleStream(bytes, 3));
        Assert.Equal(4, units.Count);
        Assert.True(units[0].IsKeyframe);
    }

    [Fact]
    public void ScanFindsThreeAndFourByteStartCodes()
    {
        var data = Convert.FromHexString("0000000109F000000165AABB");
        Assert.Equal(new[] { (0, 9), (6, 5) }, H264.ScanNals(data, data.Length));
    }

    /// <summary>Entrega no máximo <c>chunk</c> bytes por leitura, como um pipe lento.</summary>
    private sealed class TrickleStream(byte[] data, int chunk) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(chunk, buffer.Length)], ct);
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação (`TelaDois.Core.Video` não existe).

- [ ] **Step 5: Implementar `H264.cs` e `AccessUnitReader.cs`**

`windows/src/TelaDois.Core/Video/H264.cs`:

```csharp
namespace TelaDois.Core.Video;

/// <summary>Uma imagem H.264 em Annex-B; <c>IsKeyframe</c> quando contém um NAL IDR.</summary>
public sealed record AccessUnit(byte[] Data, bool IsKeyframe);

internal static class H264
{
    public const int Idr = 5;
    public const int Aud = 9;

    /// <summary>Posição de cada código de início (contando o zero extra do código de 4 bytes) e o tipo do NAL.</summary>
    public static List<(int Start, int Type)> ScanNals(byte[] data, int count)
    {
        var nals = new List<(int, int)>();
        for (var i = 0; i + 3 < count; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1)
            {
                var start = i > 0 && data[i - 1] == 0 ? i - 1 : i;
                nals.Add((start, data[i + 3] & 0x1F));
                i += 2;
            }
        }
        return nals;
    }
}
```

`windows/src/TelaDois.Core/Video/AccessUnitReader.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace TelaDois.Core.Video;

/// <summary>
/// Corta o H.264 que sai do FFmpeg em imagens pelos AUD (inseridos com <c>h264_metadata=aud=insert</c>).
/// Uma imagem só sai quando o próximo AUD chega (ou o fluxo termina); imagens sem fatias são descartadas.
/// </summary>
public sealed class AccessUnitReader(Stream stream)
{
    private byte[] _buffer = new byte[256 * 1024];
    private int _count;

    public async IAsyncEnumerable<AccessUnit> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (true)
        {
            foreach (var unit in TakeComplete(endOfStream: false))
            {
                yield return unit;
            }
            if (_count == _buffer.Length)
            {
                Array.Resize(ref _buffer, _buffer.Length * 2);
            }
            var read = await stream.ReadAsync(_buffer.AsMemory(_count), ct);
            if (read == 0)
            {
                foreach (var unit in TakeComplete(endOfStream: true))
                {
                    yield return unit;
                }
                yield break;
            }
            _count += read;
        }
    }

    private List<AccessUnit> TakeComplete(bool endOfStream)
    {
        var units = new List<AccessUnit>();
        var nals = H264.ScanNals(_buffer, _count);
        var bounds = nals.Where(n => n.Type == H264.Aud).Select(n => n.Start).ToList();
        if (endOfStream && bounds.Count > 0)
        {
            bounds.Add(_count);
        }
        var consumed = 0;
        for (var k = 0; k + 1 < bounds.Count; k++)
        {
            var (from, to) = (bounds[k], bounds[k + 1]);
            var types = nals.Where(n => n.Start >= from && n.Start < to).Select(n => n.Type).ToList();
            if (types.Any(t => t is >= 1 and <= 5))
            {
                units.Add(new AccessUnit(_buffer.AsSpan(from, to - from).ToArray(), types.Contains(H264.Idr)));
            }
            consumed = to;
        }
        if (consumed > 0)
        {
            Buffer.BlockCopy(_buffer, consumed, _buffer, 0, _count - consumed);
            _count -= consumed;
        }
        return units;
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS.

- [ ] **Step 6: Commit, push e CI**

```bash
git add ios/Packages ios/TelaDoisUITests/Fixtures windows
git commit -m "feat(video): vídeo de teste e leitura de H.264 (Swift H264Parser, C# AccessUnitReader)"
git push
```

Expected: `ios-test` (pacote com `H264ParserTests`) e `windows-test` verdes.

---

### Task 3: Perfis de compressão, linha de comando, processos e `EncoderProbe`

**Files:**
- Create: `windows/src/TelaDois.Core/Video/EncoderProfiles.cs`, `FfmpegArguments.cs`, `ProcessRunner.cs`, `FfmpegLocator.cs`, `EncoderProbe.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeProcessRunner.cs`, `windows/tests/TelaDois.Core.Tests/Video/FfmpegArgumentsTests.cs`, `EncoderProbeTests.cs`

**Interfaces:**
- Produces: `public sealed record EncoderProfile(string Id, string Codec, string Filter, IReadOnlyList<string> Options)`; `EncoderProfiles.Candidates` (ordem da spec); `FfmpegArguments.ForMonitor(EncoderProfile, int outputIndex, ushort fps)`, `FfmpegArguments.Encode(EncoderProfile, string inputGraph, ushort fps, int? frames = null)`, `FfmpegArguments.Probe(EncoderProfile, int outputIndex)` → `IReadOnlyList<string>`; `public interface IProcessRunner { IRunningProcess Start(string fileName, IReadOnlyList<string> arguments); }`; `public interface IRunningProcess : IAsyncDisposable { Stream Output { get; } string RecentErrors { get; } Task<int> WaitForExitAsync(CancellationToken ct); }`; `SystemProcessRunner : IProcessRunner`; `FfmpegLocator.Find(string? path = null) -> string?`; `public sealed class EncoderProbe(IProcessRunner runner, string ffmpegPath, Action<string> log)` com `Task<EncoderProfile> ChooseAsync(int outputIndex, CancellationToken ct)` e `TimeSpan ProbeTimeout { get; init; }`.

- [ ] **Step 1: Fake de processo** — `windows/tests/TelaDois.Core.Tests/Fakes/FakeProcessRunner.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Processo de mentira: cada <c>Start</c> consome o próximo roteiro (código de saída + bytes no stdout).</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<IReadOnlyList<string>, FakeProcess> _script;
    public List<IReadOnlyList<string>> Started { get; } = [];
    public int Disposed;

    public FakeProcessRunner(Func<IReadOnlyList<string>, FakeProcess> script) => _script = script;

    public IRunningProcess Start(string fileName, IReadOnlyList<string> arguments)
    {
        lock (Started)
        {
            Started.Add(arguments);
        }
        var process = _script(arguments);
        process.OnDispose = () => Interlocked.Increment(ref Disposed);
        return process;
    }
}

internal sealed class FakeProcess(int exitCode, byte[]? output = null, string errors = "") : IRunningProcess
{
    public Action? OnDispose;
    public Stream Output { get; } = new MemoryStream(output ?? []);
    public string RecentErrors => errors;
    public Task<int> WaitForExitAsync(CancellationToken ct) => Task.FromResult(exitCode);

    public ValueTask DisposeAsync()
    {
        OnDispose?.Invoke();
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Testes (vão falhar)** — `FfmpegArgumentsTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FfmpegArgumentsTests
{
    private static readonly EncoderProfile Qsv = EncoderProfiles.Candidates.Single(p => p.Id == "qsv-hwmap");

    [Fact]
    public void CandidatesFollowSpecOrder() =>
        Assert.Equal(
            new[] { "h264_amf", "h264_amf", "h264_qsv", "h264_qsv", "h264_nvenc", "h264_nvenc", "h264_mf", "libx264" },
            EncoderProfiles.Candidates.Select(p => p.Codec));

    [Fact]
    public void BuildsMonitorCapture()
    {
        var args = FfmpegArguments.ForMonitor(Qsv, 1, 60);
        Assert.Equal(
            "-hide_banner -loglevel warning -f lavfi -i ddagrab=output_idx=1:framerate=60:draw_mouse=1 " +
            "-vf hwmap=derive_device=qsv,format=qsv -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 " +
            "-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M -bsf:v h264_metadata=aud=insert -f h264 pipe:1",
            string.Join(' ', args));
    }

    [Fact]
    public void ProfileWithoutFilterHasNoVf()
    {
        var amf = EncoderProfiles.Candidates.First(p => p.Codec == "h264_amf");
        Assert.DoesNotContain("-vf", FfmpegArguments.ForMonitor(amf, 0, 60));
    }

    [Fact]
    public void ProbeEncodesThreeFramesToNull()
    {
        var args = string.Join(' ', FfmpegArguments.Probe(Qsv, 0));
        Assert.Contains("-i ddagrab=output_idx=0:framerate=60:draw_mouse=1", args);
        Assert.EndsWith("-frames:v 3 -f null -", args);
    }

    [Fact]
    public void FindsFfmpegInPath()
    {
        var dir = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(dir.FullName, "ffmpeg.exe"), "");
        Assert.Equal(Path.Combine(dir.FullName, "ffmpeg.exe"), FfmpegLocator.Find($@"C:\nao-existe;{dir.FullName}"));
        Assert.Null(FfmpegLocator.Find(@"C:\nao-existe"));
    }
}
```

e `EncoderProbeTests.cs`:

```csharp
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class EncoderProbeTests
{
    [Fact]
    public async Task PicksFirstProfileThatEncodesAndCachesIt()
    {
        var log = new List<string>();
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("h264_qsv") ? 0 : 1, errors: "sem placa"));
        var probe = new EncoderProbe(runner, "ffmpeg", log.Add);

        var chosen = await probe.ChooseAsync(1, CancellationToken.None);
        Assert.Equal("h264_qsv", chosen.Codec);
        Assert.Equal(3, runner.Started.Count); // amf, amf, qsv
        Assert.Equal(chosen, await probe.ChooseAsync(1, CancellationToken.None));
        Assert.Equal(3, runner.Started.Count);
        Assert.Contains(log, line => line.Contains("qsv"));
        Assert.Equal(3, runner.Disposed);
    }

    [Fact]
    public async Task FallsBackToSoftware()
    {
        var runner = new FakeProcessRunner(args => new FakeProcess(args.Contains("libx264") ? 0 : 1));
        Assert.Equal("libx264", (await new EncoderProbe(runner, "ffmpeg", _ => { }).ChooseAsync(0, CancellationToken.None)).Codec);
    }

    [Fact]
    public async Task ThrowsWhenNothingWorks()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EncoderProbe(runner, "ffmpeg", _ => { }).ChooseAsync(0, CancellationToken.None));
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação.

- [ ] **Step 3: Implementar** — `EncoderProfiles.cs`:

```csharp
namespace TelaDois.Core.Video;

/// <summary>Uma compressão H.264 e como ligar a captura (superfícies D3D11 do ddagrab) a ela.</summary>
public sealed record EncoderProfile(string Id, string Codec, string Filter, IReadOnlyList<string> Options);

public static class EncoderProfiles
{
    /// <summary>
    /// Ordem da spec. Validados no PC Intel em 2026-10-19: qsv-hwmap, qsv-download, mf, x264.
    /// AMF e NVENC (d3d11 direto e com download) são validados no PC de casa (Task 8).
    /// </summary>
    public static IReadOnlyList<EncoderProfile> Candidates { get; } =
    [
        new("amf-d3d11", "h264_amf", "", ["-usage", "ultralowlatency"]),
        new("amf-download", "h264_amf", "hwdownload,format=bgra,format=nv12", []),
        new("qsv-hwmap", "h264_qsv", "hwmap=derive_device=qsv,format=qsv", ["-low_power", "0", "-look_ahead", "0", "-async_depth", "1"]),
        new("qsv-download", "h264_qsv", "hwdownload,format=bgra,format=nv12", ["-async_depth", "1"]),
        new("nvenc-d3d11", "h264_nvenc", "", ["-preset", "p1", "-tune", "ull"]),
        new("nvenc-download", "h264_nvenc", "hwdownload,format=bgra,format=nv12", ["-preset", "p1", "-tune", "ull"]),
        new("mf", "h264_mf", "hwdownload,format=bgra,format=nv12", ["-rate_control", "cbr", "-scenario", "display_remoting"]),
        new("x264", "libx264", "hwdownload,format=bgra,format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]),
    ];
}
```

`FfmpegArguments.cs`:

```csharp
namespace TelaDois.Core.Video;

public static class FfmpegArguments
{
    public static IReadOnlyList<string> ForMonitor(EncoderProfile profile, int outputIndex, ushort fps) =>
        Encode(profile, DdaGrab(outputIndex, fps), fps);

    public static IReadOnlyList<string> Encode(EncoderProfile profile, string inputGraph, ushort fps, int? frames = null)
    {
        var args = Head(profile, inputGraph);
        args.AddRange(["-bf", "0", "-g", (fps * 2).ToString(), "-b:v", "12M", "-maxrate", "20M", "-bufsize", "4M"]);
        if (frames is { } count)
        {
            args.AddRange(["-frames:v", count.ToString()]);
        }
        args.AddRange(["-bsf:v", "h264_metadata=aud=insert", "-f", "h264", "pipe:1"]);
        return args;
    }

    /// <summary>Codifica 3 imagens do monitor e descarta: código 0 = a compressão funciona neste PC.</summary>
    public static IReadOnlyList<string> Probe(EncoderProfile profile, int outputIndex)
    {
        var args = Head(profile, DdaGrab(outputIndex, 60));
        args.AddRange(["-frames:v", "3", "-f", "null", "-"]);
        return args;
    }

    private static string DdaGrab(int outputIndex, ushort fps) => $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1";

    private static List<string> Head(EncoderProfile profile, string inputGraph)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "warning", "-f", "lavfi", "-i", inputGraph };
        if (profile.Filter.Length > 0)
        {
            args.AddRange(["-vf", profile.Filter]);
        }
        args.AddRange(["-c:v", profile.Codec]);
        args.AddRange(profile.Options);
        return args;
    }
}
```

`ProcessRunner.cs`:

```csharp
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
```

`FfmpegLocator.cs`:

```csharp
namespace TelaDois.Core.Video;

public static class FfmpegLocator
{
    /// <summary>Procura <c>ffmpeg.exe</c> nas pastas do PATH (o winget põe um atalho em WinGet\Links).</summary>
    public static string? Find(string? path = null)
    {
        path ??= Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
```

`EncoderProbe.cs`:

```csharp
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
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS.

- [ ] **Step 4: Conferir com o FFmpeg real neste PC (Intel)** — rode no Git Bash:

```bash
cd "/c/Pedro/Arquivos Pedro/Aplicativo celular"
ffmpeg -hide_banner -loglevel error -f lavfi -i "ddagrab=output_idx=1:framerate=60:draw_mouse=1" -vf "hwmap=derive_device=qsv,format=qsv" -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 -frames:v 3 -f null - ; echo "qsv-hwmap exit=$?"
```

Expected: `qsv-hwmap exit=0`. Anote no relatório.

- [ ] **Step 5: Commit e push**

```bash
git add windows
git commit -m "feat(video): perfis de compressão, linha do FFmpeg, processos e escolha da compressão"
git push
```

Expected: `windows-test` verde.

---

### Task 4: Monitores pela DXGI (`DisplayCatalog`)

**Files:**
- Create: `windows/src/TelaDois.Core/Video/DisplayCatalog.cs`, `windows/tests/TelaDois.Core.Tests/Video/DisplayCatalogTests.cs`
- Modify: `windows/src/TelaDois.Core/TelaDois.Core.csproj` (`AllowUnsafeBlocks`)

**Interfaces:**
- Produces: `public sealed record DisplayOutput(int OutputIndex, string DeviceName, int Width, int Height, bool IsPrimary)`; `DisplayCatalog.Enumerate() -> IReadOnlyList<DisplayOutput>`; `DisplayCatalog.Choose(IReadOnlyList<DisplayOutput> outputs, string? preferredDeviceName) -> DisplayOutput?`.

- [ ] **Step 1: Testes (vão falhar)** — `DisplayCatalogTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class DisplayCatalogTests
{
    private static readonly DisplayOutput Main = new(0, @"\\.\DISPLAY2", 1920, 1080, true);
    private static readonly DisplayOutput Side = new(1, @"\\.\DISPLAY1", 1366, 768, false);

    [Fact]
    public void PrefersSavedMonitor() => Assert.Equal(Main, DisplayCatalog.Choose([Main, Side], @"\\.\DISPLAY2"));

    [Fact]
    public void DefaultsToFirstNonPrimary() => Assert.Equal(Side, DisplayCatalog.Choose([Main, Side], null));

    [Fact]
    public void FallsBackWhenSavedMonitorIsGone() => Assert.Equal(Side, DisplayCatalog.Choose([Main, Side], @"\\.\DISPLAY9"));

    [Fact]
    public void UsesPrimaryWhenItIsTheOnlyOne() => Assert.Equal(Main, DisplayCatalog.Choose([Main], null));

    [Fact]
    public void NoMonitorsGivesNull() => Assert.Null(DisplayCatalog.Choose([], null));

    [Fact]
    public void EnumerateDoesNotThrowAndReportsSaneMonitors()
    {
        // No runner do CI pode não haver monitor; no PC real aparece pelo menos o principal.
        var outputs = DisplayCatalog.Enumerate();
        Assert.All(outputs, o =>
        {
            Assert.StartsWith(@"\\.\DISPLAY", o.DeviceName);
            Assert.True(o.Width > 0 && o.Height > 0);
        });
        Assert.Equal(outputs.Count, outputs.Select(o => o.OutputIndex).Distinct().Count());
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação.

- [ ] **Step 2: Implementar** — no `TelaDois.Core.csproj`, dentro do `PropertyGroup`: `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. Crie `DisplayCatalog.cs`:

```csharp
using System.Runtime.InteropServices;

namespace TelaDois.Core.Video;

/// <summary>Um monitor como o <c>ddagrab</c> o enxerga: índice de saída DXGI do adaptador 0.</summary>
public sealed record DisplayOutput(int OutputIndex, string DeviceName, int Width, int Height, bool IsPrimary);

/// <summary>
/// Lista os monitores pela DXGI. A ordem das saídas DXGI <b>não</b> segue o <c>\\.\DISPLAYn</c>
/// (neste PC: saída 0 = DISPLAY2, saída 1 = DISPLAY1), por isso o índice vem daqui.
/// Usa só o adaptador 0, que é o que o <c>ddagrab</c> abre por padrão.
/// </summary>
public static unsafe class DisplayCatalog
{
    private static readonly Guid IidDxgiFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");

    // IDXGIFactory::EnumAdapters, IDXGIAdapter::EnumOutputs e IDXGIOutput::GetDesc ficam todos no slot 7
    // (3 do IUnknown + 4 do IDXGIObject). Release é o slot 2.
    private const int EnumSlot = 7;
    private const int ReleaseSlot = 2;

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(in Guid riid, out IntPtr factory);

    private struct OutputDesc
    {
        public fixed char DeviceName[32];
        public int Left, Top, Right, Bottom;
        public int AttachedToDesktop;
        public int Rotation;
        public IntPtr Monitor;
    }

    public static IReadOnlyList<DisplayOutput> Enumerate()
    {
        var outputs = new List<DisplayOutput>();
        if (!OperatingSystem.IsWindows() || CreateDXGIFactory1(IidDxgiFactory1, out var factory) < 0)
        {
            return outputs;
        }
        try
        {
            if (Enum(factory, 0, out var adapter) < 0)
            {
                return outputs;
            }
            try
            {
                for (uint index = 0; Enum(adapter, index, out var output) >= 0; index++)
                {
                    try
                    {
                        OutputDesc desc;
                        var getDesc = (delegate* unmanaged[Stdcall]<IntPtr, OutputDesc*, int>)(*(void***)output)[EnumSlot];
                        if (getDesc(output, &desc) >= 0 && desc.AttachedToDesktop != 0)
                        {
                            outputs.Add(new DisplayOutput(
                                (int)index,
                                new string(desc.DeviceName),
                                desc.Right - desc.Left,
                                desc.Bottom - desc.Top,
                                desc.Left == 0 && desc.Top == 0));
                        }
                    }
                    finally
                    {
                        Release(output);
                    }
                }
            }
            finally
            {
                Release(adapter);
            }
        }
        finally
        {
            Release(factory);
        }
        return outputs;
    }

    public static DisplayOutput? Choose(IReadOnlyList<DisplayOutput> outputs, string? preferredDeviceName) =>
        outputs.FirstOrDefault(o => o.DeviceName == preferredDeviceName)
        ?? outputs.FirstOrDefault(o => !o.IsPrimary)
        ?? outputs.FirstOrDefault();

    private static int Enum(IntPtr obj, uint index, out IntPtr result)
    {
        IntPtr value;
        var call = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)(*(void***)obj)[EnumSlot];
        var hr = call(obj, index, &value);
        result = hr >= 0 ? value : IntPtr.Zero;
        return hr;
    }

    private static void Release(IntPtr obj) =>
        ((delegate* unmanaged[Stdcall]<IntPtr, uint>)(*(void***)obj)[ReleaseSlot])(obj);
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS.

- [ ] **Step 3: Conferir neste PC** — num teste rápido de console ou no `dotnet test` com saída (`ITestOutputHelper`), confirme que `Enumerate()` devolve `(0, \\.\DISPLAY2, 1920, 1080, true)` e `(1, \\.\DISPLAY1, 1366, 768, false)`; isso bate com o `ddagrab` (output_idx 0 = 1920×1080, 1 = 1366×768). Anote no relatório.

- [ ] **Step 4: Commit e push**

```bash
git add windows
git commit -m "feat(video): monitores pela DXGI no mesmo índice do ddagrab"
git push
```

Expected: `windows-test` verde.

---

### Task 5: Fila com descarte e captura do FFmpeg (`IVideoFeed`)

**Files:**
- Create: `windows/src/TelaDois.Core/Video/IVideoFeed.cs`, `FrameQueue.cs`, `FfmpegCapture.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Video/FrameQueueTests.cs`, `FfmpegCaptureTests.cs`

**Interfaces:**
- Consumes: `AccessUnit`, `AccessUnitReader` (Task 2), `IProcessRunner`, `FfmpegArguments`, `FfmpegLocator`, `EncoderProfile` (Task 3), `VideoConfig` (Task 1).
- Produces: `public interface IVideoFeed : IAsyncDisposable { VideoConfig Config { get; } Task<AccessUnit> NextAsync(CancellationToken ct); }`; `public sealed class FrameQueue(int capacity = 3)` com `Add(AccessUnit)`, `Task<AccessUnit> TakeAsync(CancellationToken)`, `long Dropped`; `public sealed class FfmpegCapture : IVideoFeed` com construtor `(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, VideoConfig config, Action<string> log, IReadOnlyList<TimeSpan>? backoff = null)` e `long Dropped`.

- [ ] **Step 1: Testes (vão falhar)** — `FrameQueueTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FrameQueueTests
{
    private static AccessUnit Frame(byte id, bool key = false) => new([id], key);

    [Fact]
    public async Task DeliversInOrder()
    {
        var queue = new FrameQueue();
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        Assert.Equal(1, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(2, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
    }

    [Fact]
    public async Task WhenFullDropsUntilNextKeyframe()
    {
        var queue = new FrameQueue(capacity: 3);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        queue.Add(Frame(3));
        queue.Add(Frame(4));        // cheia: descarta 1, 2, 3 e 4
        queue.Add(Frame(5));        // ainda esperando imagem completa: descarta
        queue.Add(Frame(6, true));  // completa: volta a entregar
        queue.Add(Frame(7));
        Assert.Equal(6, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(7, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(5, queue.Dropped);
    }

    [Fact]
    public async Task KeyframeArrivingWhenFullReplacesTheBacklog()
    {
        var queue = new FrameQueue(capacity: 2);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        queue.Add(Frame(3, true));
        Assert.Equal(3, (await queue.TakeAsync(CancellationToken.None)).Data[0]);
        Assert.Equal(2, queue.Dropped);
    }

    [Fact]
    public async Task TakeWaitsAndHonorsCancellation()
    {
        var queue = new FrameQueue();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.TakeAsync(cts.Token));
    }
}
```

e `FfmpegCaptureTests.cs`:

```csharp
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FfmpegCaptureTests
{
    private static readonly VideoConfig Config = new(64, 64, 30);
    private static readonly TimeSpan[] FastBackoff = [TimeSpan.FromMilliseconds(10)];

    [Fact]
    public async Task DeliversFramesAndRestartsWhenFfmpegExits()
    {
        var log = new List<string>();
        var runner = new FakeProcessRunner(_ => new FakeProcess(1, AccessUnitReaderTests.Fixture(), "caiu"));
        await using (var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, line => { lock (log) { log.Add(line); } }, FastBackoff))
        {
            Assert.Equal(Config, capture.Config);
            var frames = new List<AccessUnit>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 8; i++)
            {
                frames.Add(await capture.NextAsync(cts.Token));
            }
            Assert.True(frames[0].IsKeyframe);
            Assert.True(runner.Started.Count >= 2, "deveria ter reiniciado o FFmpeg");
        }
        Assert.Contains(log, line => line.Contains("caiu"));
        Assert.Equal(runner.Started.Count, runner.Disposed);
    }

    [Fact]
    public async Task RealFfmpegProducesFrames()
    {
        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            return; // FFmpeg ausente nesta máquina: o teste com o FFmpeg real só roda onde ele está instalado.
        }
        var profile = new EncoderProfile("teste", "libx264", "format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]);
        var args = FfmpegArguments.Encode(profile, "testsrc2=size=320x180:rate=30", 30, frames: 20);
        await using var capture = new FfmpegCapture(new SystemProcessRunner(), ffmpeg, args, new VideoConfig(320, 180, 30), _ => { });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        // O FFmpeg codifica as 20 imagens mais rápido do que o teste lê: a fila pode descartar algumas.
        // Basta provar que a primeira é completa e que outras chegam.
        var first = await capture.NextAsync(cts.Token);
        Assert.True(first.IsKeyframe);
        Assert.NotEmpty((await capture.NextAsync(cts.Token)).Data);
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação.

- [ ] **Step 2: Implementar** — `IVideoFeed.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Fonte de vídeo de uma sessão: formato fixo e imagens em sequência. Descartar encerra a captura.</summary>
public interface IVideoFeed : IAsyncDisposable
{
    VideoConfig Config { get; }
    Task<AccessUnit> NextAsync(CancellationToken ct);
}
```

`FrameQueue.cs`:

```csharp
namespace TelaDois.Core.Video;

/// <summary>
/// Fila curta entre o FFmpeg e o cabo. Cheia = o cabo está atrasado: joga fora o que está pendente e
/// as próximas imagens até chegar uma completa, para nunca acumular atraso.
/// </summary>
public sealed class FrameQueue(int capacity = 3)
{
    private readonly Queue<AccessUnit> _items = new();
    private readonly SemaphoreSlim _signal = new(0);
    private bool _waitingForKeyframe;
    private long _dropped;

    public long Dropped => Interlocked.Read(ref _dropped);

    public void Add(AccessUnit unit)
    {
        lock (_items)
        {
            if (_waitingForKeyframe && !unit.IsKeyframe)
            {
                Interlocked.Increment(ref _dropped);
                return;
            }
            _waitingForKeyframe = false;
            if (_items.Count >= capacity)
            {
                Interlocked.Add(ref _dropped, _items.Count);
                _items.Clear();
                if (!unit.IsKeyframe)
                {
                    Interlocked.Increment(ref _dropped);
                    _waitingForKeyframe = true;
                    return;
                }
            }
            _items.Enqueue(unit);
        }
        _signal.Release();
    }

    public async Task<AccessUnit> TakeAsync(CancellationToken ct)
    {
        while (true)
        {
            await _signal.WaitAsync(ct);
            lock (_items)
            {
                if (_items.TryDequeue(out var unit))
                {
                    return unit;
                }
            }
        }
    }
}
```

`FfmpegCapture.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Roda o FFmpeg e põe as imagens na <see cref="FrameQueue"/>. Se o processo morrer, reinicia após a espera
/// seguinte de <c>backoff</c> (1 s, 2 s, 5 s), que volta ao início quando chegam imagens de novo.
/// </summary>
public sealed class FfmpegCapture : IVideoFeed
{
    private static readonly TimeSpan[] DefaultBackoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private readonly FrameQueue _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;

    public FfmpegCapture(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, VideoConfig config, Action<string> log, IReadOnlyList<TimeSpan>? backoff = null)
    {
        Config = config;
        var waits = backoff ?? DefaultBackoff;
        _pump = Task.Run(() => PumpAsync(runner, ffmpegPath, arguments, log, waits, _cts.Token));
    }

    public VideoConfig Config { get; }

    public long Dropped => _queue.Dropped;

    public Task<AccessUnit> NextAsync(CancellationToken ct) => _queue.TakeAsync(ct);

    private async Task PumpAsync(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, Action<string> log, IReadOnlyList<TimeSpan> waits, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            var sawFrames = false;
            await using (var process = runner.Start(ffmpegPath, arguments))
            {
                try
                {
                    await foreach (var unit in new AccessUnitReader(process.Output).ReadAllAsync(ct))
                    {
                        sawFrames = true;
                        _queue.Add(unit);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (IOException e)
                {
                    log($"leitura do FFmpeg falhou: {e.Message}");
                }
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                log($"FFmpeg terminou: {process.RecentErrors}");
            }
            attempt = sawFrames ? 0 : attempt;
            var wait = waits[Math.Min(attempt, waits.Count - 1)];
            attempt++;
            try
            {
                await Task.Delay(wait, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _pump;
        }
        catch (OperationCanceledException)
        {
        }
        _cts.Dispose();
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS (neste PC o `RealFfmpegProducesFrames` roda de verdade; registre no relatório se rodou ou se saiu cedo por falta do FFmpeg).

- [ ] **Step 3: Commit e push**

```bash
git add windows
git commit -m "feat(video): fila com descarte e captura do FFmpeg com reinício"
git push
```

Expected: `windows-test` verde.

---

### Task 6: Sessão manda `CONFIG`/`VIDEO`; túnel e bandeja ligados ao vídeo

**Files:**
- Modify: `windows/src/TelaDois.Core/Link/LinkSession.cs`, `windows/src/TelaDois.Core/UsbTunnelService.cs`, `windows/src/TelaDois.Core/TunnelStatusText.cs`
- Create: `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs`, `windows/src/TelaDois.Tray/TraySettings.cs`
- Modify: `windows/src/TelaDois.Tray/TrayContext.cs`
- Test: `windows/tests/TelaDois.Core.Tests/LinkSessionTests.cs`, `windows/tests/TelaDois.Core.Tests/Video/VideoFeedFactoryTests.cs`, `windows/tests/TelaDois.Core.Tests/TunnelStatusTextTests.cs`

**Interfaces:**
- Consumes: `IVideoFeed`, `FfmpegCapture`, `EncoderProbe`, `DisplayCatalog`, `FfmpegArguments`, `IProcessRunner` (Tasks 3–5); `VideoConfig`, `LinkMessages.Config/Video` (Task 1).
- Produces: `LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null)`; `LinkSession.RunAsync(Stream, Action<LinkStatus>, LinkSessionOptions, CancellationToken, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)`; `TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null)`; `UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)`; `public sealed class VideoFeedFactory(string? ffmpegPath, IProcessRunner runner, Func<IReadOnlyList<DisplayOutput>> displays, Func<string?> preferredMonitor, Action<string> log)` com `event Action? FfmpegMissing` e `Task<IVideoFeed?> OpenAsync(HelloInfo hello, CancellationToken ct)`.

- [ ] **Step 1: Testes da sessão (vão falhar)** — em `LinkSessionTests.cs` acrescente:

```csharp
    private sealed class FixedFeed(VideoConfig config, IEnumerable<AccessUnit> frames) : IVideoFeed
    {
        private readonly Queue<AccessUnit> _frames = new(frames);
        public bool Disposed { get; private set; }
        public VideoConfig Config => config;

        public async Task<AccessUnit> NextAsync(CancellationToken ct)
        {
            lock (_frames)
            {
                if (_frames.TryDequeue(out var frame))
                {
                    return frame;
                }
            }
            await Task.Delay(Timeout.Infinite, ct);
            throw new OperationCanceledException(ct);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SendsConfigThenVideoAndDisposesFeed()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var feed = new FixedFeed(new VideoConfig(1366, 768, 60), [new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x65], true), new AccessUnit([0, 0, 0, 1, 9, 0xF0, 0, 0, 0, 1, 0x41], false)]);
        LinkStatus? last = null;
        var run = LinkSession.RunAsync(pc, s => last = s, Fast, cts.Token, (_, _) => Task.FromResult<IVideoFeed?>(feed));

        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        LinkFrame? Next() => reader.NextAsync(cts.Token).GetAwaiter().GetResult();
        var frames = new List<LinkFrame>();
        while (frames.Count(f => f.MessageType == LinkMessageType.Video) < 2)
        {
            frames.Add(Next()!);
        }
        var first = frames.First(f => f.MessageType != LinkMessageType.Ping);
        Assert.True(LinkMessages.TryParseConfig(first, out var config));
        Assert.Equal(new VideoConfig(1366, 768, 60), config);
        var videos = frames.Where(f => f.MessageType == LinkMessageType.Video).ToList();
        Assert.True(LinkMessages.TryParseVideo(videos[0], out var key0, out _, out _));
        Assert.True(LinkMessages.TryParseVideo(videos[1], out var key1, out _, out _));
        Assert.True(key0);
        Assert.False(key1);
        Assert.Equal(new VideoConfig(1366, 768, 60), last?.Video);

        await phone.WriteAsync(LinkMessages.Bye().Encode(), cts.Token);
        Assert.Equal(LinkEndReason.Bye, await run);
        Assert.True(feed.Disposed);
    }

    [Fact]
    public async Task WithoutFeedBehavesLikeBefore()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Bye().Encode()).ToArray());
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None, (_, _) => Task.FromResult<IVideoFeed?>(null)).WaitAsync(TimeSpan.FromSeconds(5)));
    }
```

(acrescente `using TelaDois.Core.Video;` no topo). Crie `TunnelStatusTextTests.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Tests;

public class TunnelStatusTextTests
{
    [Fact]
    public void ShowsStreamingWithMonitorSizeAndLatency()
    {
        var text = TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4), new VideoConfig(1366, 768, 60)));
        Assert.Equal("TelaDois: transmitindo · 1366×768 · 60 fps · 4 ms", text);
        Assert.True(text.Length <= 63);
    }

    [Fact]
    public void ConnectedWithoutVideoKeepsOldText() =>
        Assert.Equal("TelaDois: conectado · 2556×1179 · 4 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(4))));
}
```

e `VideoFeedFactoryTests.cs`:

```csharp
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VideoFeedFactoryTests
{
    private static readonly HelloInfo Hello = new(1, 2556, 1179);
    private static readonly DisplayOutput Main = new(0, @"\\.\DISPLAY2", 1920, 1080, true);
    private static readonly DisplayOutput Side = new(1, @"\\.\DISPLAY1", 1366, 768, false);

    [Fact]
    public async Task WithoutFfmpegReportsMissingAndReturnsNull()
    {
        var missing = 0;
        var factory = new VideoFeedFactory(null, new FakeProcessRunner(_ => new FakeProcess(0)), () => [Main], () => null, _ => { });
        factory.FfmpegMissing += () => missing++;
        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Equal(1, missing);
    }

    [Fact]
    public async Task CapturesChosenMonitorWithProbedEncoder()
    {
        var runner = new FakeProcessRunner(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, AccessUnitReaderTests.Fixture()));
        var factory = new VideoFeedFactory("ffmpeg", runner, () => [Main, Side], () => null, _ => { });
        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(feed);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed!.Config);
        await feed.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        var capture = runner.Started.Last(a => !a.Contains("-frames:v"));
        Assert.Contains("ddagrab=output_idx=1:framerate=60:draw_mouse=1", capture);
        Assert.Contains("h264_mf", capture);
    }

    [Fact]
    public async Task NoMonitorReturnsNull() =>
        Assert.Null(await new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(0)), () => [], () => null, _ => { }).OpenAsync(Hello, CancellationToken.None));
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: erro de compilação.

- [ ] **Step 2: Implementar a sessão** — em `LinkSession.cs`:
  1. `using TelaDois.Core.Video;` no topo.
  2. Troque o record por `public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null);`
  3. Assinatura: `public static async Task<LinkEndReason> RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)`.
  4. Declare junto de `receiving`: `IVideoFeed? feed = null;` e `Task? sending = null;`.
  5. Troque o trecho entre `var status = new LinkStatus(hello.Value, null);` e `onStatus(status);` por:

```csharp
            var status = new LinkStatus(hello.Value, null);
            if (openVideo is not null && (feed = await openVideo(hello.Value, cts.Token)) is not null)
            {
                status = status with { Video = feed.Config };
                await SendAsync(LinkMessages.Config(feed.Config), cts.Token);
                var activeFeed = feed;
                var started = Stopwatch.StartNew();
                sending = Task.Run(async () =>
                {
                    while (true)
                    {
                        var unit = await activeFeed.NextAsync(cts.Token);
                        await SendAsync(LinkMessages.Video(unit.IsKeyframe, (ulong)(started.Elapsed.Ticks / 10), unit.Data), cts.Token);
                    }
                }, CancellationToken.None);
            }
            onStatus(status);
```

  6. No laço principal troque a espera por:

```csharp
                var tick = Task.Delay(options.PingInterval, cts.Token);
                var done = sending is null
                    ? await Task.WhenAny(receiving, tick)
                    : await Task.WhenAny(receiving, sending, tick);
                if (done == receiving)
                {
                    return await receiving;
                }
                if (done == sending)
                {
                    await sending; // relança a falha de escrita (IOException → Closed)
                    return LinkEndReason.Closed;
                }
```

  7. No `finally`, depois de aguardar `receiving` e **antes** do `BYE`, acrescente:

```csharp
            if (sending is not null)
            {
                try
                {
                    await sending;
                }
                catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
                {
                }
            }
            if (feed is not null)
            {
                await feed.DisposeAsync();
            }
```

- [ ] **Step 3: Implementar status, fábrica e serviço**

`TunnelStatus` em `UsbTunnelService.cs`: `public sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null);`. Construtor primário: `public sealed class UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)` (com `using TelaDois.Core.Video;`). Em `RunDeviceAsync`, a chamada vira:

```csharp
                var reason = await LinkSession.RunAsync(
                    tunnel,
                    status => Report(new TunnelStatus(TunnelState.Connected, status.Hello, status.RoundTrip, status.Video)),
                    options,
                    ct,
                    openVideo);
```

`TunnelStatusText.For`: acrescente antes dos casos existentes de `Hello`:

```csharp
        _ when status is { Video: { } video, RoundTrip: { } rtt } =>
            $"TelaDois: transmitindo · {video.Width}×{video.Height} · {video.Fps} fps · {Math.Round(rtt.TotalMilliseconds):0} ms",
        _ when status.Video is { } video => $"TelaDois: transmitindo · {video.Width}×{video.Height} · {video.Fps} fps",
```

`VideoFeedFactory.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Ao abrir uma sessão: acha o FFmpeg, escolhe o monitor e a compressão e começa a captura.</summary>
public sealed class VideoFeedFactory(string? ffmpegPath, IProcessRunner runner, Func<IReadOnlyList<DisplayOutput>> displays, Func<string?> preferredMonitor, Action<string> log)
{
    public const ushort Fps = 60;

    private readonly EncoderProbe? _probe = ffmpegPath is null ? null : new EncoderProbe(runner, ffmpegPath, log);

    public event Action? FfmpegMissing;

    public async Task<IVideoFeed?> OpenAsync(HelloInfo hello, CancellationToken ct)
    {
        if (ffmpegPath is null || _probe is null)
        {
            log("FFmpeg não encontrado: instale com 'winget install Gyan.FFmpeg'");
            FfmpegMissing?.Invoke();
            return null;
        }
        var output = DisplayCatalog.Choose(displays(), preferredMonitor());
        if (output is null)
        {
            log("nenhum monitor para transmitir");
            return null;
        }
        var profile = await _probe.ChooseAsync(output.OutputIndex, ct);
        log($"transmitindo {output.DeviceName} ({output.Width}×{output.Height}) com {profile.Id} para o iPhone {hello.Width}×{hello.Height}");
        return new FfmpegCapture(runner, ffmpegPath, FfmpegArguments.ForMonitor(profile, output.OutputIndex, Fps),
            new VideoConfig((uint)output.Width, (uint)output.Height, Fps), log);
    }
}
```

Run: `dotnet test windows/TelaDois.sln` → Expected: PASS (todos, incluindo os antigos de sessão e túnel).

- [ ] **Step 4: Bandeja** — `windows/src/TelaDois.Tray/TraySettings.cs`:

```csharp
using System.Text.Json;

namespace TelaDois.Tray;

/// <summary>Preferências guardadas em %LOCALAPPDATA%\TelaDois\settings.json.</summary>
internal sealed class TraySettings
{
    public string? Monitor { get; set; }

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelaDois", "settings.json");

    public static TraySettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<TraySettings>(File.ReadAllText(FilePath)) ?? new TraySettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new TraySettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
```

Em `TrayContext.cs`: `using TelaDois.Core.Video;`; campo `private readonly TraySettings _settings = TraySettings.Load();`; no construtor, antes de "Abrir registro":

```csharp
        var monitors = new ToolStripMenuItem("Monitor transmitido");
        monitors.DropDownItems.Add("(carregando)");
        monitors.DropDownOpening += (_, _) => FillMonitors(monitors);
        menu.Items.Add(monitors);
```

troque a criação do serviço por:

```csharp
        var video = new VideoFeedFactory(FfmpegLocator.Find(), new SystemProcessRunner(), DisplayCatalog.Enumerate, () => _settings.Monitor, _log.Write);
        video.FfmpegMissing += () => ui.Post(_ => _icon.ShowBalloonTip(10000, "TelaDois",
            "Para a Segunda Tela, instale o FFmpeg: winget install Gyan.FFmpeg", ToolTipIcon.Warning), null);
        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write, video.OpenAsync);
```

e acrescente o método:

```csharp
    private void FillMonitors(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        var outputs = DisplayCatalog.Enumerate();
        var current = DisplayCatalog.Choose(outputs, _settings.Monitor);
        foreach (var output in outputs)
        {
            var label = $"{output.DeviceName.Replace(@"\\.\", "")} · {output.Width}×{output.Height}{(output.IsPrimary ? " (principal)" : "")}";
            var item = new ToolStripMenuItem(label) { Checked = output == current };
            item.Click += (_, _) =>
            {
                _settings.Monitor = output.DeviceName;
                _settings.Save();
                _log.Write($"monitor transmitido: {output.DeviceName} (vale na próxima conexão)");
            };
            menu.DropDownItems.Add(item);
        }
        if (outputs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("nenhum monitor encontrado") { Enabled = false });
        }
    }
```

Build: `dotnet build windows/TelaDois.sln -c Release` → Expected: 0 avisos, 0 erros.

- [ ] **Step 5: Commit e push**

```bash
git add windows
git commit -m "feat(video): sessão manda CONFIG/VIDEO; túnel e bandeja transmitem o monitor escolhido"
git push
```

Expected: `windows-test` verde.

---

### Task 7: iPhone mostra o vídeo (Segunda Tela) com teste de UI

**Files:**
- Create: `ios/TelaDois/VideoRenderer.swift`, `ios/TelaDois/SecondScreenView.swift`
- Modify: `ios/TelaDois/UsbLink.swift`, `ios/TelaDois/RestScreenView.swift`, `ios/project.yml`
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift`

**Interfaces:**
- Consumes: `LinkProtocol.parseConfig/parseVideo`, `LinkVideoConfig`, `LinkVideoFrame` (Task 1); `H264Parser`, `H264NALType` (Task 2); fixture `ios/TelaDoisUITests/Fixtures/bars-640x360.h264` (Task 2).
- Produces: `UsbLink.videoConfig: LinkVideoConfig?` (observável), `UsbLink.renderer: VideoRenderer`; `VideoRenderer.enqueue(_ frame: LinkVideoFrame)`, `VideoRenderer.reset()`; `SecondScreenView(renderer:)` com identificador `second-screen`.

- [ ] **Step 1: Teste de UI (vai falhar)** — em `project.yml`, troque o `sources` do alvo `TelaDoisUITests` por:

```yaml
    sources:
      - path: TelaDoisUITests
        excludes: ["Fixtures/**"]
      - path: TelaDoisUITests/Fixtures
        buildPhase: resources
```

e acrescente a `RestScreenUITests`:

```swift
    func testVideoFromPcShowsSecondScreenAndByeReturnsToClock() throws {
        let app = launch(dimSeconds: 4)
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))

        let url = try XCTUnwrap(Bundle(for: Self.self).url(forResource: "bars-640x360", withExtension: "h264"))
        let units = H264Parser.accessUnits(in: [UInt8](try Data(contentsOf: url)))
        XCTAssertEqual(units.count, 30)
        pc.send(LinkProtocol.config(width: 640, height: 360, fps: 30))
        for (index, unit) in units.enumerated() {
            pc.send(LinkProtocol.video(keyframe: index == 0, timestamp: UInt64(index) * 33_333, accessUnit: unit))
        }

        // O app derruba o PC após 3 s sem notícias: as esperas abaixo continuam mandando PING.
        XCTAssertTrue(element(app, "second-screen").waitForExistence(timeout: 2), "deveria mostrar a Segunda Tela")
        XCTAssertFalse(app.staticTexts["clock"].exists, "o relógio some na Segunda Tela")
        // Barras coloridas decodificadas: a tela não pode estar toda preta.
        var decoded = false
        for _ in 0..<10 where !decoded {
            pc.send(LinkProtocol.ping(1))
            decoded = Self.maxBrightness(XCUIScreen.main.screenshot().image) > 0.5
            if !decoded { Thread.sleep(forTimeInterval: 0.5) }
        }
        XCTAssertTrue(decoded, "as barras coloridas deveriam aparecer")
        attachScreenshot("05-segunda-tela")

        // Mantém o PC "vivo" além do tempo de escurecer: a Segunda Tela não escurece.
        for _ in 0..<6 {
            Thread.sleep(forTimeInterval: 1)
            pc.send(LinkProtocol.ping(1))
        }
        XCTAssertFalse(app.buttons["dim-overlay"].exists, "a Segunda Tela não deve escurecer")

        pc.send(LinkProtocol.bye())
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 3), "BYE deveria voltar ao relógio")
        XCTAssertFalse(element(app, "second-screen").exists)
        attachScreenshot("06-volta-descanso")
    }
```

Commit e push (RED):

```bash
git add ios/project.yml ios/TelaDoisUITests/RestScreenUITests.swift
git commit -m "test(ui): Segunda Tela mostra o vídeo do PC e volta ao relógio no BYE (ainda sem implementação)"
git push
```

Expected: `ios-test` vermelho só em `testVideoFromPcShowsSecondScreenAndByeReturnsToClock` ("deveria mostrar a Segunda Tela"); os outros testes verdes. Se falhar por compilação ou por não achar o recurso `bars-640x360.h264`, corrija primeiro.

- [ ] **Step 2: `VideoRenderer.swift`**

```swift
import AVFoundation
import TelaDoisCore
import UIKit

/// Decodifica as imagens H.264 do PC no hardware do iPhone (`AVSampleBufferDisplayLayer`).
/// Trabalha numa fila própria, fora da thread da interface. Em erro, limpa e espera a próxima imagem completa.
final class VideoRenderer: @unchecked Sendable {
    let layer = AVSampleBufferDisplayLayer()
    private let queue = DispatchQueue(label: "teladois.video")
    // Só acessados na `queue`.
    private var format: CMVideoFormatDescription?
    private var waitingForKeyframe = true

    init() {
        layer.videoGravity = .resizeAspect
        layer.backgroundColor = UIColor.black.cgColor
    }

    func enqueue(_ frame: LinkVideoFrame) {
        queue.async { self.handle(frame) }
    }

    func reset() {
        queue.async {
            self.format = nil
            self.waitingForKeyframe = true
            self.layer.sampleBufferRenderer.flush()
        }
    }

    private func handle(_ frame: LinkVideoFrame) {
        let renderer = layer.sampleBufferRenderer
        if renderer.status == .failed {
            renderer.flush()
            waitingForKeyframe = true
        }
        let nals = H264Parser.nalUnits(in: frame.accessUnit)
        if let sps = nals.first(where: { $0.type == H264NALType.sps }),
           let pps = nals.first(where: { $0.type == H264NALType.pps }) {
            format = Self.makeFormat(sps: sps.bytes, pps: pps.bytes) ?? format
        }
        if waitingForKeyframe {
            guard frame.isKeyframe, H264Parser.containsIDR(nals) else { return }
            waitingForKeyframe = false
        }
        guard let format, let sample = Self.makeSample(H264Parser.avcc(nals), format: format) else { return }
        renderer.enqueue(sample)
    }

    private static func makeFormat(sps: [UInt8], pps: [UInt8]) -> CMVideoFormatDescription? {
        var format: CMVideoFormatDescription?
        let status = sps.withUnsafeBufferPointer { spsBuffer in
            pps.withUnsafeBufferPointer { ppsBuffer -> OSStatus in
                guard let spsBase = spsBuffer.baseAddress, let ppsBase = ppsBuffer.baseAddress else { return -1 }
                let pointers = [spsBase, ppsBase]
                let sizes = [sps.count, pps.count]
                return CMVideoFormatDescriptionCreateFromH264ParameterSets(
                    allocator: kCFAllocatorDefault,
                    parameterSetCount: 2,
                    parameterSetPointers: pointers,
                    parameterSetSizes: sizes,
                    nalUnitHeaderLength: 4,
                    formatDescriptionOut: &format
                )
            }
        }
        return status == noErr ? format : nil
    }

    private static func makeSample(_ avcc: [UInt8], format: CMVideoFormatDescription) -> CMSampleBuffer? {
        guard !avcc.isEmpty else { return nil }
        var block: CMBlockBuffer?
        guard CMBlockBufferCreateWithMemoryBlock(
            allocator: kCFAllocatorDefault, memoryBlock: nil, blockLength: avcc.count,
            blockAllocator: kCFAllocatorDefault, customBlockSource: nil, offsetToData: 0,
            dataLength: avcc.count, flags: 0, blockBufferOut: &block
        ) == kCMBlockBufferNoErr, let block else { return nil }
        let copied = avcc.withUnsafeBytes { bytes -> OSStatus in
            guard let base = bytes.baseAddress else { return -1 }
            return CMBlockBufferReplaceDataBytes(with: base, blockBuffer: block, offsetIntoDestination: 0, dataLength: avcc.count)
        }
        guard copied == kCMBlockBufferNoErr else { return nil }
        var sample: CMSampleBuffer?
        var size = avcc.count
        guard CMSampleBufferCreateReady(
            allocator: kCFAllocatorDefault, dataBuffer: block, formatDescription: format,
            sampleCount: 1, sampleTimingEntryCount: 0, sampleTimingArray: nil,
            sampleSizeEntryCount: 1, sampleSizeArray: &size, sampleBufferOut: &sample
        ) == noErr, let sample else { return nil }
        if let attachments = CMSampleBufferGetSampleAttachmentsArray(sample, createIfNecessary: true),
           CFArrayGetCount(attachments) > 0 {
            let dictionary = unsafeBitCast(CFArrayGetValueAtIndex(attachments, 0), to: CFMutableDictionary.self)
            CFDictionarySetValue(
                dictionary,
                Unmanaged.passUnretained(kCMSampleAttachmentKey_DisplayImmediately).toOpaque(),
                Unmanaged.passUnretained(kCFBooleanTrue).toOpaque()
            )
        }
        return sample
    }
}
```

- [ ] **Step 3: `SecondScreenView.swift`**

```swift
import AVFoundation
import SwiftUI
import UIKit

/// Vídeo do PC em tela cheia, aspecto preservado, fundo preto.
struct SecondScreenView: UIViewRepresentable {
    let renderer: VideoRenderer

    func makeUIView(context: Context) -> VideoLayerView {
        VideoLayerView(videoLayer: renderer.layer)
    }

    func updateUIView(_ uiView: VideoLayerView, context: Context) {}
}

final class VideoLayerView: UIView {
    private let videoLayer: AVSampleBufferDisplayLayer

    init(videoLayer: AVSampleBufferDisplayLayer) {
        self.videoLayer = videoLayer
        super.init(frame: .zero)
        backgroundColor = .black
        layer.addSublayer(videoLayer)
    }

    required init?(coder: NSCoder) { nil }

    override func layoutSubviews() {
        super.layoutSubviews()
        videoLayer.frame = bounds
    }
}
```

- [ ] **Step 4: `UsbLink.swift`** — acrescente as propriedades:

```swift
    /// Formato do vídeo do PC; `nil` = sem Segunda Tela (Modo Descanso).
    private(set) var videoConfig: LinkVideoConfig?
    /// Constante: o `@Observable` não acompanha `let`.
    let renderer = VideoRenderer()
```

em `handle(_:)`, dentro do `for frame in frames`, **antes** do laço `for action in session.receive(...)`:

```swift
            switch frame.messageType {
            case .config:
                if let config = LinkProtocol.parseConfig(frame) {
                    renderer.reset()
                    videoConfig = config
                }
            case .video:
                if let video = LinkProtocol.parseVideo(frame) { renderer.enqueue(video) }
            default:
                break
            }
```

e em `disconnect()`, depois de `isConnected = false`:

```swift
        if videoConfig != nil {
            videoConfig = nil
            renderer.reset()
        }
```

Troque `maximumLength: 64 * 1024` por `maximumLength: 1024 * 1024` em `receiveNext` (imagens completas chegam a centenas de KB).

- [ ] **Step 5: `RestScreenView.swift`** — no `ZStack`, envolva o conteúdo atual (o `ClockPageView` e a camada `dim-overlay`) assim:

```swift
                if link.videoConfig != nil {
                    SecondScreenView(renderer: link.renderer)
                        .accessibilityElement()
                        .accessibilityIdentifier("second-screen")
                        .accessibilityLabel("Segunda Tela")
                } else {
                    ClockPageView(
                        clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                        temperature: weather.temperatureLabel,
                        batteryLevel: battery.level,
                        isCharging: battery.isCharging,
                        isLinked: link.isConnected,
                        safeArea: geo.safeAreaInsets
                    )
                    if dim.isDimmed {
                        // (bloco atual da camada preta, sem mudanças)
                    }
                }
```

(mantenha o bloco do `dim-overlay` exatamente como está, só movido para dentro do `else`). No `.onReceive(ticker)` troque o corpo por:

```swift
            now = tick.addingTimeInterval(clockOffset)
            if link.videoConfig != nil {
                // Segunda Tela: nunca escurece e usa o brilho do usuário.
                dim.touch(now: tick)
                brightness.restore()
                return
            }
            let wasDimmed = dim.isDimmed
            dim.update(now: tick)
            if dim.isDimmed, !wasDimmed { brightness.dim() }
```

- [ ] **Step 6: Commit, push e CI (GREEN)**

```bash
git add ios
git commit -m "feat(ios): Segunda Tela decodifica o vídeo do PC e volta ao Descanso ao desconectar"
git push
```

Expected: `ios-test` (todos os testes de UI + pacote) e `ios-build` verdes, **zero avisos** (`gh run view <id> --log | grep -c 'warning:'` = 0). Se o `AVSampleBufferDisplayLayer.sampleBufferRenderer` gerar aviso de disponibilidade, envolva em `if #available(iOS 17.0, *)` só se o aviso aparecer de fato.

- [ ] **Step 7: Revisar os prints** (a spec fala em `04-segunda-tela`; o `04` já é o `04-pc-conectado` da Etapa 4, por isso aqui são `05` e `06`) — baixe `screenshots` numa pasta nova do scratchpad e abra `05-segunda-tela.png` e `06-volta-descanso.png` com Read. Expected: `05` mostra as barras coloridas SMPTE ocupando a altura toda (640×360 cabe com barras pretas nas laterais), sem relógio; `06` é o relógio como no `01-relogio.png`.

---

### Task 8: Teste no iPhone real, documentação e `main`

**Files:**
- Modify: `CLAUDE.md` (Estado atual, Estrutura de pastas, Como rodar localmente), `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8, linha da Etapa 5: "5a concluída")
- Modify (se a validação no PC AMD pedir): `windows/src/TelaDois.Core/Video/EncoderProfiles.cs`

**Interfaces:**
- Consumes: tudo acima; CI verde no SHA final do branch.

- [ ] **Step 1: App do PC** — `dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows`. Feche um `TelaDois.exe` antigo (bandeja → Sair) e abra `builds/windows/TelaDois.exe`.

- [ ] **Step 2: App do iPhone** — baixe `TelaDois-ipa` do `ios-build` verde do SHA final, inspecione com Python isolado (regras do `CLAUDE.md`) e instale: `wsl -d Ubuntu --exec bash -lc 'xtool install --usb "/mnt/c/Pedro/Arquivos Pedro/Aplicativo celular/builds/<pasta>/TelaDois.ipa"'`. Confirme "Successfully installed!".

- [ ] **Step 3: Critério de pronto (com o usuário, PC Intel)**
  1. App aberto + cabo: o monitor de 1366×768 aparece no iPhone em ≤ 3 s; bandeja diz "transmitindo · 1366×768 · 60 fps · N ms"; o registro (`%LOCALAPPDATA%\TelaDois\tela-dois.log`) diz `compressão escolhida: qsv-hwmap (h264_qsv)`.
  2. Latência < 100 ms: abra um cronômetro com milissegundos no monitor transmitido; o usuário fotografa o PC e o iPhone juntos; anote a diferença.
  3. Rolar uma página e um vídeo do YouTube: fluido; texto parado nítido.
  4. Tirar o cabo e fechar o app: o iPhone volta ao relógio; nenhum `ffmpeg.exe` no Gerenciador de Tarefas (`tasklist | findstr ffmpeg` vazio).

- [ ] **Step 4: PC de casa (AMD)** — repita o Step 3 no PC `D:\Downloads\Teladois`. Se o registro não mostrar `amf-d3d11` ou `amf-download`, rode as linhas de `FfmpegArguments.Probe` à mão para cada perfil AMF, descubra a combinação que funciona, ajuste `EncoderProfiles.Candidates`, rode `dotnet test` e faça commit.

- [ ] **Step 5: Documentação e `main`** — atualize o "Estado atual" do `CLAUDE.md` (5a pronta, compressão de cada PC, latência medida, próximos passos: 5b monitor virtual) e a estrutura de pastas (`windows/src/TelaDois.Core/Video/`, `ios/TelaDois/VideoRenderer.swift`). Commit, push, confirme `ios-test`, `ios-build` e `windows-test` verdes no SHA final e junte ao `main`:

```bash
git switch main && git pull -q && git merge --ff-only etapa-5a-video && git push origin main
```
