# Etapa 5 — Segunda Tela (monitor virtual + vídeo pelo cabo): Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Com o iPhone no cabo e o app aberto, o Windows ganha um monitor extra de 2556×1179; o app de bandeja captura esse monitor com o FFmpeg, codifica em H.264 pela GPU e manda pelo túnel da Etapa 4; o iPhone mostra o vídeo em tela cheia. Ao desconectar (cabo, app no fundo, BYE ou tempo-limite) o monitor sai do Windows e o iPhone volta ao Modo Descanso.

**Architecture:** O protocolo ganha `CONFIG` (largura, altura, fps), `VIDEO` (um *access unit* H.264 Annex-B por mensagem) e `KEYFRAME_REQ`. No PC, `LinkSession` recebe um `IVideoSource`: depois do `HELLO`, liga o monitor virtual (`DisplayManager`, API de configuração de vídeo do Windows, sem administrador), descobre o índice do monitor para o `ddagrab`, inicia o FFmpeg (`FfmpegCapture`) e manda cada quadro. No iPhone, `UsbLink` repassa os quadros ao `StreamPlayer`, que monta `CMSampleBuffer`s (AVCC) e os entrega ao `AVSampleBufferVideoRenderer` de um `AVSampleBufferDisplayLayer`, que decodifica por hardware. O separador de quadros e a conversão Annex-B → AVCC são lógica pura testada nos dois lados.

**Tech Stack:** Swift 6 (pacote) / Swift 5 + concorrência estrita (app), AVFoundation/CoreMedia, XCTest/XCUITest; C# 14 / .NET 10, P/Invoke `user32` (`EnumDisplayDevices`, `ChangeDisplaySettingsEx`), `System.Threading.Channels`, xUnit; FFmpeg 9.0.2 (Gyan, winget `Gyan.FFmpeg`) com `ddagrab` + `h264_amf` (reserva `libx264`); Virtual Display Driver 25.7.23 (VirtualDrivers/Virtual-Display-Driver).

Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§3, §3.4, §4.1 itens 5–6, §5, §7). Base: Etapa 4 (`docs/superpowers/plans/2026-10-18-etapa-4-tunel-usb.md`).

**Fora desta etapa:** toque/mouse no iPhone, áudio, ajuste fino de latência e 30 fps automático (Etapa 6), iniciar com o Windows, ícone próprio da bandeja.

## Fatos levantados em 2026-10-19 (base das decisões)

- FFmpeg 9.0.2 no PC tem `ddagrab`, `h264_amf`, `libx264`. O `h264_amf` **recusa BGRA** e o `scale_d3d11=format=nv12` falha neste driver AMD; o caminho que funciona é `ddagrab,hwdownload,format=bgra,format=nv12` (conversão de cor na CPU): 180 quadros em 3 s a 1600×900.
- `h264_amf -g 60 -header_spacing 60` manda SPS/PPS antes de cada IDR e já insere **AUD** em todo quadro (pedir `h264_metadata=aud=insert` duplica o AUD). `libx264 -tune zerolatency` divide cada quadro em ~12 fatias e precisa de `-x264-params repeat-headers=1` + `h264_metadata=aud=insert`.
- Virtual Display Driver: lê `C:\VirtualDisplayDriver\vdd_settings.xml` (`<monitors><count>`, `<resolutions>` com `width`/`height`/`refresh_rate`); `count` 0 vira 1 — o driver **não** remove o monitor. Comandos UTF-16 no pipe `\\.\pipe\MTTVirtualDisplayPipe` (`RELOAD_DRIVER`, `SETDISPLAYCOUNT n`, `PING`). O pipe aceita qualquer usuário (`D:(A;;GA;;;WD)`) — registrar na revisão de segurança.
- Por isso o monitor fica sempre instalado e o app **liga/desliga do desktop** com `ChangeDisplaySettingsEx` (posição + tamanho; tamanho 0 = desligado), que não pede administrador e faz o Windows devolver as janelas ao monitor principal.

## Global Constraints

- Tudo da Etapa 4 continua valendo (quadro `[tipo][tamanho BE][dados]`, 16 MiB, `PING` 1 s, tempo-limite 3 s, porta 7100 só em 127.0.0.1, zero avisos, `TreatWarningsAsErrors`, textos em português, commits com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`, mensagens de commit com acento via `git commit -F`).
- `CONFIG` (PC → iPhone): largura `UInt32` BE, altura `UInt32` BE, fps `UInt16` BE. Referência: `CONFIG(2556, 1179, 60)` = `020000000A000009FC0000049B003C`.
- `VIDEO` (PC → iPhone): um access unit H.264 Annex-B completo (começa num AUD; SPS/PPS antes de cada IDR). `KEYFRAME_REQ` (iPhone → PC): sem dados. Referência: `KEYFRAME_REQ` = `0400000000`.
- O monitor virtual usa **exatamente** a resolução do `HELLO` (2556×1179 no iPhone do usuário), 60 fps, H.264 Main sem B-frames, GOP = 1 s (`-g 60`), 12 Mbit/s.
- iPhone: com vídeo chegando, a tela não escurece e o brilho volta ao do usuário; ao perder o PC volta ao Descanso ativo (spec §4.1 itens 5–6).
- PC: o monitor virtual só fica ligado durante a sessão; ao iniciar, o app desliga monitor virtual que tenha sobrado (spec §5).
- Branch: **`etapa-5-segunda-tela`**; juntar ao `main` só com `ios-test`, `ios-build` e `windows-test` verdes.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264.swift` | NALs, tipo, AVCC, access unit (SPS/PPS/keyframe), separar fluxo por AUD |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift` | + `CONFIG`, `VIDEO`, `KEYFRAME_REQ` |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift` | + ações `startVideo`, `video` |
| `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/H264Tests.swift` | Testes do H.264 |
| `ios/TelaDois/StreamPlayer.swift` | `CMSampleBuffer` + `AVSampleBufferVideoRenderer`, pede keyframe em erro |
| `ios/TelaDois/SecondScreenView.swift` | View SwiftUI com a camada de vídeo (`second-screen`) |
| `ios/TelaDois/UsbLink.swift` | + `isStreaming`, `player`, repassa `CONFIG`/`VIDEO`, manda `KEYFRAME_REQ` |
| `ios/TelaDois/RestScreenView.swift` | Mostra a Segunda Tela no lugar do relógio; desliga o escurecer |
| `ios/TelaDoisUITests/Fixtures/testsrc.h264` | 30 quadros 640×296 (libx264, AUD, SPS/PPS) gerados pelo FFmpeg |
| `ios/TelaDoisUITests/RestScreenUITests.swift` | + teste do vídeo e print `05-segunda-tela` |
| `windows/src/TelaDois.Core/Link/LinkMessages.cs` | + `Config`, `Video`, `KeyframeRequest` |
| `windows/src/TelaDois.Core/Video/H264AccessUnitSplitter.cs` | Separa o stdout do FFmpeg em access units |
| `windows/src/TelaDois.Core/Video/IVideoSource.cs` | Contrato entre sessão e captura |
| `windows/src/TelaDois.Core/Link/LinkSession.cs` | + `CONFIG`, envio de quadros, `KEYFRAME_REQ` |
| `windows/src/TelaDois.Core/Video/FfmpegArgs.cs`, `FfmpegCapture.cs` | Linha de comando e processo do FFmpeg |
| `windows/src/TelaDois.Core/Display/DisplayManager.cs` | Liga/desliga o monitor virtual (Win32) |
| `windows/src/TelaDois.Core/Video/SecondScreenSource.cs` | Junta monitor + FFmpeg num `IVideoSource` |
| `windows/src/TelaDois.Core/UsbTunnelService.cs`, `TunnelStatusText.cs`, `TelaDois.Tray/TrayContext.cs` | Liga a Segunda Tela à sessão e à bandeja |

---

### Task 1: H.264 e mensagens de vídeo no `TelaDoisCore` (TDD)

**Files:**
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264.swift`
- Modify: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift`
- Modify: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/H264Tests.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`, `LinkSessionStateTests.swift` (novos casos)

**Interfaces:**
- Produces:
  - `struct LinkConfig: Equatable, Sendable { width: UInt32; height: UInt32; fps: UInt16 }`
  - `LinkProtocol.config(width:height:fps:) -> LinkFrame`, `parseConfig(_:) -> LinkConfig?`, `video(_ accessUnit: [UInt8]) -> LinkFrame`, `keyframeRequest() -> LinkFrame`
  - `LinkSessionState.Action` + `case startVideo(LinkConfig)`, `case video([UInt8])`
  - `enum H264 { static func nalUnits(in: [UInt8]) -> [ArraySlice<UInt8>]; static func type(of: ArraySlice<UInt8>) -> UInt8; static func avcc(_: [ArraySlice<UInt8>]) -> [UInt8]; static func parseAccessUnit(_: [UInt8]) -> H264AccessUnit; static func accessUnits(in: [UInt8]) -> [[UInt8]] }`
  - `struct H264AccessUnit: Equatable, Sendable { sps: [UInt8]?; pps: [UInt8]?; isKeyframe: Bool; avcc: [UInt8] }` — `avcc` só com as fatias (tipos 1 e 5)

- [ ] **Step 1: Testes** — `H264Tests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class H264Tests: XCTestCase {
    // AUD, SPS, PPS, IDR com código de início de 4 bytes; depois AUD + fatia com 3 bytes.
    private let keyframe: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0,
                                     0, 0, 0, 1, 0x67, 0x4D, 0x00, 0x1F,
                                     0, 0, 0, 1, 0x68, 0xEE, 0x3C, 0x80,
                                     0, 0, 0, 1, 0x65, 0x88, 0x84]
    private let delta: [UInt8] = [0, 0, 1, 0x09, 0xF0, 0, 0, 1, 0x41, 0x9A, 0x02]

    func testSplitsNalUnitsWithThreeAndFourByteStartCodes() {
        let types = H264.nalUnits(in: keyframe).map(H264.type(of:))
        XCTAssertEqual(types, [9, 7, 8, 5])
        XCTAssertEqual(Array(H264.nalUnits(in: keyframe)[1]), [0x67, 0x4D, 0x00, 0x1F])
    }

    func testKeyframeCarriesParameterSetsAndOnlySlicesInAvcc() {
        let unit = H264.parseAccessUnit(keyframe)
        XCTAssertTrue(unit.isKeyframe)
        XCTAssertEqual(unit.sps, [0x67, 0x4D, 0x00, 0x1F])
        XCTAssertEqual(unit.pps, [0x68, 0xEE, 0x3C, 0x80])
        XCTAssertEqual(unit.avcc, [0, 0, 0, 3, 0x65, 0x88, 0x84])
    }

    func testDeltaFrameHasNoParameterSets() {
        let unit = H264.parseAccessUnit(delta)
        XCTAssertFalse(unit.isKeyframe)
        XCTAssertNil(unit.sps)
        XCTAssertEqual(unit.avcc, [0, 0, 0, 3, 0x41, 0x9A, 0x02])
    }

    func testSplitsStreamIntoAccessUnitsAtEachAud() {
        let units = H264.accessUnits(in: keyframe + delta)
        XCTAssertEqual(units, [keyframe, delta])
    }

    func testAudWithoutSliceIsMergedIntoNextAccessUnit() {
        // O AMF manda AUD duplicado quando se pede outro: o AUD vazio não vira quadro.
        let lonelyAud: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0]
        XCTAssertEqual(H264.accessUnits(in: lonelyAud + delta), [lonelyAud + delta])
    }
}
```

Em `LinkProtocolTests.swift`:

```swift
    func testConfigMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.config(width: 2556, height: 1179, fps: 60).encoded(), hex("020000000A000009FC0000049B003C"))
        XCTAssertEqual(LinkProtocol.parseConfig(LinkProtocol.config(width: 2556, height: 1179, fps: 60)), LinkConfig(width: 2556, height: 1179, fps: 60))
    }

    func testKeyframeRequestMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.keyframeRequest().encoded(), hex("0400000000"))
    }
```

Em `LinkSessionStateTests.swift`:

```swift
    func testConfigStartsVideoAndVideoFramesArePassedOn() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.config(width: 640, height: 296, fps: 30), now: t0),
                       [.startVideo(LinkConfig(width: 640, height: 296, fps: 30))])
        XCTAssertEqual(state.receive(LinkProtocol.video([1, 2, 3]), now: t0), [.video([1, 2, 3])])
    }
```

- [ ] **Step 2: Commit e push — CI DEVE falhar** (não compila). Mensagem: "test(core): H.264 e mensagens de vídeo (ainda sem implementação)".

- [ ] **Step 3: `H264.swift`**

```swift
/// H.264 em Annex-B (NALs separadas por `00 00 01` ou `00 00 00 01`), como o FFmpeg entrega.
public enum H264 {
    public static let sliceType: UInt8 = 1
    public static let idrType: UInt8 = 5
    public static let spsType: UInt8 = 7
    public static let ppsType: UInt8 = 8
    public static let audType: UInt8 = 9

    /// NALs de um trecho Annex-B, sem os códigos de início.
    public static func nalUnits(in bytes: [UInt8]) -> [ArraySlice<UInt8>] {
        var units: [ArraySlice<UInt8>] = []
        var start: Int?
        var index = 0
        while index + 2 < bytes.count {
            if bytes[index] == 0, bytes[index + 1] == 0, bytes[index + 2] == 1 {
                if let start { units.append(trimmingTrailingZeros(bytes[start..<index])) }
                index += 3
                start = index
            } else {
                index += 1
            }
        }
        if let start, start < bytes.count { units.append(bytes[start..<bytes.count]) }
        return units.filter { !$0.isEmpty }
    }

    public static func type(of nal: ArraySlice<UInt8>) -> UInt8 {
        (nal.first ?? 0) & 0x1F
    }

    /// AVCC: cada NAL precedida do tamanho em 4 bytes big-endian (formato do `CMSampleBuffer`).
    public static func avcc(_ nals: [ArraySlice<UInt8>]) -> [UInt8] {
        nals.reduce(into: [UInt8]()) { result, nal in
            result += bigEndianBytes(UInt32(nal.count))
            result += nal
        }
    }

    public static func parseAccessUnit(_ annexB: [UInt8]) -> H264AccessUnit {
        let nals = nalUnits(in: annexB)
        let slices = nals.filter { type(of: $0) == sliceType || type(of: $0) == idrType }
        return H264AccessUnit(
            sps: nals.first { type(of: $0) == spsType }.map(Array.init),
            pps: nals.first { type(of: $0) == ppsType }.map(Array.init),
            isKeyframe: slices.contains { type(of: $0) == idrType },
            avcc: avcc(slices)
        )
    }

    /// Separa um fluxo Annex-B em access units, cada um começando num AUD.
    /// Um AUD sem fatia (duplicado) é juntado ao access unit seguinte.
    public static func accessUnits(in stream: [UInt8]) -> [[UInt8]] {
        var starts: [Int] = []
        var index = 0
        while index + 3 < stream.count {
            if stream[index] == 0, stream[index + 1] == 0, stream[index + 2] == 1, stream[index + 3] & 0x1F == audType {
                starts.append(index > 0 && stream[index - 1] == 0 ? index - 1 : index)
                index += 4
            } else {
                index += 1
            }
        }
        var units: [[UInt8]] = []
        var pending: [UInt8] = []
        for (position, start) in starts.enumerated() {
            let end = position + 1 < starts.count ? starts[position + 1] : stream.count
            pending += stream[start..<end]
            if nalUnits(in: pending).contains(where: { type(of: $0) == sliceType || type(of: $0) == idrType }) {
                units.append(pending)
                pending = []
            }
        }
        if !pending.isEmpty, let last = units.popLast() { units.append(last + pending) }
        return units
    }

    private static func trimmingTrailingZeros(_ nal: ArraySlice<UInt8>) -> ArraySlice<UInt8> {
        var end = nal.endIndex
        while end > nal.startIndex, nal[end - 1] == 0 { end -= 1 }
        return nal[nal.startIndex..<end]
    }
}

public struct H264AccessUnit: Equatable, Sendable {
    public let sps: [UInt8]?
    public let pps: [UInt8]?
    public let isKeyframe: Bool
    /// Só as fatias (tipos 1 e 5), já em AVCC.
    public let avcc: [UInt8]
}
```

- [ ] **Step 4: `LinkProtocol.swift`** — acrescente:

```swift
public struct LinkConfig: Equatable, Sendable {
    public let width: UInt32
    public let height: UInt32
    public let fps: UInt16

    public init(width: UInt32, height: UInt32, fps: UInt16) {
        self.width = width
        self.height = height
        self.fps = fps
    }
}
```

e dentro de `enum LinkProtocol`:

```swift
    public static func config(width: UInt32, height: UInt32, fps: UInt16) -> LinkFrame {
        LinkFrame(.config, payload: bigEndianBytes(width) + bigEndianBytes(height) + bigEndianBytes(fps))
    }

    public static func parseConfig(_ frame: LinkFrame) -> LinkConfig? {
        guard frame.messageType == .config, frame.payload.count == 10 else { return nil }
        let bytes = frame.payload[...]
        return LinkConfig(
            width: readBigEndian(bytes[0..<4]),
            height: readBigEndian(bytes[4..<8]),
            fps: readBigEndian(bytes[8..<10])
        )
    }

    /// Um access unit H.264 Annex-B.
    public static func video(_ accessUnit: [UInt8]) -> LinkFrame { LinkFrame(.video, payload: accessUnit) }

    public static func keyframeRequest() -> LinkFrame { LinkFrame(.keyframeRequest) }
```

- [ ] **Step 5: `LinkSessionState.swift`** — novos casos em `Action` e em `receive`:

```swift
        case startVideo(LinkConfig)
        case video([UInt8])
```

```swift
        case .config:
            return LinkProtocol.parseConfig(frame).map { [.startVideo($0)] } ?? []
        case .video:
            return [.video(frame.payload)]
```

- [ ] **Step 6: Commit e push — CI DEVE passar** (`ios-test`, `ios-build`). Mensagem: "feat(core): H.264 (NALs, AVCC, access units) e CONFIG/VIDEO/KEYFRAME_REQ".

---

### Task 2: iPhone mostra o vídeo (`StreamPlayer`, Segunda Tela)

**Files:**
- Create: `ios/TelaDois/StreamPlayer.swift`, `ios/TelaDois/SecondScreenView.swift`
- Create: `ios/TelaDoisUITests/Fixtures/testsrc.h264` (já gerado: `ffmpeg -f lavfi -i testsrc2=size=640x296:rate=30 -frames:v 30 -c:v libx264 -profile:v main -pix_fmt yuv420p -bf 0 -g 30 -x264-params repeat-headers=1 -bsf:v h264_metadata=aud=insert -f h264 testsrc.h264`; 77 601 bytes, 1 IDR + 29 P, AUD em todos)
- Modify: `ios/TelaDois/UsbLink.swift`, `ios/TelaDois/RestScreenView.swift`
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift`

**Interfaces:**
- Consumes: `H264`, `LinkConfig`, `LinkSessionState.Action.startVideo/video` (Task 1).
- Produces: `@MainActor final class StreamPlayer { let layer: AVSampleBufferDisplayLayer; var onNeedKeyframe: (() -> Void)?; func reset(); func enqueue(_ annexB: [UInt8]) }`; `UsbLink.isStreaming: Bool`, `UsbLink.player: StreamPlayer`; elemento `second-screen`; print `05-segunda-tela`.

- [ ] **Step 1: Teste de UI** — em `RestScreenUITests`:

```swift
    private static func fixture() -> [UInt8] {
        let url = Bundle(for: FakePC.self).url(forResource: "testsrc", withExtension: "h264")!
        return [UInt8](try! Data(contentsOf: url))
    }

    /// Mantém a conexão viva (PING) por `seconds`, lendo o que o app mandar.
    private func keepAlive(_ pc: FakePC, seconds: TimeInterval) {
        let end = Date().addingTimeInterval(seconds)
        while Date() < end {
            pc.send(LinkProtocol.ping(1))
            _ = pc.readFrame()
            Thread.sleep(forTimeInterval: 0.5)
        }
    }

    func testVideoFromPcShowsSecondScreenAndKeepsScreenOn() {
        let app = launch(dimSeconds: 2)
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))

        pc.send(LinkProtocol.config(width: 640, height: 296, fps: 30))
        for unit in H264.accessUnits(in: Self.fixture()) {
            pc.send(LinkProtocol.video(unit))
        }
        XCTAssertTrue(element(app, "second-screen").waitForExistence(timeout: 5), "deveria mostrar a Segunda Tela")

        // Mais tempo que o escurecer (2 s): com vídeo, a tela não pode apagar.
        keepAlive(pc, seconds: 3)
        XCTAssertFalse(app.buttons["dim-overlay"].exists, "com vídeo a tela não escurece")
        let lit = XCTNSPredicateExpectation(
            predicate: NSPredicate { _, _ in Self.maxBrightness(XCUIScreen.main.screenshot().image) > 0.3 },
            object: nil
        )
        wait(for: [lit], timeout: 5)
        attachScreenshot("05-segunda-tela")

        pc.send(LinkProtocol.bye())
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 3), "BYE deveria voltar ao relógio")
    }
```

- [ ] **Step 2: Fixture e commit — CI DEVE falhar** (`second-screen` não existe). Commit `ios/TelaDoisUITests/Fixtures/testsrc.h264` + o teste. Mensagem: "test(ui): Segunda Tela mostra o vídeo do PC e não escurece (ainda sem implementação)".

- [ ] **Step 3: `StreamPlayer.swift`**

```swift
import AVFoundation
import CoreMedia
import TelaDoisCore
import UIKit

/// Mostra o H.264 do PC: monta `CMSampleBuffer`s (AVCC) e entrega ao renderizador da camada,
/// que decodifica por hardware. Descarta quadros até o primeiro keyframe e, se a decodificação
/// falhar, limpa e pede outro keyframe ao PC.
@MainActor
final class StreamPlayer {
    let layer = AVSampleBufferDisplayLayer()
    var onNeedKeyframe: (() -> Void)?

    private var format: CMVideoFormatDescription?
    private var waitingForKeyframe = true

    init() {
        layer.videoGravity = .resizeAspect
        layer.backgroundColor = UIColor.black.cgColor
    }

    func reset() {
        layer.sampleBufferRenderer.flush(removingDisplayedImage: true, completionHandler: nil)
        format = nil
        waitingForKeyframe = true
    }

    func enqueue(_ annexB: [UInt8]) {
        let unit = H264.parseAccessUnit(annexB)
        if let sps = unit.sps, let pps = unit.pps, let newFormat = Self.makeFormat(sps: sps, pps: pps) {
            format = newFormat
        }
        let renderer = layer.sampleBufferRenderer
        if renderer.status == .failed || renderer.requiresFlushToResumeDecoding {
            renderer.flush()
            waitingForKeyframe = true
            onNeedKeyframe?()
        }
        if waitingForKeyframe {
            guard unit.isKeyframe else { return }
            waitingForKeyframe = false
        }
        guard let format, !unit.avcc.isEmpty, let sample = Self.makeSample(unit.avcc, format: format) else { return }
        renderer.enqueue(sample)
    }

    private static func makeFormat(sps: [UInt8], pps: [UInt8]) -> CMVideoFormatDescription? {
        var format: CMVideoFormatDescription?
        let status = sps.withUnsafeBufferPointer { spsBuffer in
            pps.withUnsafeBufferPointer { ppsBuffer in
                guard let spsBase = spsBuffer.baseAddress, let ppsBase = ppsBuffer.baseAddress else { return OSStatus(-1) }
                return CMVideoFormatDescriptionCreateFromH264ParameterSets(
                    allocator: kCFAllocatorDefault,
                    parameterSetCount: 2,
                    parameterSetPointers: [spsBase, ppsBase],
                    parameterSetSizes: [sps.count, pps.count],
                    nalUnitHeaderLength: 4,
                    formatDescriptionOut: &format
                )
            }
        }
        return status == noErr ? format : nil
    }

    private static func makeSample(_ avcc: [UInt8], format: CMVideoFormatDescription) -> CMSampleBuffer? {
        var block: CMBlockBuffer?
        guard CMBlockBufferCreateWithMemoryBlock(
            allocator: kCFAllocatorDefault, memoryBlock: nil, blockLength: avcc.count,
            blockAllocator: kCFAllocatorDefault, customBlockSource: nil, offsetToData: 0,
            dataLength: avcc.count, flags: 0, blockBufferOut: &block
        ) == noErr, let block else { return nil }
        let copied = avcc.withUnsafeBytes { bytes in
            CMBlockBufferReplaceDataBytes(with: bytes.baseAddress!, blockBuffer: block, offsetIntoDestination: 0, dataLength: avcc.count)
        }
        guard copied == noErr else { return nil }
        var sample: CMSampleBuffer?
        var size = avcc.count
        guard CMSampleBufferCreateReady(
            allocator: kCFAllocatorDefault, dataBuffer: block, formatDescription: format,
            sampleCount: 1, sampleTimingEntryCount: 0, sampleTimingArray: nil,
            sampleSizeEntryCount: 1, sampleSizeArray: &size, sampleBufferOut: &sample
        ) == noErr, let sample else { return nil }
        // Sem relógio: mostra assim que decodificar (menor latência).
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

- [ ] **Step 4: `SecondScreenView.swift`**

```swift
import SwiftUI
import UIKit

/// Tela cheia com o vídeo do PC.
struct SecondScreenView: UIViewRepresentable {
    let player: StreamPlayer

    func makeUIView(context: Context) -> PlayerView {
        let view = PlayerView()
        view.backgroundColor = .black
        view.layer.addSublayer(player.layer)
        view.isAccessibilityElement = true
        view.accessibilityIdentifier = "second-screen"
        view.accessibilityLabel = "Segunda tela do PC"
        return view
    }

    func updateUIView(_ uiView: PlayerView, context: Context) {}

    final class PlayerView: UIView {
        override func layoutSubviews() {
            super.layoutSubviews()
            layer.sublayers?.forEach { $0.frame = bounds }
        }
    }
}
```

- [ ] **Step 5: `UsbLink.swift`** — estado e repasse:

```swift
    private(set) var isStreaming = false
    @ObservationIgnored let player = StreamPlayer()
```

no fim de `start()` (antes de `listener.start`), uma vez só:

```swift
        player.onNeedKeyframe = { [weak self] in self?.send(LinkProtocol.keyframeRequest()) }
```

em `handle(_:)`, no `switch action`:

```swift
                case .startVideo:
                    player.reset()
                    isStreaming = true
                case .video(let accessUnit):
                    player.enqueue(accessUnit)
```

e em `disconnect()`:

```swift
        if isStreaming {
            isStreaming = false
            player.reset()
        }
```

- [ ] **Step 6: `RestScreenView.swift`** — dentro do `GeometryReader`, troque o `ZStack` por:

```swift
            if link.isStreaming {
                SecondScreenView(player: link.player)
                    .ignoresSafeArea()
            } else {
                ZStack { /* conteúdo atual do Descanso, sem mudanças */ }
                    .ignoresSafeArea()
                    .simultaneousGesture(/* gesto atual */)
            }
```

No `onReceive(ticker)`, antes de `dim.update`:

```swift
            // Segunda Tela: o escurecer fica desligado (spec §4.1 item 5).
            if link.isStreaming { dim.touch(now: tick) }
```

e depois do `.onChange(of: scenePhase)`:

```swift
        .onChange(of: link.isStreaming) { _, streaming in
            if streaming { brightness.restore() }
        }
```

- [ ] **Step 7: Commit e push — CI DEVE passar**, zero avisos. Mensagem: "feat: Segunda Tela no iPhone (H.264 pelo AVSampleBufferDisplayLayer, sem escurecer)".

- [ ] **Step 8: Revisar `05-segunda-tela.png`** — Expected: o padrão colorido do `testsrc2` ocupando a tela (aspecto 640×296 ≈ tela), sem relógio, sem faixa preta grande, sem camada escura.

---

### Task 3: Mensagens de vídeo e separador de quadros no PC (TDD, local)

**Files:**
- Modify: `windows/src/TelaDois.Core/Link/LinkMessages.cs`
- Create: `windows/src/TelaDois.Core/Video/H264AccessUnitSplitter.cs`
- Test: `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs` (novos casos), `windows/tests/TelaDois.Core.Tests/H264AccessUnitSplitterTests.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fixtures/testsrc.h264` (cópia do arquivo do iOS; `<None Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />` no `.csproj` de testes)

**Interfaces:**
- Produces: `readonly record struct VideoConfig(uint Width, uint Height, ushort Fps)`; `LinkMessages.Config(VideoConfig)`, `TryParseConfig(LinkFrame, out VideoConfig)`, `Video(byte[])`, `KeyframeRequest()`; `sealed class H264AccessUnitSplitter { IReadOnlyList<byte[]> Append(ReadOnlySpan<byte>) }` (namespace `TelaDois.Core.Video`) — devolve só access units completos (do AUD até antes do próximo AUD com fatia), juntando AUD vazio ao seguinte.

- [ ] **Step 1: Testes** — em `LinkProtocolTests.cs`:

```csharp
    [Fact]
    public void ConfigMatchesGoldenBytes()
    {
        var frame = LinkMessages.Config(new VideoConfig(2556, 1179, 60));
        Assert.Equal(Convert.FromHexString("020000000A000009FC0000049B003C"), frame.Encode());
        Assert.True(LinkMessages.TryParseConfig(frame, out var config));
        Assert.Equal(new VideoConfig(2556, 1179, 60), config);
    }

    [Fact]
    public void KeyframeRequestMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("0400000000"), LinkMessages.KeyframeRequest().Encode());
```

`H264AccessUnitSplitterTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class H264AccessUnitSplitterTests
{
    private static readonly byte[] Keyframe = Convert.FromHexString("00000001" + "09F0" + "00000001" + "674D001F" + "00000001" + "68EE3C80" + "00000001" + "658884");
    private static readonly byte[] Delta = Convert.FromHexString("000001" + "09F0" + "000001" + "419A02");

    [Fact]
    public void EmitsAccessUnitOnlyWhenTheNextOneStarts()
    {
        var splitter = new H264AccessUnitSplitter();
        Assert.Empty(splitter.Append(Keyframe));
        var units = splitter.Append(Delta);
        Assert.Equal(Keyframe, Assert.Single(units));
    }

    [Fact]
    public void HandlesBytesArrivingOneByOne()
    {
        var splitter = new H264AccessUnitSplitter();
        var units = new List<byte[]>();
        foreach (var b in Keyframe.Concat(Delta).Concat(Keyframe))
        {
            units.AddRange(splitter.Append([b]));
        }
        Assert.Equal(2, units.Count);
        Assert.Equal(Keyframe, units[0]);
        Assert.Equal(Delta, units[1]);
    }

    [Fact]
    public void MergesAudWithoutSliceIntoNextAccessUnit()
    {
        var lonelyAud = Convert.FromHexString("0000000109F0");
        var splitter = new H264AccessUnitSplitter();
        splitter.Append(lonelyAud);
        splitter.Append(Delta);
        var units = splitter.Append(Keyframe);
        Assert.Equal(lonelyAud.Concat(Delta).ToArray(), Assert.Single(units));
    }

    [Fact]
    public void SplitsTheRealFixtureIntoThirtyFrames()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "testsrc.h264"));
        var splitter = new H264AccessUnitSplitter();
        var units = splitter.Append(bytes).Concat(splitter.Append(Convert.FromHexString("0000000109F0000001419A02"))).ToList();
        Assert.Equal(30, units.Count);
    }
}
```

- [ ] **Step 2: Rodar — DEVE falhar** (`dotnet test windows/TelaDois.sln`)

- [ ] **Step 3: Implementar** — em `LinkMessages.cs`:

```csharp
public readonly record struct VideoConfig(uint Width, uint Height, ushort Fps);
```

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

    /// <summary>Um access unit H.264 Annex-B.</summary>
    public static LinkFrame Video(byte[] accessUnit) => new(LinkMessageType.Video, accessUnit);

    public static LinkFrame KeyframeRequest() => new(LinkMessageType.KeyframeRequest);
```

`src/TelaDois.Core/Video/H264AccessUnitSplitter.cs`:

```csharp
namespace TelaDois.Core.Video;

/// <summary>
/// Separa o H.264 Annex-B do FFmpeg em access units (um quadro cada), cortando antes de cada AUD.
/// Um AUD sem fatia (o AMF duplica quando se pede outro) é juntado ao quadro seguinte.
/// </summary>
public sealed class H264AccessUnitSplitter
{
    private const byte AudType = 9;
    private readonly List<byte> _buffer = [];
    private int _scanFrom;

    public IReadOnlyList<byte[]> Append(ReadOnlySpan<byte> data)
    {
        _buffer.AddRange(data);
        var units = new List<byte[]>();
        while (true)
        {
            var next = FindAud(Math.Max(_scanFrom, 1));
            if (next < 0)
            {
                _scanFrom = Math.Max(1, _buffer.Count - 4);
                return units;
            }
            if (HasSlice(0, next))
            {
                units.Add(_buffer.GetRange(0, next).ToArray());
                _buffer.RemoveRange(0, next);
                _scanFrom = 1;
            }
            else
            {
                _scanFrom = next + 4;
            }
        }
    }

    /// <summary>Início (incluindo o zero extra do código de 4 bytes) do próximo AUD a partir de <paramref name="from"/>.</summary>
    private int FindAud(int from)
    {
        for (var i = from; i + 3 < _buffer.Count; i++)
        {
            if (_buffer[i] == 0 && _buffer[i + 1] == 0 && _buffer[i + 2] == 1 && (_buffer[i + 3] & 0x1F) == AudType)
            {
                return i > 0 && _buffer[i - 1] == 0 ? i - 1 : i;
            }
        }
        return -1;
    }

    private bool HasSlice(int start, int end)
    {
        for (var i = start; i + 3 < end; i++)
        {
            if (_buffer[i] == 0 && _buffer[i + 1] == 0 && _buffer[i + 2] == 1)
            {
                var type = _buffer[i + 3] & 0x1F;
                if (type is 1 or 5)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
```

- [ ] **Step 4: Rodar — DEVE passar.** Commit: "feat(windows): CONFIG/VIDEO/KEYFRAME_REQ e separador de quadros H.264".

---

### Task 4: Sessão do PC manda vídeo (`IVideoSource`)

**Files:**
- Create: `windows/src/TelaDois.Core/Video/IVideoSource.cs`
- Modify: `windows/src/TelaDois.Core/Link/LinkSession.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeVideoSource.cs`
- Test: `windows/tests/TelaDois.Core.Tests/LinkSessionVideoTests.cs`

**Interfaces:**
- Consumes: `LinkMessages.Config/Video/KeyframeRequest`, `VideoConfig` (Task 3).
- Produces:
  - `interface IVideoSource { Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct); }`
  - `interface IVideoStream : IAsyncDisposable { VideoConfig Config { get; } IAsyncEnumerable<byte[]> AccessUnitsAsync(CancellationToken ct); void RequestKeyframe(); }`
  - `LinkStatus` ganha `VideoConfig? Video = null`; `LinkSession.RunAsync(Stream, Action<LinkStatus>, LinkSessionOptions, CancellationToken, IVideoSource? video = null)` — com fonte: depois do HELLO manda `CONFIG`, bombeia os quadros, repassa `KEYFRAME_REQ`, descarta a fonte ao sair (mesmo em erro). Fim do fluxo de vídeo encerra a sessão (`LinkEndReason.Closed`).

- [ ] **Step 1: Fonte falsa** — `Fakes/FakeVideoSource.cs`:

```csharp
using System.Runtime.CompilerServices;
using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Fakes;

internal sealed class FakeVideoSource(int frames) : IVideoSource
{
    public HelloInfo? StartedWith { get; private set; }
    public FakeVideoStream? Stream { get; private set; }

    public Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct)
    {
        StartedWith = hello;
        Stream = new FakeVideoStream(new VideoConfig(hello.Width, hello.Height, 60), frames);
        return Task.FromResult<IVideoStream>(Stream);
    }
}

internal sealed class FakeVideoStream(VideoConfig config, int frames) : IVideoStream
{
    private int _keyframeRequests;

    public VideoConfig Config => config;
    public int KeyframeRequests => Volatile.Read(ref _keyframeRequests);
    public bool Disposed { get; private set; }

    public async IAsyncEnumerable<byte[]> AccessUnitsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 0; i < frames; i++)
        {
            yield return [0, 0, 0, 1, 9, 0xF0, 0, 0, 1, 0x41, (byte)i];
        }
        // Depois dos quadros fica "capturando" até a sessão terminar.
        await Task.Delay(Timeout.Infinite, ct);
    }

    public void RequestKeyframe() => Interlocked.Increment(ref _keyframeRequests);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Testes** — `LinkSessionVideoTests.cs`:

```csharp
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests;

public class LinkSessionVideoTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    [Fact]
    public async Task SendsConfigThenFramesAndForwardsKeyframeRequests()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var source = new FakeVideoSource(frames: 3);
        var run = LinkSession.RunAsync(pc, _ => { }, Fast, cts.Token, source);

        await phone.WriteAsync(LinkMessages.Hello(640, 296).Encode(), cts.Token);
        var reader = new LinkFrameReader(phone);
        var received = new List<LinkFrame>();
        while (received.Count(f => f.MessageType == LinkMessageType.Video) < 3)
        {
            var frame = await reader.NextAsync(cts.Token) ?? throw new InvalidOperationException("sessão fechou cedo");
            if (frame.MessageType is LinkMessageType.Config or LinkMessageType.Video)
            {
                received.Add(frame);
            }
        }
        Assert.True(LinkMessages.TryParseConfig(received[0], out var config));
        Assert.Equal(new VideoConfig(640, 296, 60), config);
        Assert.Equal(new byte[] { 0, 1, 2 }, received.Skip(1).Select(f => f.Payload[^1]));

        await phone.WriteAsync(LinkMessages.KeyframeRequest().Encode(), cts.Token);
        while (source.Stream!.KeyframeRequests == 0)
        {
            await Task.Delay(10, cts.Token);
        }
        cts.Cancel();
        Assert.Equal(LinkEndReason.Cancelled, await run);
        Assert.True(source.Stream.Disposed);
    }

    [Fact]
    public async Task ReportsVideoConfigInStatus()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var withVideo = new TaskCompletionSource<LinkStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = FakePhone.RespondAsync(phone, cts.Token);
        var run = LinkSession.RunAsync(pc, s => { if (s.Video is not null) withVideo.TrySetResult(s); }, Fast, cts.Token, new FakeVideoSource(1));

        var status = await withVideo.Task.WaitAsync(cts.Token);
        Assert.Equal(new VideoConfig(2556, 1179, 60), status.Video);
        cts.Cancel();
        await run;
    }
}
```

- [ ] **Step 3: Rodar — DEVE falhar.**

- [ ] **Step 4: Implementar** — `Video/IVideoSource.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>De onde vem o vídeo da Segunda Tela (monitor virtual + FFmpeg; nos testes, uma fonte falsa).</summary>
public interface IVideoSource
{
    Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct);
}

public interface IVideoStream : IAsyncDisposable
{
    VideoConfig Config { get; }

    /// <summary>Access units H.264 Annex-B, um por quadro, até o token ser cancelado ou a captura parar.</summary>
    IAsyncEnumerable<byte[]> AccessUnitsAsync(CancellationToken ct);

    void RequestKeyframe();
}
```

`LinkSession.cs` — mudanças:
1. `public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip, VideoConfig? Video = null);`
2. Assinatura: `RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct, IVideoSource? video = null)`.
3. Depois de `onStatus(status)` do HELLO:

```csharp
            IVideoStream? videoStream = null;
            Task<LinkEndReason>? pumping = null;
            if (video is not null)
            {
                videoStream = await video.StartAsync(hello.Value, cts.Token);
                streams.Add(videoStream);
                await SendAsync(LinkMessages.Config(videoStream.Config), cts.Token);
                status = status with { Video = videoStream.Config };
                onStatus(status);
                pumping = Task.Run(async () =>
                {
                    await foreach (var unit in videoStream.AccessUnitsAsync(cts.Token))
                    {
                        await SendAsync(LinkMessages.Video(unit), cts.Token);
                    }
                    return LinkEndReason.Closed;
                }, CancellationToken.None);
            }
```

   (`var streams = new List<IVideoStream>();` declarado antes do `try`; `status` passa a ser `var` mutável capturado pelo `receiving`.)
4. No `receiving`, novo caso:

```csharp
                        case LinkMessageType.KeyframeRequest:
                            videoStream?.RequestKeyframe();
                            break;
```

5. No laço de PING, aguarde também o bombeamento: `var finished = await Task.WhenAny(pumping is null ? [receiving, tick] : [receiving, pumping, tick]);` — se `finished == receiving` ou `finished == pumping`, `return await (Task<LinkEndReason>)finished;`.
6. No `finally`, depois de cancelar e esperar `receiving` (e `pumping`, com o mesmo `catch`), antes do BYE: `foreach (var s in streams) { await s.DisposeAsync(); }`.

- [ ] **Step 5: Rodar — DEVE passar** (3 vezes). Commit: "feat(windows): sessão manda CONFIG e os quadros de uma fonte de vídeo".

---

### Task 5: Captura e codificação com o FFmpeg

**Files:**
- Create: `windows/src/TelaDois.Core/Video/FfmpegArgs.cs`, `windows/src/TelaDois.Core/Video/FfmpegCapture.cs`
- Test: `windows/tests/TelaDois.Core.Tests/FfmpegArgsTests.cs`, `windows/tests/TelaDois.Core.Tests/FfmpegCaptureTests.cs` (`[Trait("Category", "Local")]`: precisa de FFmpeg)
- Modify: `.github/workflows/windows-test.yml` (`--filter "Category!=Local"`)

**Interfaces:**
- Consumes: `IVideoStream`, `VideoConfig`, `H264AccessUnitSplitter`.
- Produces:
  - `static class FfmpegArgs { IReadOnlyList<string> Desktop(int outputIndex, VideoConfig c, string encoder); IReadOnlyList<string> TestPattern(VideoConfig c, int frames); IReadOnlyList<string> ProbeOutput(int outputIndex); }` — `encoder` ∈ `h264_amf`, `libx264`
  - `sealed class FfmpegCapture : IVideoStream { static FfmpegCapture Start(string ffmpegPath, IReadOnlyList<string> args, VideoConfig config, Action<string> log); static Task<string> ChooseEncoderAsync(string ffmpegPath, CancellationToken ct); static Task<int?> FindOutputIndexAsync(string ffmpegPath, uint width, uint height, CancellationToken ct); static string? Locate() }`

- [ ] **Step 1: Testes** — `FfmpegArgsTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class FfmpegArgsTests
{
    private static readonly VideoConfig Phone = new(2556, 1179, 60);

    [Fact]
    public void AmfCapturesTheChosenOutputAsNv12WithoutBFramesAndOneSecondGop()
    {
        var args = string.Join(' ', FfmpegArgs.Desktop(1, Phone, "h264_amf"));
        Assert.Contains("ddagrab=output_idx=1:framerate=60:draw_mouse=1,hwdownload,format=bgra,format=nv12", args);
        Assert.Contains("-c:v h264_amf -usage ultralowlatency -bf 0 -b:v 12M -g 60 -header_spacing 60", args);
        Assert.DoesNotContain("aud=insert", args); // o AMF já põe AUD
        Assert.EndsWith("-f h264 -", args);
    }

    [Fact]
    public void X264RepeatsHeadersAndInsertsAud()
    {
        var args = string.Join(' ', FfmpegArgs.Desktop(0, Phone, "libx264"));
        Assert.Contains("-c:v libx264 -preset ultrafast -tune zerolatency -profile:v main -bf 0 -b:v 12M -g 60 -x264-params repeat-headers=1", args);
        Assert.Contains("-bsf:v h264_metadata=aud=insert", args);
    }
}
```

`FfmpegCaptureTests.cs`:

```csharp
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

[Trait("Category", "Local")]
public class FfmpegCaptureTests
{
    [Fact]
    public async Task EncodesTestPatternIntoAccessUnitsStartingWithAKeyframe()
    {
        var ffmpeg = FfmpegCapture.Locate() ?? throw new InvalidOperationException("FFmpeg não encontrado (winget install Gyan.FFmpeg)");
        var config = new VideoConfig(640, 296, 30);
        await using var capture = FfmpegCapture.Start(ffmpeg, FfmpegArgs.TestPattern(config, 30), config, _ => { });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var units = new List<byte[]>();
        await foreach (var unit in capture.AccessUnitsAsync(cts.Token))
        {
            units.Add(unit);
        }
        Assert.InRange(units.Count, 29, 30); // o último quadro só sai quando o FFmpeg fecha
        Assert.Contains(units[0].Skip(4).Take(64), b => (b & 0x1F) == 7); // SPS no primeiro
    }

    [Fact]
    public async Task ChoosesAWorkingEncoder() =>
        Assert.Contains(await FfmpegCapture.ChooseEncoderAsync(FfmpegCapture.Locate()!, CancellationToken.None), new[] { "h264_amf", "libx264" });
}
```

- [ ] **Step 2: Rodar — DEVE falhar.**

- [ ] **Step 3: `FfmpegArgs.cs`**

```csharp
using System.Globalization;

namespace TelaDois.Core.Video;

/// <summary>Linhas de comando do FFmpeg (testadas sem rodar o FFmpeg).</summary>
public static class FfmpegArgs
{
    public static IReadOnlyList<string> Desktop(int outputIndex, VideoConfig config, string encoder)
    {
        var fps = config.Fps.ToString(CultureInfo.InvariantCulture);
        // O h264_amf recusa BGRA e o scale_d3d11 falha neste driver: converte para NV12 na CPU.
        var filter = $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1,hwdownload,format=bgra,format=nv12";
        return ["-hide_banner", "-loglevel", "error", "-filter_complex", filter, "-an", .. Encoder(encoder, fps), "-f", "h264", "-"];
    }

    public static IReadOnlyList<string> TestPattern(VideoConfig config, int frames)
    {
        var fps = config.Fps.ToString(CultureInfo.InvariantCulture);
        var size = $"{config.Width}x{config.Height}";
        return ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"testsrc2=size={size}:rate={fps}",
            "-frames:v", frames.ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p", .. Encoder("libx264", fps), "-f", "h264", "-"];
    }

    /// <summary>Abre a saída <paramref name="outputIndex"/> por um quadro, para ler a resolução no log.</summary>
    public static IReadOnlyList<string> ProbeOutput(int outputIndex) =>
        ["-hide_banner", "-filter_complex", $"ddagrab=output_idx={outputIndex}", "-frames:v", "1", "-f", "null", "-"];

    private static IReadOnlyList<string> Encoder(string encoder, string fps) => encoder switch
    {
        "h264_amf" => ["-c:v", "h264_amf", "-usage", "ultralowlatency", "-bf", "0", "-b:v", "12M", "-g", fps, "-header_spacing", fps],
        _ => ["-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-profile:v", "main", "-bf", "0", "-b:v", "12M",
            "-g", fps, "-x264-params", "repeat-headers=1", "-bsf:v", "h264_metadata=aud=insert"],
    };
}
```

- [ ] **Step 4: `FfmpegCapture.cs`**

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace TelaDois.Core.Video;

/// <summary>Roda o FFmpeg e entrega o stdout separado em access units.</summary>
public sealed partial class FfmpegCapture : IVideoStream
{
    private readonly Process _process;
    private readonly Action<string> _log;

    private FfmpegCapture(Process process, VideoConfig config, Action<string> log)
    {
        _process = process;
        Config = config;
        _log = log;
    }

    public VideoConfig Config { get; }

    public static FfmpegCapture Start(string ffmpegPath, IReadOnlyList<string> args, VideoConfig config, Action<string> log)
    {
        var info = new ProcessStartInfo(ffmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }
        var process = Process.Start(info) ?? throw new IOException("não foi possível iniciar o FFmpeg");
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log($"ffmpeg: {e.Data}");
            }
        };
        process.BeginErrorReadLine();
        return new FfmpegCapture(process, config, log);
    }

    public async IAsyncEnumerable<byte[]> AccessUnitsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var splitter = new H264AccessUnitSplitter();
        var buffer = new byte[256 * 1024];
        var stdout = _process.StandardOutput.BaseStream;
        while (true)
        {
            var read = await stdout.ReadAsync(buffer, ct);
            if (read == 0)
            {
                break;
            }
            foreach (var unit in splitter.Append(buffer.AsSpan(0, read)))
            {
                yield return unit;
            }
        }
        // FFmpeg terminou: o que sobrou no separador é o último quadro.
        foreach (var unit in splitter.Append(Convert.FromHexString("0000000109F0000001419A02")).Take(1))
        {
            yield return unit;
        }
    }

    /// <summary>GOP de 1 s: o próximo keyframe chega em até 1 s (o FFmpeg não aceita pedido no meio).</summary>
    public void RequestKeyframe() => _log("iPhone pediu keyframe (próximo em até 1 s)");

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                // "q" no stdin encerra o FFmpeg com calma; se não sair, mata.
                await _process.StandardInput.WriteAsync('q');
                await _process.StandardInput.FlushAsync();
            }
            catch (IOException)
            {
            }
            using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await _process.WaitForExitAsync(wait.Token);
            }
            catch (OperationCanceledException)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        _process.Dispose();
    }

    /// <summary><c>h264_amf</c> se a GPU AMD codifica um quadro de teste; senão <c>libx264</c> (spec §5).</summary>
    public static async Task<string> ChooseEncoderAsync(string ffmpegPath, CancellationToken ct)
    {
        var info = new ProcessStartInfo(ffmpegPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=640x360:rate=30", "-frames:v", "5", "-pix_fmt", "nv12", "-c:v", "h264_amf", "-f", "null", "-" })
        {
            info.ArgumentList.Add(arg);
        }
        using var process = Process.Start(info)!;
        await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return process.ExitCode == 0 ? "h264_amf" : "libx264";
    }

    /// <summary>Índice do <c>ddagrab</c> cuja tela tem <paramref name="width"/>×<paramref name="height"/>.</summary>
    public static async Task<int?> FindOutputIndexAsync(string ffmpegPath, uint width, uint height, CancellationToken ct)
    {
        for (var index = 0; index < 6; index++)
        {
            var info = new ProcessStartInfo(ffmpegPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            foreach (var arg in FfmpegArgs.ProbeOutput(index))
            {
                info.ArgumentList.Add(arg);
            }
            using var process = Process.Start(info)!;
            var log = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0)
            {
                return null; // acabaram as saídas
            }
            var match = SizeRegex().Match(log);
            if (match.Success && uint.Parse(match.Groups[1].Value) == width && uint.Parse(match.Groups[2].Value) == height)
            {
                return index;
            }
        }
        return null;
    }

    /// <summary>FFmpeg no PATH ou instalado pelo winget (Gyan.FFmpeg).</summary>
    public static string? Locate()
    {
        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(folder.Trim(), "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        var winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
        return Directory.Exists(winget)
            ? Directory.EnumerateFiles(winget, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    [GeneratedRegex(@"Video: .*?, (\d{3,5})x(\d{3,5})")]
    private static partial Regex SizeRegex();
}
```

- [ ] **Step 5: Rodar — DEVE passar** (local, com FFmpeg). No `windows-test.yml`: `run: dotnet test windows/TelaDois.sln -c Release --filter "Category!=Local"`. Commit: "feat(windows): captura e codificação com o FFmpeg (AMF com reserva libx264)".

---

### Task 6: Monitor virtual e Segunda Tela no app de bandeja

**Files:**
- Create: `windows/src/TelaDois.Core/Display/DisplayManager.cs`
- Create: `windows/src/TelaDois.Core/Video/SecondScreenSource.cs`
- Modify: `windows/src/TelaDois.Core/UsbTunnelService.cs`, `TunnelStatusText.cs`, `windows/src/TelaDois.Tray/TrayContext.cs`
- Test: `windows/tests/TelaDois.Core.Tests/UsbTunnelServiceTests.cs` (texto da bandeja com vídeo)

**Interfaces:**
- Consumes: `IVideoSource`, `FfmpegCapture`, `FfmpegArgs`, `LinkStatus.Video`.
- Produces:
  - `static class DisplayManager { const string VirtualAdapterName = "Virtual Display"; static string? FindVirtualDisplay(); static void Attach(string deviceName, uint width, uint height, uint fps); static void Detach(string deviceName); static void DetachLeftover(Action<string> log) }` — lança `InvalidOperationException` com o código do Windows se falhar
  - `sealed class SecondScreenSource(string ffmpegPath, Action<string> log) : IVideoSource`
  - `UsbTunnelService(UsbmuxClient, LinkSessionOptions, Action<string> log, IVideoSource? video = null)`; `TunnelStatus` ganha `VideoConfig? Video = null`
  - Texto: `"TelaDois: segunda tela · 2556×1179 · 60 fps · 12 ms"`

- [ ] **Step 1: Teste do texto** — em `UsbTunnelServiceTests.cs`:

```csharp
    [Fact]
    public void DescribesSecondScreen() =>
        Assert.Equal("TelaDois: segunda tela · 2556×1179 · 60 fps · 12 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, new HelloInfo(1, 2556, 1179), TimeSpan.FromMilliseconds(12), new VideoConfig(2556, 1179, 60))));
```

- [ ] **Step 2: `DisplayManager.cs`**

```csharp
using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

/// <summary>
/// Liga e desliga o monitor do Virtual Display Driver no desktop do Windows. O driver mantém o
/// monitor instalado (não aceita 0 monitores); "desligar" é tirar do desktop (tamanho 0), o que faz
/// o Windows devolver as janelas ao monitor principal. Não precisa de administrador.
/// </summary>
public static partial class DisplayManager
{
    public const string VirtualAdapterName = "Virtual Display";

    private const int AttachedToDesktop = 0x1;
    private const int PrimaryDevice = 0x4;
    private const int DmPosition = 0x20;
    private const int DmPelsWidth = 0x80000;
    private const int DmPelsHeight = 0x100000;
    private const int DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1;
    private const uint CdsNoReset = 0x10000000;
    private const int DispChangeSuccessful = 0;

    /// <summary>Nome do dispositivo (ex.: <c>\\.\DISPLAY3</c>) do monitor virtual, ligado ou não.</summary>
    public static string? FindVirtualDisplay() =>
        Adapters().FirstOrDefault(a => a.DeviceString.Contains(VirtualAdapterName, StringComparison.OrdinalIgnoreCase)).DeviceName;

    public static void Attach(string deviceName, uint width, uint height, uint fps)
    {
        var primary = Adapters().First(a => (a.StateFlags & PrimaryDevice) != 0);
        var primaryMode = CurrentMode(primary.DeviceName);
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight | DmDisplayFrequency;
        mode.dmPositionX = primaryMode.dmPelsWidth; // à direita do principal
        mode.dmPositionY = 0;
        mode.dmPelsWidth = (int)width;
        mode.dmPelsHeight = (int)height;
        mode.dmDisplayFrequency = (int)fps;
        Apply(deviceName, ref mode, "ligar");
    }

    public static void Detach(string deviceName)
    {
        var mode = NewMode();
        mode.dmFields = DmPosition | DmPelsWidth | DmPelsHeight;
        Apply(deviceName, ref mode, "desligar");
    }

    /// <summary>Ao iniciar: desliga o monitor virtual que tenha sobrado de uma execução anterior (spec §5).</summary>
    public static void DetachLeftover(Action<string> log)
    {
        var adapter = Adapters().FirstOrDefault(a => a.DeviceString.Contains(VirtualAdapterName, StringComparison.OrdinalIgnoreCase));
        if (adapter.DeviceName is not null && (adapter.StateFlags & AttachedToDesktop) != 0)
        {
            Detach(adapter.DeviceName);
            log($"monitor virtual que sobrou foi desligado ({adapter.DeviceName})");
        }
    }

    private static void Apply(string deviceName, ref DevMode mode, string action)
    {
        var staged = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsUpdateRegistry | CdsNoReset, IntPtr.Zero);
        var applied = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (staged != DispChangeSuccessful || applied != DispChangeSuccessful)
        {
            throw new InvalidOperationException($"o Windows não deixou {action} o monitor virtual (códigos {staged}/{applied})");
        }
    }

    private static IEnumerable<DisplayDevice> Adapters()
    {
        for (uint i = 0; ; i++)
        {
            var device = new DisplayDevice { cb = Marshal.SizeOf<DisplayDevice>() };
            if (!EnumDisplayDevices(null, i, ref device, 0))
            {
                yield break;
            }
            yield return device;
        }
    }

    private static DevMode CurrentMode(string deviceName)
    {
        var mode = NewMode();
        EnumDisplaySettings(deviceName, -1, ref mode); // ENUM_CURRENT_SETTINGS
        return mode;
    }

    private static DevMode NewMode() => new() { dmSize = (short)Marshal.SizeOf<DevMode>() };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
    private static extern bool EnumDisplayDevices(string? device, uint index, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsW")]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode mode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref DevMode mode, IntPtr hwnd, uint flags, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, IntPtr mode, IntPtr hwnd, uint flags, IntPtr param);
}
```

(`TelaDois.Core.csproj` passa a `net10.0-windows` com `<SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>`; os testes também `net10.0-windows`, para o analisador de plataforma não reclamar de `user32`.)

- [ ] **Step 3: `SecondScreenSource.cs`**

```csharp
using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Liga o monitor virtual na resolução do iPhone, acha a saída no ddagrab e inicia o FFmpeg.</summary>
public sealed class SecondScreenSource(string ffmpegPath, Action<string> log) : IVideoSource
{
    public const ushort Fps = 60;
    private string? _encoder;

    public async Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct)
    {
        var device = DisplayManager.FindVirtualDisplay()
            ?? throw new InvalidOperationException("monitor virtual não encontrado: instale o Virtual Display Driver");
        DisplayManager.Attach(device, hello.Width, hello.Height, Fps);
        log($"monitor virtual ligado ({device}, {hello.Width}×{hello.Height})");
        try
        {
            _encoder ??= await FfmpegCapture.ChooseEncoderAsync(ffmpegPath, ct);
            int? output = null;
            for (var attempt = 0; attempt < 10 && output is null; attempt++)
            {
                await Task.Delay(300, ct); // o Windows leva um instante para criar a saída
                output = await FfmpegCapture.FindOutputIndexAsync(ffmpegPath, hello.Width, hello.Height, ct);
            }
            if (output is null)
            {
                throw new InvalidOperationException($"o ddagrab não achou uma tela de {hello.Width}×{hello.Height}");
            }
            var config = new VideoConfig(hello.Width, hello.Height, Fps);
            log($"capturando a saída {output} com {_encoder}");
            var capture = FfmpegCapture.Start(ffmpegPath, FfmpegArgs.Desktop(output.Value, config, _encoder), config, log);
            return new DetachOnDispose(capture, device, log);
        }
        catch
        {
            DisplayManager.Detach(device);
            throw;
        }
    }

    private sealed class DetachOnDispose(IVideoStream inner, string device, Action<string> log) : IVideoStream
    {
        public VideoConfig Config => inner.Config;

        public IAsyncEnumerable<byte[]> AccessUnitsAsync(CancellationToken ct) => inner.AccessUnitsAsync(ct);

        public void RequestKeyframe() => inner.RequestKeyframe();

        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            DisplayManager.Detach(device);
            log("monitor virtual desligado");
        }
    }
}
```

- [ ] **Step 4: Ligar ao serviço e à bandeja**
  - `TunnelStatus`: `public sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null);`
  - `UsbTunnelService`: parâmetro `IVideoSource? video = null` no construtor primário; `LinkSession.RunAsync(tunnel, status => Report(new TunnelStatus(TunnelState.Connected, status.Hello, status.RoundTrip, status.Video)), options, ct, video)`; capturar `InvalidOperationException` junto com `IOException` em `RunDeviceAsync` (log + nova tentativa).
  - `TunnelStatusText`: primeiro caso conectado: `_ when status is { Hello: { } h, RoundTrip: { } rtt, Video: { } v } => $"TelaDois: segunda tela · {v.Width}×{v.Height} · {v.Fps} fps · {Math.Round(rtt.TotalMilliseconds):0} ms"`.
  - `TrayContext`: `DisplayManager.DetachLeftover(_log.Write)` antes de criar o serviço; `var ffmpeg = FfmpegCapture.Locate();` — se `null`, registra "FFmpeg não encontrado: só o túnel, sem vídeo" e passa `video: null`; senão `new SecondScreenSource(ffmpeg, _log.Write)`. Balão ao conectar: "Segunda tela ligada".

- [ ] **Step 5: Testes, publicação e commit.** `dotnet test` (todos), `dotnet publish ... -o builds/windows`. Commit: "feat(windows): monitor virtual + FFmpeg viram a Segunda Tela do iPhone".

---

### Task 7: Instalar o driver, testar com o iPhone, documentação e `main`

- [ ] **Step 1: Baixar e conferir o driver** — release 25.7.23 de https://github.com/VirtualDrivers/Virtual-Display-Driver (`VDD.Control.25.7.23.zip`), numa pasta nova em `builds/vdd/`. Conferir assinatura do `.cat`/`.sys` com `Get-AuthenticodeSignature` (deve ser válida, assinada pela Microsoft/WHQL ou pelo SignPath do projeto) e o hash publicado na release, se houver. Registrar o resultado.
- [ ] **Step 2: Usuário instala** pelo VDD Control (pede administrador — o usuário faz). Depois, com o usuário: em `C:\VirtualDisplayDriver\vdd_settings.xml`, `<count>1</count>` e uma resolução `2556`×`1179` @ `60` (o usuário confirma a edição se pedir administrador), e "Reload Driver" no VDD Control.
- [ ] **Step 3: Conferir no PC sem iPhone** — `DisplayManager.FindVirtualDisplay()` acha o monitor; ligar/desligar a mão funciona; `ffmpeg -filter_complex ddagrab=output_idx=N -frames:v 1 -f null -` mostra 2556x1179 para o N certo.
- [ ] **Step 4: `.ipa` da Task 2 (CI verde, inspecionado) instalado pelo xtool**; `TelaDois.exe` novo rodando; usuário abre o app no cabo. Expected: o Windows ganha um monitor à direita; arrastar uma janela para lá aparece no iPhone; bandeja "segunda tela · 2556×1179 · 60 fps · N ms"; tirar o cabo → monitor some e as janelas voltam; app para o fundo → idem; voltar → monitor volta. Anotar latência e fps reais, e o uso de CPU do FFmpeg.
- [ ] **Step 5: Documentação** (`CLAUDE.md`: estado, como rodar, decisões de monitor virtual/FFmpeg/AMF; `docs/INSTALACAO.md`: driver e FFmpeg; spec §8 linha da Etapa 5), commit, push, CI verde, `main` por fast-forward.
