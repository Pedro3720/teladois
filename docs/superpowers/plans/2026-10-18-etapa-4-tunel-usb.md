# Etapa 4 — Túnel USB (PC ↔ iPhone pelo cabo): Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Com o iPhone no cabo e o app aberto, o app de bandeja do Windows detecta o iPhone, abre um túnel pelo usbmuxd até a porta 7100 do app, recebe o `HELLO` (resolução da tela) e troca `PING`/`PONG` a cada 1 s, mostrando "conectado · 2556×1179 · 12 ms" na bandeja e "PC conectado" no iPhone.

**Architecture:** O protocolo (enquadramento `[tipo][tamanho BE][dados]`, `HELLO`, `PING`/`PONG`, `BYE`) é implementado duas vezes — Swift no pacote `TelaDoisCore` e C# em `TelaDois.Core` — e as duas pontas são presas pelos mesmos bytes de referência ("golden bytes") nos testes. No iPhone, `UsbLink` escuta em `127.0.0.1:7100` (o usbmuxd entrega o túnel no loopback do aparelho). No PC, `UsbmuxClient` fala com o Apple Mobile Device Service em `127.0.0.1:27015`, `LinkSession` conduz a conversa e `UsbTunnelService` liga tudo (iPhone plugado → tenta conectar a cada 2 s → sessão → volta a esperar).

**Tech Stack:** Swift 6 (pacote) / Swift 5 + concorrência estrita (app), Network.framework (`NWListener`), XCTest/XCUITest; C# 14 / .NET 10 (SDK fixado em `windows/global.json`), WinForms (`NotifyIcon`), xUnit; GitHub Actions `macos-15` (iOS) e `windows-2025` (PC).

Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§3.1, §3.3, §3.4, §4.1, §5, §7).

**Fora desta etapa:** monitor virtual, FFmpeg, `CONFIG`, `VIDEO`, `KEYFRAME_REQ` e o estado "Segunda Tela" no iPhone (Etapa 5); ícone próprio da bandeja, iniciar com o Windows e latência ajustada (Etapa 6); Spotify (Etapa 3, adiada pelo usuário em 2026-10-18).

## Global Constraints

- Mensagem: `[tipo: 1 byte][tamanho: 4 bytes, big-endian][dados]`; tamanho máximo dos dados **16 MiB** (`16 * 1024 * 1024`); acima disso a conexão é encerrada.
- Tipos: `HELLO=1`, `CONFIG=2`, `VIDEO=3`, `KEYFRAME_REQ=4`, `PING=5`, `PONG=6`, `BYE=7`. Tipo desconhecido é ignorado.
- `HELLO` (iPhone → PC): versão `UInt16` BE (= **1**), largura `UInt32` BE, altura `UInt32` BE — em pixels, **paisagem** (largura > altura).
- `PING`/`PONG`: `UInt64` BE = carimbo de tempo em ms de quem mandou o `PING`; o `PONG` devolve o mesmo valor.
- `BYE`: sem dados.
- Bytes de referência (iguais nas duas linguagens): `HELLO(2556, 1179)` = `010000000A0001000009FC0000049B`; `PING(1234567890123)` = `05000000080000011F71FB04CB`.
- Porta do app no iPhone: **7100**, só em `127.0.0.1`; listener ligado só com o app em primeiro plano (para quando o app vai para o fundo — `scenePhase == .background`).
- O PC manda `PING` a cada **1 s**; os dois lados encerram após **3 s** sem receber nada.
- usbmuxd: `127.0.0.1:27015`; cabeçalho de 16 bytes **little-endian** (tamanho total, versão = 1, tipo = 8 [plist], tag); corpo em plist XML; `PortNumber` vai com os bytes trocados (7100 → **48155**); `Result` `Number` 0 = ok, 3 = conexão recusada (app fechado).
- iOS: zero avisos com `SWIFT_STRICT_CONCURRENCY: complete`; nunca rodar `xcodebuild`/`xcodegen`/`swift` localmente — o CI verifica.
- PC: `Nullable` ligado e `TreatWarningsAsErrors` (zero avisos); testes rodam localmente com `dotnet test` e no CI `windows-test`.
- Textos para o usuário em português; identificadores em inglês. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- No Windows, não editar arquivos com PowerShell `Get-Content`/`Set-Content` (corrompe UTF-8). Mensagens de commit com acento: gravar num arquivo e usar `git commit -F`.
- Branch: **`etapa-4-tunel-usb`**; juntar ao `main` (fast-forward) só com `ios-test`, `ios-build` e `windows-test` verdes no SHA final.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift` | Tipos, `LinkFrame`, `LinkFrameDecoder`, fábricas/parse de `HELLO`/`PING`/`PONG`/`BYE` |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift` | Lado iPhone, puro: responde `PING`, fecha no `BYE`, tempo-limite de 3 s |
| `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift` | Bytes de referência, decoder |
| `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift` | Respostas e tempo-limite |
| `ios/TelaDois/UsbLink.swift` | `NWListener` em 127.0.0.1:7100, uma conexão por vez, `isConnected` |
| `ios/TelaDois/ClockPageView.swift` | + rótulo "PC conectado" (`link-status`) |
| `ios/TelaDois/RestScreenView.swift` | + `UsbLink` ligado ao ciclo de vida |
| `ios/TelaDoisUITests/FakePC.swift` | Socket BSD que faz o papel do PC nos testes de UI |
| `ios/TelaDoisUITests/RestScreenUITests.swift` | + 2 testes do túnel e print `04-pc-conectado` |
| `ios/project.yml` | Testes de UI dependem do `TelaDoisCore`; versão 0.3.0 |
| `windows/global.json`, `windows/Directory.Build.props` | SDK fixo, nullable, avisos = erros |
| `windows/TelaDois.sln` | Solução |
| `windows/src/TelaDois.Core/Link/*.cs` | Protocolo e sessão do lado PC |
| `windows/src/TelaDois.Core/Usbmux/*.cs` | Plist, pacote e cliente do usbmuxd |
| `windows/src/TelaDois.Core/UsbTunnelService.cs`, `TunnelStatusText.cs` | Orquestração e textos da bandeja |
| `windows/src/TelaDois.Tray/*.cs` | App de bandeja (`TelaDois.exe`), registro em `%LOCALAPPDATA%\TelaDois\tela-dois.log` |
| `windows/tests/TelaDois.Core.Tests/**` | xUnit + usbmuxd e iPhone falsos |
| `.github/workflows/windows-test.yml` | `dotnet test` no `windows-2025` |

---

### Task 1: Protocolo do túnel no pacote `TelaDoisCore` (TDD)

**Files:**
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift`
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift`

**Interfaces:**
- Produces:
  - `enum LinkMessageType: UInt8 { hello = 1, config, video, keyframeRequest, ping, pong, bye }`
  - `struct LinkFrame: Equatable, Sendable { let type: UInt8; let payload: [UInt8]; init(type:payload:); init(_:payload:); var messageType: LinkMessageType?; func encoded() -> [UInt8] }`
  - `struct LinkHello: Equatable, Sendable { version: UInt16; width: UInt32; height: UInt32 }`
  - `enum LinkProtocol { version: UInt16 = 1; port: UInt16 = 7100; maxPayload = 16 MiB; timeout: TimeInterval = 3; hello(width:height:); parseHello(_:) -> LinkHello?; ping(_:); pong(_:); bye(); timestamp(of:) -> UInt64? }`
  - `struct LinkFrameDecoder: Sendable { mutating func append(_ bytes: some Sequence<UInt8>) throws -> [LinkFrame] }` (lança `LinkDecodeError.payloadTooLarge`)
  - `struct LinkSessionState: Sendable { enum Action { send(LinkFrame), close }; init(now:timeout:); mutating func receive(_:now:) -> [Action]; func isTimedOut(now:) -> Bool }`

- [ ] **Step 1: Escrever os testes**

`LinkProtocolTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class LinkProtocolTests: XCTestCase {
    private func hex(_ string: String) -> [UInt8] {
        stride(from: 0, to: string.count, by: 2).map { offset in
            let start = string.index(string.startIndex, offsetBy: offset)
            return UInt8(string[start..<string.index(start, offsetBy: 2)], radix: 16)!
        }
    }

    func testHelloMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.hello(width: 2556, height: 1179).encoded(), hex("010000000A0001000009FC0000049B"))
    }

    func testPingMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.ping(1_234_567_890_123).encoded(), hex("05000000080000011F71FB04CB"))
    }

    func testParsesHello() {
        XCTAssertEqual(
            LinkProtocol.parseHello(LinkProtocol.hello(width: 2622, height: 1206)),
            LinkHello(version: 1, width: 2622, height: 1206)
        )
    }

    func testRejectsHelloWithWrongSize() {
        XCTAssertNil(LinkProtocol.parseHello(LinkFrame(.hello, payload: [0, 1])))
    }

    func testPongCarriesTimestamp() {
        XCTAssertEqual(LinkProtocol.timestamp(of: LinkProtocol.pong(42)), 42)
        XCTAssertNil(LinkProtocol.timestamp(of: LinkProtocol.bye()))
    }

    func testDecoderHandlesSplitAndBatchedFrames() throws {
        let bytes = LinkProtocol.hello(width: 2556, height: 1179).encoded() + LinkProtocol.ping(7).encoded()
        var decoder = LinkFrameDecoder()
        let first = try decoder.append(bytes[0..<3])
        XCTAssertEqual(first, [])
        let rest = try decoder.append(bytes[3...])
        XCTAssertEqual(rest.map(\.messageType), [.hello, .ping])
        let none = try decoder.append([UInt8]())
        XCTAssertEqual(none, [])
    }

    func testDecoderRejectsHugePayload() {
        var decoder = LinkFrameDecoder()
        XCTAssertThrowsError(try decoder.append(hex("03FFFFFFFF")))
    }

    func testUnknownTypeHasNoMessageType() {
        XCTAssertNil(LinkFrame(type: 99).messageType)
    }
}
```

`LinkSessionStateTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class LinkSessionStateTests: XCTestCase {
    private let t0 = Date(timeIntervalSince1970: 1_000_000)

    func testPingIsAnsweredWithPongCarryingSameTimestamp() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.ping(99), now: t0), [.send(LinkProtocol.pong(99))])
    }

    func testByeClosesTheSession() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.bye(), now: t0), [.close])
    }

    func testOtherFramesNeedNoReply() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkFrame(type: 99), now: t0), [])
    }

    func testTimesOutAfterThreeSecondsOfSilence() {
        let state = LinkSessionState(now: t0)
        XCTAssertFalse(state.isTimedOut(now: t0.addingTimeInterval(3)))
        XCTAssertTrue(state.isTimedOut(now: t0.addingTimeInterval(3.01)))
    }

    func testAnyFrameRefreshesTheTimeout() {
        var state = LinkSessionState(now: t0)
        _ = state.receive(LinkFrame(type: 99), now: t0.addingTimeInterval(2))
        XCTAssertFalse(state.isTimedOut(now: t0.addingTimeInterval(4.5)))
    }
}
```

- [ ] **Step 2: Commit e push — CI DEVE falhar** (não compila: tipos inexistentes)

```bash
git add ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift
git commit -F <arquivo com: "test(core): protocolo do túnel USB (ainda sem implementação)">
git push -u origin etapa-4-tunel-usb
```

Expected: `ios-test` vermelho no passo "Testes do pacote TelaDoisCore" (cannot find 'LinkProtocol' in scope).

- [ ] **Step 3: Implementar `LinkProtocol.swift`**

```swift
import Foundation

/// Tipos de mensagem do protocolo PC ↔ iPhone (spec §3.4).
public enum LinkMessageType: UInt8, Sendable {
    case hello = 1, config, video, keyframeRequest, ping, pong, bye
}

/// Uma mensagem: `[tipo: 1 byte][tamanho: 4 bytes big-endian][dados]`.
public struct LinkFrame: Equatable, Sendable {
    public static let headerSize = 5
    public let type: UInt8
    public let payload: [UInt8]

    public init(type: UInt8, payload: [UInt8] = []) {
        self.type = type
        self.payload = payload
    }

    public init(_ type: LinkMessageType, payload: [UInt8] = []) {
        self.init(type: type.rawValue, payload: payload)
    }

    /// `nil` para tipos desconhecidos (versões futuras), que são ignorados.
    public var messageType: LinkMessageType? { LinkMessageType(rawValue: type) }

    public func encoded() -> [UInt8] {
        [type] + bigEndianBytes(UInt32(payload.count)) + payload
    }
}

public struct LinkHello: Equatable, Sendable {
    public let version: UInt16
    public let width: UInt32
    public let height: UInt32

    public init(version: UInt16, width: UInt32, height: UInt32) {
        self.version = version
        self.width = width
        self.height = height
    }
}

public enum LinkProtocol {
    public static let version: UInt16 = 1
    public static let port: UInt16 = 7100
    public static let maxPayload = 16 * 1024 * 1024
    /// Sem receber nada por este tempo, a conexão é encerrada.
    public static let timeout: TimeInterval = 3

    public static func hello(width: UInt32, height: UInt32) -> LinkFrame {
        LinkFrame(.hello, payload: bigEndianBytes(version) + bigEndianBytes(width) + bigEndianBytes(height))
    }

    public static func parseHello(_ frame: LinkFrame) -> LinkHello? {
        guard frame.messageType == .hello, frame.payload.count == 10 else { return nil }
        let bytes = frame.payload[...]
        return LinkHello(
            version: readBigEndian(bytes[0..<2]),
            width: readBigEndian(bytes[2..<6]),
            height: readBigEndian(bytes[6..<10])
        )
    }

    public static func ping(_ timestamp: UInt64) -> LinkFrame {
        LinkFrame(.ping, payload: bigEndianBytes(timestamp))
    }

    public static func pong(_ timestamp: UInt64) -> LinkFrame {
        LinkFrame(.pong, payload: bigEndianBytes(timestamp))
    }

    public static func bye() -> LinkFrame { LinkFrame(.bye) }

    /// Carimbo de tempo de um `PING` ou `PONG`.
    public static func timestamp(of frame: LinkFrame) -> UInt64? {
        guard frame.messageType == .ping || frame.messageType == .pong, frame.payload.count == 8 else { return nil }
        return readBigEndian(frame.payload[...])
    }
}

public enum LinkDecodeError: Error, Equatable {
    case payloadTooLarge(UInt32)
}

/// Junta os bytes que chegam em pedaços e devolve as mensagens completas.
public struct LinkFrameDecoder: Sendable {
    private var buffer: [UInt8] = []

    public init() {}

    public mutating func append(_ bytes: some Sequence<UInt8>) throws -> [LinkFrame] {
        buffer.append(contentsOf: bytes)
        var frames: [LinkFrame] = []
        var offset = 0
        while buffer.count - offset >= LinkFrame.headerSize {
            let length: UInt32 = readBigEndian(buffer[(offset + 1)..<(offset + 5)])
            guard length <= LinkProtocol.maxPayload else { throw LinkDecodeError.payloadTooLarge(length) }
            let end = offset + LinkFrame.headerSize + Int(length)
            guard buffer.count >= end else { break }
            frames.append(LinkFrame(type: buffer[offset], payload: Array(buffer[(offset + LinkFrame.headerSize)..<end])))
            offset = end
        }
        if offset > 0 { buffer.removeFirst(offset) }
        return frames
    }
}

func bigEndianBytes<T: FixedWidthInteger>(_ value: T) -> [UInt8] {
    withUnsafeBytes(of: value.bigEndian) { Array($0) }
}

func readBigEndian<T: FixedWidthInteger>(_ bytes: ArraySlice<UInt8>) -> T {
    bytes.reduce(T.zero) { ($0 << 8) | T($1) }
}
```

- [ ] **Step 4: Implementar `LinkSessionState.swift`**

```swift
import Foundation

/// Lado do iPhone na conversa com o PC: responde `PING` com `PONG`, encerra no `BYE`
/// e considera a conexão morta após `timeout` segundos sem receber nada.
public struct LinkSessionState: Sendable {
    public enum Action: Equatable, Sendable {
        case send(LinkFrame)
        case close
    }

    public let timeout: TimeInterval
    public private(set) var lastHeard: Date

    public init(now: Date, timeout: TimeInterval = LinkProtocol.timeout) {
        self.timeout = timeout
        self.lastHeard = now
    }

    public mutating func receive(_ frame: LinkFrame, now: Date) -> [Action] {
        lastHeard = now
        switch frame.messageType {
        case .ping:
            return LinkProtocol.timestamp(of: frame).map { [.send(LinkProtocol.pong($0))] } ?? []
        case .bye:
            return [.close]
        default:
            return []
        }
    }

    public func isTimedOut(now: Date) -> Bool {
        now.timeIntervalSince(lastHeard) > timeout
    }
}
```

- [ ] **Step 5: Commit e push — CI DEVE passar**

```bash
git add ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift
git commit -F <arquivo com: "feat(core): protocolo do túnel USB (quadros, HELLO, PING/PONG, BYE)">
git push
```

Expected: `ios-test` verde (13 + 13 testes do pacote; testes de UI inalterados) e `ios-build` verde.

---

### Task 2: App do iPhone escuta o PC (`UsbLink`) e mostra "PC conectado"

**Files:**
- Create: `ios/TelaDois/UsbLink.swift`
- Create: `ios/TelaDoisUITests/FakePC.swift`
- Modify: `ios/TelaDois/ClockPageView.swift` (novo parâmetro `isLinked`)
- Modify: `ios/TelaDois/RestScreenView.swift` (liga `UsbLink`)
- Modify: `ios/TelaDoisUITests/RestScreenUITests.swift` (2 testes)
- Modify: `ios/project.yml` (testes de UI dependem do pacote; versão 0.3.0)

**Interfaces:**
- Consumes: `LinkProtocol`, `LinkFrameDecoder`, `LinkSessionState` (Task 1).
- Produces: `@MainActor @Observable final class UsbLink { private(set) var isConnected: Bool; func start(); func stop() }`; elemento de acessibilidade `link-status` (texto "PC conectado") visível só com o PC conectado; print `04-pc-conectado`.

- [ ] **Step 1: `project.yml`** — em `targets.TelaDoisUITests` acrescente a dependência do pacote e suba a versão:

```yaml
  TelaDoisUITests:
    type: bundle.ui-testing
    platform: iOS
    sources: [TelaDoisUITests]
    dependencies:
      - target: TelaDois
      - package: TelaDoisCore
```

e em `settings.base`: `MARKETING_VERSION: "0.3.0"`.

- [ ] **Step 2: Criar `FakePC.swift`** (socket BSD síncrono; no simulador o app e o teste dividem o loopback do Mac)

```swift
import Darwin
import Foundation
import TelaDoisCore

/// Faz o papel do PC nos testes de UI: conecta em 127.0.0.1:7100, onde o app escuta.
final class FakePC {
    private let socketFD: Int32
    private var decoder = LinkFrameDecoder()
    private var pending: [LinkFrame] = []

    private init(socketFD: Int32) {
        self.socketFD = socketFD
    }

    deinit {
        close(socketFD)
    }

    /// Tenta conectar por até `timeout` segundos (o listener sobe logo depois que o app abre).
    static func connect(port: UInt16 = LinkProtocol.port, timeout: TimeInterval = 5) -> FakePC? {
        let deadline = Date().addingTimeInterval(timeout)
        repeat {
            let fd = socket(AF_INET, SOCK_STREAM, 0)
            if fd >= 0 {
                var address = sockaddr_in()
                address.sin_family = sa_family_t(AF_INET)
                address.sin_port = port.bigEndian
                address.sin_addr.s_addr = inet_addr("127.0.0.1")
                let result = withUnsafePointer(to: &address) {
                    $0.withMemoryRebound(to: sockaddr.self, capacity: 1) {
                        Darwin.connect(fd, $0, socklen_t(MemoryLayout<sockaddr_in>.size))
                    }
                }
                if result == 0 {
                    var receiveTimeout = timeval(tv_sec: 5, tv_usec: 0)
                    _ = setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &receiveTimeout, socklen_t(MemoryLayout<timeval>.size))
                    return FakePC(socketFD: fd)
                }
                close(fd)
            }
            Thread.sleep(forTimeInterval: 0.2)
        } while Date() < deadline
        return nil
    }

    func send(_ frame: LinkFrame) {
        let bytes = frame.encoded()
        _ = bytes.withUnsafeBytes { Darwin.send(socketFD, $0.baseAddress, $0.count, 0) }
    }

    /// Próxima mensagem do app; `nil` se a conexão fechou ou ficou 5 s sem dados.
    func readFrame() -> LinkFrame? {
        while pending.isEmpty {
            var buffer = [UInt8](repeating: 0, count: 4096)
            let count = recv(socketFD, &buffer, buffer.count, 0)
            guard count > 0 else { return nil }
            guard let frames = try? decoder.append(buffer[0..<count]) else { return nil }
            pending += frames
        }
        return pending.removeFirst()
    }
}
```

- [ ] **Step 3: Testes de UI** — acrescente em `RestScreenUITests`:

```swift
    func testPcConnectionShowsStatusAndAnswersPing() {
        let app = launch()
        XCTAssertFalse(element(app, "link-status").exists, "sem PC não deve aparecer o aviso")
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }

        let hello = pc.readFrame().flatMap(LinkProtocol.parseHello)
        XCTAssertEqual(hello?.version, LinkProtocol.version)
        XCTAssertGreaterThan(hello?.width ?? 0, hello?.height ?? 0, "HELLO deve vir em paisagem")
        XCTAssertTrue(element(app, "link-status").waitForExistence(timeout: 5), "deveria mostrar PC conectado")

        pc.send(LinkProtocol.ping(42))
        let pong = pc.readFrame()
        XCTAssertEqual(pong?.messageType, .pong)
        XCTAssertEqual(pong.flatMap(LinkProtocol.timestamp(of:)), 42)
        attachScreenshot("04-pc-conectado")

        pc.send(LinkProtocol.bye())
        XCTAssertTrue(element(app, "link-status").waitForNonExistence(timeout: 3), "BYE deveria desconectar")
    }

    func testSilentPcIsDroppedAfterTimeout() {
        let app = launch()
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))
        XCTAssertTrue(element(app, "link-status").waitForExistence(timeout: 5))
        // Sem PING por mais de 3 s, o app fecha a conexão (recv devolve 0 antes dos 5 s do teste).
        XCTAssertNil(pc.readFrame(), "o app deveria fechar a conexão de um PC calado")
        XCTAssertTrue(element(app, "link-status").waitForNonExistence(timeout: 2))
    }
```

e no topo do arquivo: `import TelaDoisCore`.

- [ ] **Step 4: Commit e push — CI DEVE falhar**

```bash
git add ios/project.yml ios/TelaDoisUITests/FakePC.swift ios/TelaDoisUITests/RestScreenUITests.swift
git commit -F <arquivo com: "test(ui): túnel USB — HELLO, PING/PONG, BYE e tempo-limite (ainda sem implementação)">
git push
```

Expected: `ios-test` vermelho só nos 2 testes novos ("o app não aceitou conexão em 127.0.0.1:7100").

- [ ] **Step 5: Criar `UsbLink.swift`**

```swift
import Foundation
import Network
import Observation
import TelaDoisCore
import UIKit

/// Escuta o PC pelo cabo: o usbmuxd entrega o túnel em 127.0.0.1:7100 no iPhone.
/// Manda HELLO, responde PING com PONG e fecha no BYE, em erro ou após 3 s sem notícias do PC.
@MainActor
@Observable
final class UsbLink {
    private(set) var isConnected = false

    @ObservationIgnored private var listener: NWListener?
    @ObservationIgnored private var connection: NWConnection?
    @ObservationIgnored private var decoder = LinkFrameDecoder()
    @ObservationIgnored private var session = LinkSessionState(now: Date())
    @ObservationIgnored private var watchdog: Task<Void, Never>?

    func start() {
        guard listener == nil, let port = NWEndpoint.Port(rawValue: LinkProtocol.port) else { return }
        let parameters = NWParameters.tcp
        parameters.allowLocalEndpointReuse = true
        // Só loopback: nada exposto no Wi-Fi e sem pedido de "rede local".
        parameters.requiredLocalEndpoint = .hostPort(host: .ipv4(.loopback), port: port)
        guard let listener = try? NWListener(using: parameters) else { return }
        listener.newConnectionHandler = { [weak self] newConnection in
            MainActor.assumeIsolated { self?.accept(newConnection) }
        }
        listener.stateUpdateHandler = { [weak self] state in
            guard case .failed = state else { return }
            MainActor.assumeIsolated {
                self?.listener?.cancel()
                self?.listener = nil
            }
        }
        listener.start(queue: .main)
        self.listener = listener
    }

    /// App foi para o fundo: o PC percebe o socket fechado e encerra a sessão.
    func stop() {
        disconnect()
        listener?.cancel()
        listener = nil
    }

    private func accept(_ newConnection: NWConnection) {
        disconnect()
        connection = newConnection
        decoder = LinkFrameDecoder()
        let id = ObjectIdentifier(newConnection)
        newConnection.stateUpdateHandler = { [weak self] state in
            MainActor.assumeIsolated {
                guard let self, self.isCurrent(id) else { return }
                switch state {
                case .ready: self.didConnect(id)
                case .failed, .cancelled: self.disconnect()
                default: break
                }
            }
        }
        newConnection.start(queue: .main)
    }

    private func isCurrent(_ id: ObjectIdentifier) -> Bool {
        connection.map { ObjectIdentifier($0) == id } ?? false
    }

    private func didConnect(_ id: ObjectIdentifier) {
        let size = Self.landscapePixels()
        session = LinkSessionState(now: Date())
        send(LinkProtocol.hello(width: size.width, height: size.height))
        isConnected = true
        receiveNext(id)
        watchdog = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(500))
                guard let self, self.isCurrent(id) else { return }
                if self.session.isTimedOut(now: Date()) {
                    self.disconnect()
                    return
                }
            }
        }
    }

    private func receiveNext(_ id: ObjectIdentifier) {
        connection?.receive(minimumIncompleteLength: 1, maximumLength: 64 * 1024) { [weak self] data, _, isComplete, error in
            MainActor.assumeIsolated {
                guard let self, self.isCurrent(id) else { return }
                if let data, !data.isEmpty, !self.handle(data) { return }
                if isComplete || error != nil {
                    self.disconnect()
                } else {
                    self.receiveNext(id)
                }
            }
        }
    }

    /// Processa os bytes recebidos; devolve `false` se a conexão foi encerrada.
    private func handle(_ data: Data) -> Bool {
        let frames: [LinkFrame]
        do {
            frames = try decoder.append(data)
        } catch {
            disconnect()
            return false
        }
        for frame in frames {
            for action in session.receive(frame, now: Date()) {
                switch action {
                case .send(let reply):
                    send(reply)
                case .close:
                    disconnect()
                    return false
                }
            }
        }
        return true
    }

    private func send(_ frame: LinkFrame) {
        connection?.send(content: Data(frame.encoded()), completion: .contentProcessed { _ in })
    }

    private func disconnect() {
        watchdog?.cancel()
        watchdog = nil
        connection?.stateUpdateHandler = nil
        connection?.cancel()
        connection = nil
        isConnected = false
    }

    /// Resolução nativa em paisagem (largura > altura), em pixels.
    private static func landscapePixels() -> (width: UInt32, height: UInt32) {
        let screen = UIApplication.shared.connectedScenes.compactMap { ($0 as? UIWindowScene)?.screen }.first
        let size = screen?.nativeBounds.size ?? .zero
        return (UInt32(max(size.width, size.height)), UInt32(min(size.width, size.height)))
    }
}
```

- [ ] **Step 6: `ClockPageView.swift`** — novo parâmetro depois de `isCharging`:

```swift
    /// PC conectado pelo cabo (Etapa 4: só o aviso; a Segunda Tela vem na Etapa 5).
    let isLinked: Bool
```

e na coluna da direita, depois do `Spacer(minLength: 0)` do `VStack`:

```swift
                    if isLinked {
                        Text("PC conectado")
                            .font(.system(size: height * 0.06, weight: .semibold, design: .rounded))
                            .foregroundStyle(Theme.muted)
                            .lineLimit(1)
                            .accessibilityIdentifier("link-status")
                    }
```

- [ ] **Step 7: `RestScreenView.swift`** — estado, parâmetro e ciclo de vida:

```swift
    @State private var link = UsbLink()
```

```swift
                ClockPageView(
                    clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                    temperature: weather.temperatureLabel,
                    batteryLevel: battery.level,
                    isCharging: battery.isCharging,
                    isLinked: link.isConnected,
                    safeArea: geo.safeAreaInsets
                )
```

```swift
        .onAppear {
            battery.start()
            weather.start()
            link.start()
            UIApplication.shared.isIdleTimerDisabled = true
        }
        .onChange(of: scenePhase) { _, phase in
            UIApplication.shared.isIdleTimerDisabled = (phase == .active)
            if phase == .active {
                dim.touch(now: Date())
                link.start()
            } else {
                brightness.restore()
            }
            // Só no fundo: puxar a Central de Controle (inactive) não derruba o PC.
            if phase == .background { link.stop() }
        }
```

- [ ] **Step 8: Commit e push — CI DEVE passar**

```bash
git add ios/TelaDois/UsbLink.swift ios/TelaDois/ClockPageView.swift ios/TelaDois/RestScreenView.swift
git commit -F <arquivo com: "feat: app escuta o PC em 127.0.0.1:7100 (HELLO, PING/PONG, BYE, tempo-limite) e mostra PC conectado">
git push
```

Expected: `ios-test` verde (5 testes de UI) e `ios-build` verde, zero avisos.

- [ ] **Step 9: Revisar o print `04-pc-conectado.png`**

Expected: igual ao `01-relogio.png` mais "PC conectado" em cinza (`muted`) no pé da coluna da direita, sem cortar e sem encostar no canto.

---

### Task 3: Solução .NET e protocolo do lado PC (TDD, local)

**Files:**
- Create: `windows/global.json`, `windows/Directory.Build.props`, `windows/TelaDois.sln`
- Create: `windows/src/TelaDois.Core/TelaDois.Core.csproj` (via `dotnet new`)
- Create: `windows/src/TelaDois.Core/Link/LinkFrame.cs`, `LinkFrameDecoder.cs`, `LinkFrameReader.cs`, `LinkMessages.cs`
- Create: `windows/tests/TelaDois.Core.Tests/TelaDois.Core.Tests.csproj` (via `dotnet new`)
- Test: `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs`
- Create: `.github/workflows/windows-test.yml`

**Interfaces:**
- Produces (namespace `TelaDois.Core.Link`):
  - `enum LinkMessageType : byte { Hello = 1, Config, Video, KeyframeRequest, Ping, Pong, Bye }`
  - `sealed record LinkFrame(byte Type, byte[] Payload) { const int HeaderSize = 5; const int MaxPayload; LinkFrame(LinkMessageType, byte[]? = null); LinkMessageType? MessageType; byte[] Encode() }`
  - `sealed class LinkFrameDecoder { IReadOnlyList<LinkFrame> Append(ReadOnlySpan<byte>) }` (lança `InvalidDataException`)
  - `sealed class LinkFrameReader(Stream) { Task<LinkFrame?> NextAsync(CancellationToken) }` (`null` = conexão fechada)
  - `readonly record struct HelloInfo(ushort Version, uint Width, uint Height)`
  - `static class LinkMessages { const ushort ProtocolVersion = 1; const ushort Port = 7100; Hello(uint, uint); TryParseHello(LinkFrame, out HelloInfo); Ping(ulong); Pong(ulong); Bye(); TryReadTimestamp(LinkFrame, out ulong) }`

- [ ] **Step 1: Criar a solução**

```bash
cd windows
dotnet new sln -n TelaDois --format sln
dotnet new classlib -n TelaDois.Core -o src/TelaDois.Core -f net10.0
dotnet new xunit -n TelaDois.Core.Tests -o tests/TelaDois.Core.Tests -f net10.0
dotnet sln TelaDois.sln add src/TelaDois.Core/TelaDois.Core.csproj tests/TelaDois.Core.Tests/TelaDois.Core.Tests.csproj
dotnet add tests/TelaDois.Core.Tests/TelaDois.Core.Tests.csproj reference src/TelaDois.Core/TelaDois.Core.csproj
rm src/TelaDois.Core/Class1.cs tests/TelaDois.Core.Tests/UnitTest1.cs
```

`windows/global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

`windows/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Em `TelaDois.Core.csproj`, deixe os testes enxergarem tipos `internal`:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="TelaDois.Core.Tests" />
  </ItemGroup>
```

- [ ] **Step 2: Escrever os testes** — `tests/TelaDois.Core.Tests/LinkProtocolTests.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Tests;

public class LinkProtocolTests
{
    [Fact]
    public void HelloMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("010000000A0001000009FC0000049B"), LinkMessages.Hello(2556, 1179).Encode());

    [Fact]
    public void PingMatchesGoldenBytes() =>
        Assert.Equal(Convert.FromHexString("05000000080000011F71FB04CB"), LinkMessages.Ping(1234567890123).Encode());

    [Fact]
    public void ParsesHello()
    {
        Assert.True(LinkMessages.TryParseHello(LinkMessages.Hello(2622, 1206), out var hello));
        Assert.Equal(new HelloInfo(1, 2622, 1206), hello);
    }

    [Fact]
    public void RejectsHelloWithWrongSize() =>
        Assert.False(LinkMessages.TryParseHello(new LinkFrame(LinkMessageType.Hello, [0, 1]), out _));

    [Fact]
    public void PongCarriesTimestamp()
    {
        Assert.True(LinkMessages.TryReadTimestamp(LinkMessages.Pong(42), out var timestamp));
        Assert.Equal(42UL, timestamp);
        Assert.False(LinkMessages.TryReadTimestamp(LinkMessages.Bye(), out _));
    }

    [Fact]
    public void DecoderHandlesSplitAndBatchedFrames()
    {
        var bytes = LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Ping(7).Encode()).ToArray();
        var decoder = new LinkFrameDecoder();
        Assert.Empty(decoder.Append(bytes.AsSpan(0, 3)));
        var frames = decoder.Append(bytes.AsSpan(3));
        Assert.Equal(new LinkMessageType?[] { LinkMessageType.Hello, LinkMessageType.Ping }, frames.Select(f => f.MessageType));
        Assert.Empty(decoder.Append([]));
    }

    [Fact]
    public void DecoderRejectsHugePayload() =>
        Assert.Throws<InvalidDataException>(() => new LinkFrameDecoder().Append(Convert.FromHexString("03FFFFFFFF")));

    [Fact]
    public void UnknownTypeHasNoMessageType() => Assert.Null(new LinkFrame(99, []).MessageType);

    [Fact]
    public async Task ReaderReturnsFramesThenNullWhenClosed()
    {
        var bytes = LinkMessages.Ping(1).Encode().Concat(LinkMessages.Bye().Encode()).ToArray();
        var reader = new LinkFrameReader(new MemoryStream(bytes));
        Assert.Equal(LinkMessageType.Ping, (await reader.NextAsync(CancellationToken.None))?.MessageType);
        Assert.Equal(LinkMessageType.Bye, (await reader.NextAsync(CancellationToken.None))?.MessageType);
        Assert.Null(await reader.NextAsync(CancellationToken.None));
    }
}
```

- [ ] **Step 3: Rodar — DEVE falhar**

Run: `dotnet test windows/TelaDois.sln`
Expected: erro de compilação (`LinkMessages` não existe).

- [ ] **Step 4: Implementar** — `src/TelaDois.Core/Link/LinkFrame.cs`:

```csharp
using System.Buffers.Binary;

namespace TelaDois.Core.Link;

/// <summary>Tipos de mensagem do protocolo PC ↔ iPhone (spec §3.4).</summary>
public enum LinkMessageType : byte
{
    Hello = 1,
    Config = 2,
    Video = 3,
    KeyframeRequest = 4,
    Ping = 5,
    Pong = 6,
    Bye = 7,
}

/// <summary>Uma mensagem: <c>[tipo: 1 byte][tamanho: 4 bytes big-endian][dados]</c>.</summary>
public sealed record LinkFrame(byte Type, byte[] Payload)
{
    public const int HeaderSize = 5;
    public const int MaxPayload = 16 * 1024 * 1024;

    public LinkFrame(LinkMessageType type, byte[]? payload = null) : this((byte)type, payload ?? []) { }

    /// <summary><c>null</c> para tipos desconhecidos (versões futuras), que são ignorados.</summary>
    public LinkMessageType? MessageType =>
        Enum.IsDefined((LinkMessageType)Type) ? (LinkMessageType)Type : null;

    public byte[] Encode()
    {
        var bytes = new byte[HeaderSize + Payload.Length];
        bytes[0] = Type;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), (uint)Payload.Length);
        Payload.CopyTo(bytes, HeaderSize);
        return bytes;
    }
}
```

`src/TelaDois.Core/Link/LinkFrameDecoder.cs`:

```csharp
using System.Buffers.Binary;

namespace TelaDois.Core.Link;

/// <summary>Junta os bytes que chegam em pedaços e devolve as mensagens completas.</summary>
public sealed class LinkFrameDecoder
{
    private byte[] _buffer = new byte[64 * 1024];
    private int _count;

    public IReadOnlyList<LinkFrame> Append(ReadOnlySpan<byte> data)
    {
        if (_count + data.Length > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_count + data.Length, _buffer.Length * 2));
        }
        data.CopyTo(_buffer.AsSpan(_count));
        _count += data.Length;

        var frames = new List<LinkFrame>();
        var offset = 0;
        while (_count - offset >= LinkFrame.HeaderSize)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(offset + 1));
            if (length > LinkFrame.MaxPayload)
            {
                throw new InvalidDataException($"mensagem grande demais: {length} bytes");
            }
            var total = LinkFrame.HeaderSize + (int)length;
            if (_count - offset < total)
            {
                break;
            }
            frames.Add(new LinkFrame(_buffer[offset], _buffer.AsSpan(offset + LinkFrame.HeaderSize, (int)length).ToArray()));
            offset += total;
        }
        if (offset > 0)
        {
            Buffer.BlockCopy(_buffer, offset, _buffer, 0, _count - offset);
            _count -= offset;
        }
        return frames;
    }
}
```

`src/TelaDois.Core/Link/LinkFrameReader.cs`:

```csharp
namespace TelaDois.Core.Link;

/// <summary>Lê mensagens de um stream; <c>null</c> quando o outro lado fecha a conexão.</summary>
public sealed class LinkFrameReader(Stream stream)
{
    private readonly LinkFrameDecoder _decoder = new();
    private readonly Queue<LinkFrame> _pending = new();
    private readonly byte[] _buffer = new byte[64 * 1024];

    public async Task<LinkFrame?> NextAsync(CancellationToken ct)
    {
        while (_pending.Count == 0)
        {
            var read = await stream.ReadAsync(_buffer, ct);
            if (read == 0)
            {
                return null;
            }
            foreach (var frame in _decoder.Append(_buffer.AsSpan(0, read)))
            {
                _pending.Enqueue(frame);
            }
        }
        return _pending.Dequeue();
    }
}
```

`src/TelaDois.Core/Link/LinkMessages.cs`:

```csharp
using System.Buffers.Binary;

namespace TelaDois.Core.Link;

public readonly record struct HelloInfo(ushort Version, uint Width, uint Height);

public static class LinkMessages
{
    public const ushort ProtocolVersion = 1;
    public const ushort Port = 7100;

    public static LinkFrame Hello(uint width, uint height)
    {
        var payload = new byte[10];
        BinaryPrimitives.WriteUInt16BigEndian(payload, ProtocolVersion);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(2), width);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(6), height);
        return new LinkFrame(LinkMessageType.Hello, payload);
    }

    public static bool TryParseHello(LinkFrame frame, out HelloInfo hello)
    {
        hello = default;
        if (frame.MessageType != LinkMessageType.Hello || frame.Payload.Length != 10)
        {
            return false;
        }
        var payload = frame.Payload.AsSpan();
        hello = new HelloInfo(
            BinaryPrimitives.ReadUInt16BigEndian(payload),
            BinaryPrimitives.ReadUInt32BigEndian(payload[2..]),
            BinaryPrimitives.ReadUInt32BigEndian(payload[6..]));
        return true;
    }

    public static LinkFrame Ping(ulong timestamp) => Timestamped(LinkMessageType.Ping, timestamp);

    public static LinkFrame Pong(ulong timestamp) => Timestamped(LinkMessageType.Pong, timestamp);

    public static LinkFrame Bye() => new(LinkMessageType.Bye);

    /// <summary>Carimbo de tempo de um PING ou PONG.</summary>
    public static bool TryReadTimestamp(LinkFrame frame, out ulong timestamp)
    {
        timestamp = 0;
        if (frame.MessageType is not (LinkMessageType.Ping or LinkMessageType.Pong) || frame.Payload.Length != 8)
        {
            return false;
        }
        timestamp = BinaryPrimitives.ReadUInt64BigEndian(frame.Payload);
        return true;
    }

    private static LinkFrame Timestamped(LinkMessageType type, ulong timestamp)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(payload, timestamp);
        return new LinkFrame(type, payload);
    }
}
```

- [ ] **Step 5: Rodar — DEVE passar**

Run: `dotnet test windows/TelaDois.sln`
Expected: `Passed!  - Failed: 0, Passed: 9`, zero avisos.

- [ ] **Step 6: CI do Windows** — `.github/workflows/windows-test.yml`:

```yaml
name: windows-test
on:
  push:
    branches: [main, "etapa-*"]
    paths:
      - "windows/**"
      - ".github/workflows/windows-test.yml"
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  test:
    runs-on: windows-2025
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: windows/global.json
      - name: Testes do PC
        run: dotnet test windows/TelaDois.sln -c Release
```

- [ ] **Step 7: Commit e push**

```bash
git add windows .github/workflows/windows-test.yml
git commit -F <arquivo com: "feat(windows): solução .NET e protocolo do túnel do lado PC">
git push
```

Expected: `windows-test` verde.

---

### Task 4: Cliente do usbmuxd (plist, pacote, Listen e Connect)

**Files:**
- Create: `windows/src/TelaDois.Core/Usbmux/Plist.cs`, `UsbmuxPacket.cs`, `UsbmuxMessages.cs`, `UsbmuxClient.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeUsbmuxd.cs`, `Fakes/FakePhone.cs`
- Test: `windows/tests/TelaDois.Core.Tests/PlistTests.cs`, `UsbmuxTests.cs`

**Interfaces:**
- Consumes: `LinkMessages`, `LinkFrameReader` (Task 3).
- Produces (namespace `TelaDois.Core.Usbmux`):
  - `static class Plist { byte[] Serialize(IReadOnlyDictionary<string, object>); Dictionary<string, object> Parse(byte[]) }` — inteiros saem como `long`
  - `static class UsbmuxPacket { byte[] Build(uint tag, IReadOnlyDictionary<string, object>); Task<(uint Tag, Dictionary<string, object> Body)> ReadAsync(Stream, CancellationToken); ushort NetworkPort(ushort) }`
  - `abstract record UsbmuxEvent; record ListenStarted (primeiro evento, logo após o Listen ser aceito); record DeviceAttached(int DeviceId, string SerialNumber, string ConnectionType); record DeviceDetached(int DeviceId)`
  - `static class UsbmuxMessages { ResultOk = 0; ResultConnectionRefused = 3; Listen(); Connect(int, ushort); long? ResultCode(...); UsbmuxEvent? ParseEvent(...) }`
  - `sealed class UsbmuxException : IOException { long Code }`
  - `sealed class UsbmuxClient(IPEndPoint) { static DefaultEndpoint = 127.0.0.1:27015; IAsyncEnumerable<UsbmuxEvent> ListenAsync(ct); Task<Stream> ConnectAsync(int deviceId, ushort port, ct) }`
  - Testes: `FakeUsbmuxd` (porta livre no loopback; `ConnectResult`, `Phone`, `Requests`, `SendEventAsync`, `Attached(id)`, `Detached(id)`) e `FakePhone` (`RespondAsync`, `SilentAfterHelloAsync`).

- [ ] **Step 1: Usbmuxd e iPhone falsos** — `tests/TelaDois.Core.Tests/Fakes/FakePhone.cs`:

```csharp
using TelaDois.Core.Link;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Faz o papel do app do iPhone do outro lado do túnel.</summary>
internal static class FakePhone
{
    public static async Task RespondAsync(Stream stream, CancellationToken ct)
    {
        await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
        var reader = new LinkFrameReader(stream);
        while (await reader.NextAsync(ct) is { } frame)
        {
            if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
            {
                await stream.WriteAsync(LinkMessages.Pong(sent).Encode(), ct);
            }
            if (frame.MessageType == LinkMessageType.Bye)
            {
                return;
            }
        }
    }

    public static async Task SilentAfterHelloAsync(Stream stream, CancellationToken ct)
    {
        await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}
```

`tests/TelaDois.Core.Tests/Fakes/FakeUsbmuxd.cs`:

```csharp
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>usbmuxd falso no loopback: responde Listen (e manda eventos) e Connect (e vira o iPhone).</summary>
internal sealed class FakeUsbmuxd : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private volatile Stream? _listenStream;

    public FakeUsbmuxd()
    {
        _listener.Start();
        _acceptLoop = AcceptLoopAsync();
    }

    public IPEndPoint Endpoint => (IPEndPoint)_listener.LocalEndpoint;

    /// <summary>Resposta ao Connect: 0 = ok, 3 = app fechado.</summary>
    public volatile int ConnectResult;

    public Func<Stream, CancellationToken, Task> Phone { get; set; } = FakePhone.RespondAsync;

    public IReadOnlyList<Dictionary<string, object>> EventsAfterListen { get; set; } = [Attached(1)];

    public ConcurrentQueue<Dictionary<string, object>> Requests { get; } = new();

    public static Dictionary<string, object> Attached(int id) => new()
    {
        ["MessageType"] = "Attached",
        ["DeviceID"] = id,
        ["Properties"] = new Dictionary<string, object>
        {
            ["ConnectionType"] = "USB",
            ["DeviceID"] = id,
            ["SerialNumber"] = "00008120-TESTE",
        },
    };

    public static Dictionary<string, object> Detached(int id) => new()
    {
        ["MessageType"] = "Detached",
        ["DeviceID"] = id,
    };

    public async Task SendEventAsync(Dictionary<string, object> body)
    {
        var stream = _listenStream ?? throw new InvalidOperationException("ninguém está em Listen");
        await stream.WriteAsync(UsbmuxPacket.Build(0, body), _cts.Token);
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleAsync(client);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
        {
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            try
            {
                var (tag, request) = await UsbmuxPacket.ReadAsync(stream, _cts.Token);
                Requests.Enqueue(request);
                var type = (string)request["MessageType"];
                var code = type == "Connect" ? ConnectResult : 0;
                await stream.WriteAsync(UsbmuxPacket.Build(tag, Result(code)), _cts.Token);
                if (code != 0)
                {
                    return;
                }
                if (type == "Listen")
                {
                    _listenStream = stream;
                    foreach (var body in EventsAfterListen)
                    {
                        await stream.WriteAsync(UsbmuxPacket.Build(0, body), _cts.Token);
                    }
                    await Task.Delay(Timeout.Infinite, _cts.Token);
                }
                else if (type == "Connect")
                {
                    await Phone(stream, _cts.Token);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
            }
        }
    }

    private static Dictionary<string, object> Result(int code) => new()
    {
        ["MessageType"] = "Result",
        ["Number"] = code,
    };

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        await _acceptLoop;
        _cts.Dispose();
    }
}
```

- [ ] **Step 2: Testes** — `tests/TelaDois.Core.Tests/PlistTests.cs`:

```csharp
using System.Text;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests;

public class PlistTests
{
    [Fact]
    public void SerializesNestedDictionary()
    {
        var xml = Encoding.UTF8.GetString(Plist.Serialize(new Dictionary<string, object>
        {
            ["MessageType"] = "Connect",
            ["DeviceID"] = 3,
            ["Props"] = new Dictionary<string, object> { ["A"] = "b" },
        }));
        Assert.StartsWith("<?xml", xml);
        Assert.Contains("<key>MessageType</key><string>Connect</string>", xml);
        Assert.Contains("<key>DeviceID</key><integer>3</integer>", xml);
        Assert.Contains("<key>Props</key><dict><key>A</key><string>b</string></dict>", xml);
    }

    [Fact]
    public void ParsesApplePlistWithDoctype()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
            	<key>MessageType</key>
            	<string>Attached</string>
            	<key>DeviceID</key>
            	<integer>7</integer>
            	<key>Properties</key>
            	<dict>
            		<key>ConnectionType</key>
            		<string>USB</string>
            	</dict>
            </dict>
            </plist>
            """;
        var body = Plist.Parse(Encoding.UTF8.GetBytes(xml));
        Assert.Equal("Attached", body["MessageType"]);
        Assert.Equal(7L, body["DeviceID"]);
        var properties = Assert.IsType<Dictionary<string, object>>(body["Properties"]);
        Assert.Equal("USB", properties["ConnectionType"]);
    }

    [Fact]
    public void RoundTripsBooleansDataAndArrays()
    {
        var body = Plist.Parse(Plist.Serialize(new Dictionary<string, object>
        {
            ["Yes"] = true,
            ["Bytes"] = new byte[] { 1, 2, 3 },
            ["List"] = new List<object> { "a", 2 },
        }));
        Assert.True((bool)body["Yes"]);
        Assert.Equal(new byte[] { 1, 2, 3 }, body["Bytes"]);
        Assert.Equal(new List<object> { "a", 2L }, body["List"]);
    }
}
```

`tests/TelaDois.Core.Tests/UsbmuxTests.cs`:

```csharp
using System.Buffers.Binary;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests;

public class UsbmuxTests
{
    [Fact]
    public void PortIsSentWithBytesSwapped() => Assert.Equal(48155, UsbmuxPacket.NetworkPort(7100));

    [Fact]
    public void HeaderIsLittleEndianPlistPacket()
    {
        var packet = UsbmuxPacket.Build(5, UsbmuxMessages.Listen());
        Assert.Equal((uint)packet.Length, BinaryPrimitives.ReadUInt32LittleEndian(packet));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4)));
        Assert.Equal(8u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(8)));
        Assert.Equal(5u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(12)));
    }

    [Fact]
    public void ParsesAttachedAndDetachedEvents()
    {
        Assert.Equal(new DeviceAttached(1, "00008120-TESTE", "USB"), UsbmuxMessages.ParseEvent(RoundTrip(FakeUsbmuxd.Attached(1))));
        Assert.Equal(new DeviceDetached(1), UsbmuxMessages.ParseEvent(RoundTrip(FakeUsbmuxd.Detached(1))));
        Assert.Null(UsbmuxMessages.ParseEvent(RoundTrip(new Dictionary<string, object> { ["MessageType"] = "Paired", ["DeviceID"] = 1 })));
    }

    [Fact]
    public async Task ListenReceivesAttachedDevice()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var events = new UsbmuxClient(usbmuxd.Endpoint).ListenAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.True(await events.MoveNextAsync());
        Assert.Equal(new ListenStarted(), events.Current);
        Assert.True(await events.MoveNextAsync());
        Assert.Equal(new DeviceAttached(1, "00008120-TESTE", "USB"), events.Current);
    }

    [Fact]
    public async Task ConnectIsRefusedWhenAppIsClosed()
    {
        await using var usbmuxd = new FakeUsbmuxd { ConnectResult = UsbmuxMessages.ResultConnectionRefused };
        var error = await Assert.ThrowsAsync<UsbmuxException>(() => new UsbmuxClient(usbmuxd.Endpoint).ConnectAsync(1, LinkMessages.Port));
        Assert.Equal(3L, error.Code);
        var request = Assert.Single(usbmuxd.Requests, r => (string)r["MessageType"] == "Connect");
        Assert.Equal(48155L, request["PortNumber"]);
        Assert.Equal(1L, request["DeviceID"]);
    }

    [Fact]
    public async Task ConnectReturnsTunnelToThePhone()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var tunnel = await new UsbmuxClient(usbmuxd.Endpoint).ConnectAsync(1, LinkMessages.Port, cts.Token);
        var first = await new LinkFrameReader(tunnel).NextAsync(cts.Token);
        Assert.True(LinkMessages.TryParseHello(first!, out var hello));
        Assert.Equal(new HelloInfo(1, 2556, 1179), hello);
    }

    private static Dictionary<string, object> RoundTrip(Dictionary<string, object> body) => Plist.Parse(Plist.Serialize(body));
}
```

- [ ] **Step 3: Rodar — DEVE falhar** (`dotnet test windows/TelaDois.sln`: `Plist` não existe)

- [ ] **Step 4: Implementar** — `src/TelaDois.Core/Usbmux/Plist.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TelaDois.Core.Usbmux;

/// <summary>Plist XML mínimo (dict, string, integer, true/false, data, array) para falar com o usbmuxd.</summary>
public static class Plist
{
    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n";

    public static byte[] Serialize(IReadOnlyDictionary<string, object> dict)
    {
        var root = new XElement("plist", new XAttribute("version", "1.0"), ToElement(dict));
        return Encoding.UTF8.GetBytes(Header + root.ToString(SaveOptions.DisableFormatting) + "\n");
    }

    /// <summary>Lê o dict da raiz. Inteiros viram <see cref="long"/>.</summary>
    public static Dictionary<string, object> Parse(byte[] xml)
    {
        // As respostas da Apple trazem DOCTYPE: ignora a DTD em vez de recusar o documento.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(new MemoryStream(xml), settings);
        var root = XDocument.Load(reader).Root?.Elements().FirstOrDefault()
            ?? throw new InvalidDataException("plist vazio");
        return FromElement(root) as Dictionary<string, object>
            ?? throw new InvalidDataException("plist sem dict na raiz");
    }

    private static XElement ToElement(object value) => value switch
    {
        string text => new XElement("string", text),
        bool flag => new XElement(flag ? "true" : "false"),
        int or long or uint or ushort => new XElement("integer", Convert.ToInt64(value, CultureInfo.InvariantCulture)),
        byte[] bytes => new XElement("data", Convert.ToBase64String(bytes)),
        IReadOnlyDictionary<string, object> dict => new XElement("dict",
            dict.SelectMany(pair => new[] { new XElement("key", pair.Key), ToElement(pair.Value) })),
        IEnumerable<object> list => new XElement("array", list.Select(ToElement)),
        _ => throw new ArgumentException($"tipo não suportado no plist: {value.GetType()}"),
    };

    private static object FromElement(XElement element) => element.Name.LocalName switch
    {
        "string" => element.Value,
        "integer" => long.Parse(element.Value, CultureInfo.InvariantCulture),
        "true" => true,
        "false" => false,
        "data" => Convert.FromBase64String(string.Concat(element.Value.Where(c => !char.IsWhiteSpace(c)))),
        "dict" => ParseDict(element),
        "array" => element.Elements().Select(FromElement).ToList(),
        _ => throw new InvalidDataException($"elemento de plist não suportado: {element.Name}"),
    };

    private static Dictionary<string, object> ParseDict(XElement element)
    {
        var result = new Dictionary<string, object>();
        var children = element.Elements().ToList();
        for (var i = 0; i + 1 < children.Count; i += 2)
        {
            result[children[i].Value] = FromElement(children[i + 1]);
        }
        return result;
    }
}
```

`src/TelaDois.Core/Usbmux/UsbmuxPacket.cs`:

```csharp
using System.Buffers.Binary;

namespace TelaDois.Core.Usbmux;

/// <summary>Pacote do usbmuxd: cabeçalho de 16 bytes little-endian (tamanho, versão 1, tipo 8 = plist, tag) + plist.</summary>
public static class UsbmuxPacket
{
    public const int HeaderSize = 16;
    public const uint ProtocolVersion = 1;
    public const uint PlistMessageType = 8;
    private const int MaxSize = 1024 * 1024;

    public static byte[] Build(uint tag, IReadOnlyDictionary<string, object> body)
    {
        var plist = Plist.Serialize(body);
        var packet = new byte[HeaderSize + plist.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(packet, (uint)packet.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(4), ProtocolVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8), PlistMessageType);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), tag);
        plist.CopyTo(packet, HeaderSize);
        return packet;
    }

    public static async Task<(uint Tag, Dictionary<string, object> Body)> ReadAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[HeaderSize];
        await stream.ReadExactlyAsync(header, ct);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length < HeaderSize || length > MaxSize)
        {
            throw new InvalidDataException($"pacote do usbmuxd com tamanho inválido: {length}");
        }
        var body = new byte[length - HeaderSize];
        await stream.ReadExactlyAsync(body, ct);
        return (BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12)), Plist.Parse(body));
    }

    /// <summary>O usbmuxd espera a porta com os bytes trocados (ordem de rede lida como little-endian).</summary>
    public static ushort NetworkPort(ushort port) => BinaryPrimitives.ReverseEndianness(port);
}
```

`src/TelaDois.Core/Usbmux/UsbmuxMessages.cs`:

```csharp
namespace TelaDois.Core.Usbmux;

public abstract record UsbmuxEvent;

/// <summary>O usbmuxd aceitou o Listen (o serviço da Apple está de pé).</summary>
public sealed record ListenStarted : UsbmuxEvent;

public sealed record DeviceAttached(int DeviceId, string SerialNumber, string ConnectionType) : UsbmuxEvent;

public sealed record DeviceDetached(int DeviceId) : UsbmuxEvent;

public static class UsbmuxMessages
{
    public const int ResultOk = 0;
    public const int ResultConnectionRefused = 3;

    public static Dictionary<string, object> Listen() => Request("Listen");

    public static Dictionary<string, object> Connect(int deviceId, ushort port)
    {
        var request = Request("Connect");
        request["DeviceID"] = deviceId;
        request["PortNumber"] = (int)UsbmuxPacket.NetworkPort(port);
        return request;
    }

    /// <summary>Código de uma resposta <c>Result</c>; <c>null</c> se não for uma.</summary>
    public static long? ResultCode(IReadOnlyDictionary<string, object> body) =>
        body.TryGetValue("MessageType", out var type) && type is "Result"
        && body.TryGetValue("Number", out var number) && number is long code
            ? code
            : null;

    public static UsbmuxEvent? ParseEvent(IReadOnlyDictionary<string, object> body)
    {
        if (!body.TryGetValue("MessageType", out var type) || !body.TryGetValue("DeviceID", out var idValue) || idValue is not long id)
        {
            return null;
        }
        return type switch
        {
            "Attached" => new DeviceAttached((int)id, Property(body, "SerialNumber"), Property(body, "ConnectionType")),
            "Detached" => new DeviceDetached((int)id),
            _ => null,
        };
    }

    private static string Property(IReadOnlyDictionary<string, object> body, string key) =>
        body.TryGetValue("Properties", out var value) && value is IReadOnlyDictionary<string, object> properties
        && properties.TryGetValue(key, out var property) && property is string text
            ? text
            : "";

    private static Dictionary<string, object> Request(string type) => new()
    {
        ["MessageType"] = type,
        ["ClientVersionString"] = "TelaDois",
        ["ProgName"] = "TelaDois",
        ["kLibUSBMuxVersion"] = 3,
    };
}
```

`src/TelaDois.Core/Usbmux/UsbmuxClient.cs`:

```csharp
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace TelaDois.Core.Usbmux;

public sealed class UsbmuxException(string operation, long code)
    : IOException($"o usbmuxd recusou {operation} (código {code})")
{
    public long Code { get; } = code;
}

/// <summary>Cliente do usbmuxd do Apple Mobile Device Service (cada operação usa uma conexão nova).</summary>
public sealed class UsbmuxClient(IPEndPoint endpoint)
{
    public static IPEndPoint DefaultEndpoint { get; } = new(IPAddress.Loopback, 27015);

    /// <summary>Eventos de iPhone conectado/desconectado até o token ser cancelado ou a conexão cair.</summary>
    public async IAsyncEnumerable<UsbmuxEvent> ListenAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(endpoint, ct);
        var stream = tcp.GetStream();
        await RequestAsync(stream, 1, UsbmuxMessages.Listen(), "Listen", ct);
        yield return new ListenStarted();
        while (true)
        {
            var (_, body) = await UsbmuxPacket.ReadAsync(stream, ct);
            if (UsbmuxMessages.ParseEvent(body) is { } usbmuxEvent)
            {
                yield return usbmuxEvent;
            }
        }
    }

    /// <summary>Abre um túnel TCP até <paramref name="port"/> no iPhone; o stream devolvido é o túnel.</summary>
    public async Task<Stream> ConnectAsync(int deviceId, ushort port, CancellationToken ct = default)
    {
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            await tcp.ConnectAsync(endpoint, ct);
            var stream = tcp.GetStream();
            await RequestAsync(stream, 2, UsbmuxMessages.Connect(deviceId, port), "Connect", ct);
            return stream;
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    private static async Task RequestAsync(Stream stream, uint tag, Dictionary<string, object> body, string operation, CancellationToken ct)
    {
        await stream.WriteAsync(UsbmuxPacket.Build(tag, body), ct);
        var (_, reply) = await UsbmuxPacket.ReadAsync(stream, ct);
        var code = UsbmuxMessages.ResultCode(reply) ?? -1;
        if (code != UsbmuxMessages.ResultOk)
        {
            throw new UsbmuxException(operation, code);
        }
    }
}
```

- [ ] **Step 5: Rodar — DEVE passar** (`dotnet test windows/TelaDois.sln`: 9 + 9 testes)

- [ ] **Step 6: Commit e push**

```bash
git add windows
git commit -F <arquivo com: "feat(windows): cliente do usbmuxd (plist, Listen, Connect) com usbmuxd falso nos testes">
git push
```

---

### Task 5: Sessão do lado PC (`LinkSession`)

**Files:**
- Create: `windows/src/TelaDois.Core/Link/LinkSession.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/Loopback.cs`
- Test: `windows/tests/TelaDois.Core.Tests/LinkSessionTests.cs`

**Interfaces:**
- Consumes: `LinkFrameReader`, `LinkMessages` (Task 3); `FakePhone` (Task 4).
- Produces: `sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout) { static Default = (1 s, 3 s) }`; `sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip)`; `enum LinkEndReason { Bye, Timeout, Closed, Cancelled }`; `static class LinkSession { Task<LinkEndReason> RunAsync(Stream, Action<LinkStatus>, LinkSessionOptions, CancellationToken) }` (lança `InvalidDataException` se a 1ª mensagem não for HELLO).

- [ ] **Step 1: Par de sockets para testes** — `tests/TelaDois.Core.Tests/Fakes/Loopback.cs`:

```csharp
using System.Net;
using System.Net.Sockets;

namespace TelaDois.Core.Tests.Fakes;

internal static class Loopback
{
    /// <summary>Dois streams TCP ligados entre si (PC e iPhone); fechar um fecha o socket.</summary>
    public static async Task<(Stream Pc, Stream Phone)> CreatePairAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var pc = new TcpClient { NoDelay = true };
            var connecting = pc.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
            var phone = await listener.AcceptTcpClientAsync();
            await connecting;
            return (pc.GetStream(), phone.GetStream());
        }
        finally
        {
            listener.Stop();
        }
    }
}
```

- [ ] **Step 2: Testes** — `tests/TelaDois.Core.Tests/LinkSessionTests.cs`:

```csharp
using System.Collections.Concurrent;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;

namespace TelaDois.Core.Tests;

public class LinkSessionTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    [Fact]
    public async Task ReportsHelloThenRoundTrip()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var phoneTask = FakePhone.RespondAsync(phone, cts.Token);
        var statuses = new ConcurrentQueue<LinkStatus>();
        var measured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var run = LinkSession.RunAsync(pc, status =>
        {
            statuses.Enqueue(status);
            if (status.RoundTrip is not null)
            {
                measured.TrySetResult();
            }
        }, Fast, cts.Token);

        await measured.Task.WaitAsync(cts.Token);
        Assert.True(statuses.TryPeek(out var first));
        Assert.Equal(new LinkStatus(new HelloInfo(1, 2556, 1179), null), first);
        cts.Cancel();
        Assert.Equal(LinkEndReason.Cancelled, await run);
    }

    [Fact]
    public async Task EndsWithByeFromPhone()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode().Concat(LinkMessages.Bye().Encode()).ToArray());
        Assert.Equal(LinkEndReason.Bye, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task EndsWhenPhoneClosesTheSocket()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await phone.WriteAsync(LinkMessages.Hello(2556, 1179).Encode());
        await phone.DisposeAsync();
        Assert.Equal(LinkEndReason.Closed, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task TimesOutWhenPhoneGoesSilent()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = FakePhone.SilentAfterHelloAsync(phone, cts.Token);
        Assert.Equal(LinkEndReason.Timeout, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(cts.Token));
    }

    [Fact]
    public async Task TimesOutWithoutHello()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        Assert.Equal(LinkEndReason.Timeout, await LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task RejectsFirstMessageThatIsNotHello()
    {
        var (pc, phone) = await Loopback.CreatePairAsync();
        await using var _pc = pc;
        await using var _phone = phone;
        await phone.WriteAsync(LinkMessages.Ping(1).Encode());
        await Assert.ThrowsAsync<InvalidDataException>(() => LinkSession.RunAsync(pc, _ => { }, Fast, CancellationToken.None));
    }
}
```

- [ ] **Step 3: Rodar — DEVE falhar** (`LinkSession` não existe)

- [ ] **Step 4: Implementar** — `src/TelaDois.Core/Link/LinkSession.cs`:

```csharp
using System.Diagnostics;

namespace TelaDois.Core.Link;

public sealed record LinkSessionOptions(TimeSpan PingInterval, TimeSpan Timeout)
{
    public static LinkSessionOptions Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
}

/// <summary>Estado da conexão: resolução do iPhone e a última latência de ida e volta medida.</summary>
public sealed record LinkStatus(HelloInfo Hello, TimeSpan? RoundTrip);

public enum LinkEndReason
{
    Bye,
    Timeout,
    Closed,
    Cancelled,
}

/// <summary>
/// Lado do PC: espera o HELLO, manda PING a cada intervalo, mede o PONG e encerra no BYE,
/// quando o iPhone fecha o socket ou após o tempo-limite sem receber nada. Ao sair, manda BYE.
/// </summary>
public static class LinkSession
{
    public static async Task<LinkEndReason> RunAsync(Stream stream, Action<LinkStatus> onStatus, LinkSessionOptions options, CancellationToken ct)
    {
        var reader = new LinkFrameReader(stream);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clock = Stopwatch.StartNew();
        long lastHeard = 0;
        Task<LinkEndReason>? receiving = null;

        async Task SendAsync(LinkFrame frame, CancellationToken token)
        {
            await writeLock.WaitAsync(token);
            try
            {
                await stream.WriteAsync(frame.Encode(), token);
            }
            finally
            {
                writeLock.Release();
            }
        }

        try
        {
            var hello = await ReadHelloAsync(reader, options.Timeout, cts.Token);
            if (hello is null)
            {
                return LinkEndReason.Closed;
            }
            var status = new LinkStatus(hello.Value, null);
            onStatus(status);
            Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);

            receiving = Task.Run(async () =>
            {
                while (await reader.NextAsync(cts.Token) is { } frame)
                {
                    Interlocked.Exchange(ref lastHeard, clock.ElapsedMilliseconds);
                    switch (frame.MessageType)
                    {
                        case LinkMessageType.Pong when LinkMessages.TryReadTimestamp(frame, out var sent):
                            onStatus(status with { RoundTrip = TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds - (long)sent) });
                            break;
                        case LinkMessageType.Ping when LinkMessages.TryReadTimestamp(frame, out var theirs):
                            await SendAsync(LinkMessages.Pong(theirs), cts.Token);
                            break;
                        case LinkMessageType.Bye:
                            return LinkEndReason.Bye;
                    }
                }
                return LinkEndReason.Closed;
            }, CancellationToken.None);

            while (true)
            {
                var tick = Task.Delay(options.PingInterval, cts.Token);
                if (await Task.WhenAny(receiving, tick) == receiving)
                {
                    return await receiving;
                }
                ct.ThrowIfCancellationRequested();
                if (clock.ElapsedMilliseconds - Interlocked.Read(ref lastHeard) > options.Timeout.TotalMilliseconds)
                {
                    return LinkEndReason.Timeout;
                }
                await SendAsync(LinkMessages.Ping((ulong)clock.ElapsedMilliseconds), cts.Token);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return LinkEndReason.Cancelled;
        }
        catch (TimeoutException)
        {
            return LinkEndReason.Timeout;
        }
        catch (IOException)
        {
            return LinkEndReason.Closed;
        }
        finally
        {
            cts.Cancel();
            if (receiving is not null)
            {
                try
                {
                    await receiving;
                }
                catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
                {
                }
            }
            using var byeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            try
            {
                await SendAsync(LinkMessages.Bye(), byeTimeout.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
            }
        }
    }

    private static async Task<HelloInfo?> ReadHelloAsync(LinkFrameReader reader, TimeSpan timeout, CancellationToken ct)
    {
        using var helloCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        helloCts.CancelAfter(timeout);
        LinkFrame? first;
        try
        {
            first = await reader.NextAsync(helloCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("o iPhone não mandou HELLO a tempo");
        }
        if (first is null)
        {
            return null;
        }
        return LinkMessages.TryParseHello(first, out var hello)
            ? hello
            : throw new InvalidDataException("a primeira mensagem do iPhone não é HELLO");
    }
}
```

- [ ] **Step 5: Rodar — DEVE passar** (`dotnet test windows/TelaDois.sln`; rode 3 vezes para pegar instabilidade de tempo)

- [ ] **Step 6: Commit e push**

```bash
git add windows
git commit -F <arquivo com: "feat(windows): sessão do PC (HELLO, PING a cada 1 s, latência, BYE e tempo-limite de 3 s)">
git push
```

---

### Task 6: Serviço do túnel (`UsbTunnelService`) e textos da bandeja

**Files:**
- Create: `windows/src/TelaDois.Core/UsbTunnelService.cs`
- Create: `windows/src/TelaDois.Core/TunnelStatusText.cs`
- Test: `windows/tests/TelaDois.Core.Tests/UsbTunnelServiceTests.cs`

**Interfaces:**
- Consumes: `UsbmuxClient`, `UsbmuxException`, `DeviceAttached`/`DeviceDetached` (Task 4); `LinkSession`, `LinkSessionOptions` (Task 5).
- Produces (namespace `TelaDois.Core`): `enum TunnelState { ServiceMissing, WaitingForIPhone, WaitingForApp, Connected }`; `sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null)`; `sealed class UsbTunnelService(UsbmuxClient, LinkSessionOptions, Action<string> log) { event Action<TunnelStatus>? StatusChanged; TimeSpan Retry { get; init; } = 2 s; Task RunAsync(CancellationToken) }`; `static class TunnelStatusText { string For(TunnelStatus) }` (≤ 63 caracteres, limite do `NotifyIcon.Text`).

- [ ] **Step 1: Testes** — `tests/TelaDois.Core.Tests/UsbTunnelServiceTests.cs`:

```csharp
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests;

public class UsbTunnelServiceTests
{
    private static readonly LinkSessionOptions Fast = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(400));

    private static UsbTunnelService Create(IPEndPoint endpoint, ConcurrentQueue<string> log) =>
        new(new UsbmuxClient(endpoint), Fast, log.Enqueue) { Retry = TimeSpan.FromMilliseconds(100) };

    private static Task<TunnelStatus> WaitFor(UsbTunnelService service, Func<TunnelStatus, bool> match)
    {
        var found = new TaskCompletionSource<TunnelStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.StatusChanged += status =>
        {
            if (match(status))
            {
                found.TrySetResult(status);
            }
        };
        return found.Task;
    }

    [Fact]
    public async Task ConnectsAndMeasuresLatency()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var service = Create(usbmuxd.Endpoint, new());
        var connected = WaitFor(service, s => s is { State: TunnelState.Connected, RoundTrip: not null });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        var status = await connected.WaitAsync(cts.Token);
        Assert.Equal(new HelloInfo(1, 2556, 1179), status.Hello);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task WaitsForAppWhileConnectIsRefused()
    {
        await using var usbmuxd = new FakeUsbmuxd { ConnectResult = UsbmuxMessages.ResultConnectionRefused };
        var service = Create(usbmuxd.Endpoint, new());
        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForApp);
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await waiting.WaitAsync(cts.Token);
        usbmuxd.ConnectResult = UsbmuxMessages.ResultOk;
        await connected.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task GoesBackToWaitingWhenCableIsRemoved()
    {
        await using var usbmuxd = new FakeUsbmuxd();
        var service = Create(usbmuxd.Endpoint, new());
        var connected = WaitFor(service, s => s.State == TunnelState.Connected);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);
        await connected.WaitAsync(cts.Token);

        var waiting = WaitFor(service, s => s.State == TunnelState.WaitingForIPhone);
        await usbmuxd.SendEventAsync(FakeUsbmuxd.Detached(1));
        await waiting.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task ReportsMissingAppleService()
    {
        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var endpoint = (IPEndPoint)closed.LocalEndpoint;
        closed.Stop();
        var log = new ConcurrentQueue<string>();
        var service = Create(endpoint, log);
        var missing = WaitFor(service, s => s.State == TunnelState.ServiceMissing);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var run = service.RunAsync(cts.Token);

        await missing.WaitAsync(cts.Token);
        cts.Cancel();
        await run;
        Assert.Single(log, line => line.StartsWith("usbmuxd indisponível", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(TunnelState.ServiceMissing, "TelaDois: falta o Apple Mobile Device Service")]
    [InlineData(TunnelState.WaitingForIPhone, "TelaDois: conecte o iPhone no cabo")]
    [InlineData(TunnelState.WaitingForApp, "TelaDois: abra o app no iPhone")]
    public void DescribesWaitingStates(TunnelState state, string text) =>
        Assert.Equal(text, TunnelStatusText.For(new TunnelStatus(state)));

    [Fact]
    public void DescribesConnectedState()
    {
        var hello = new HelloInfo(1, 2556, 1179);
        Assert.Equal("TelaDois: conectado · 2556×1179 · 12 ms",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello, TimeSpan.FromMilliseconds(12.4))));
        Assert.Equal("TelaDois: conectado · 2556×1179",
            TunnelStatusText.For(new TunnelStatus(TunnelState.Connected, hello)));
    }
}
```

- [ ] **Step 2: Rodar — DEVE falhar** (`UsbTunnelService` não existe)

- [ ] **Step 3: Implementar** — `src/TelaDois.Core/UsbTunnelService.cs`:

```csharp
using System.Net.Sockets;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core;

public enum TunnelState
{
    ServiceMissing,
    WaitingForIPhone,
    WaitingForApp,
    Connected,
}

public sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null);

/// <summary>
/// Liga tudo: escuta o usbmuxd; com um iPhone no cabo, tenta abrir o túnel até a porta 7100 a cada
/// <see cref="Retry"/> (recusado = app fechado) e conduz a sessão; ao desplugar, volta a esperar.
/// </summary>
public sealed class UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log)
{
    private string? _lastError;

    public event Action<TunnelStatus>? StatusChanged;

    public TimeSpan Retry { get; init; } = TimeSpan.FromSeconds(2);

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await WatchDevicesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException e)
            {
                LogOnce($"usbmuxd indisponível: {e.Message}");
                Report(new TunnelStatus(TunnelState.ServiceMissing));
            }
            catch (Exception e) when (e is IOException or InvalidDataException)
            {
                LogOnce($"conexão com o usbmuxd caiu: {e.Message}");
            }
            try
            {
                await Task.Delay(Retry, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task WatchDevicesAsync(CancellationToken ct)
    {
        (int Id, CancellationTokenSource Cts, Task Task)? active = null;
        try
        {
            await foreach (var usbmuxEvent in usbmux.ListenAsync(ct))
            {
                switch (usbmuxEvent)
                {
                    case ListenStarted:
                        if (_lastError is not null)
                        {
                            log("usbmuxd disponível");
                            _lastError = null;
                        }
                        Report(new TunnelStatus(TunnelState.WaitingForIPhone));
                        break;
                    case DeviceAttached attached when attached.ConnectionType == "USB" && active is null:
                        log($"iPhone no cabo (DeviceID {attached.DeviceId}, {attached.SerialNumber})");
                        var deviceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        active = (attached.DeviceId, deviceCts, RunDeviceAsync(attached.DeviceId, deviceCts.Token));
                        break;
                    case DeviceDetached detached when active?.Id == detached.DeviceId:
                        log($"iPhone saiu do cabo (DeviceID {detached.DeviceId})");
                        await StopAsync(active.Value);
                        active = null;
                        Report(new TunnelStatus(TunnelState.WaitingForIPhone));
                        break;
                }
            }
        }
        finally
        {
            if (active is not null)
            {
                await StopAsync(active.Value);
            }
        }
    }

    private async Task RunDeviceAsync(int deviceId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Report(new TunnelStatus(TunnelState.WaitingForApp));
            try
            {
                await using var tunnel = await usbmux.ConnectAsync(deviceId, LinkMessages.Port, ct);
                log("túnel aberto até o app do iPhone");
                var reason = await LinkSession.RunAsync(
                    tunnel,
                    status => Report(new TunnelStatus(TunnelState.Connected, status.Hello, status.RoundTrip)),
                    options,
                    ct);
                log($"sessão encerrada: {reason}");
            }
            catch (UsbmuxException e) when (e.Code == UsbmuxMessages.ResultConnectionRefused)
            {
                // App fechado ou em segundo plano: tenta de novo.
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e) when (e is IOException or InvalidDataException)
            {
                log($"falha no túnel: {e.Message}");
            }
            try
            {
                await Task.Delay(Retry, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task StopAsync((int Id, CancellationTokenSource Cts, Task Task) device)
    {
        device.Cts.Cancel();
        try
        {
            await device.Task;
        }
        catch (OperationCanceledException)
        {
        }
        device.Cts.Dispose();
    }

    private void LogOnce(string message)
    {
        if (message != _lastError)
        {
            log(message);
            _lastError = message;
        }
    }

    private void Report(TunnelStatus status) => StatusChanged?.Invoke(status);
}
```

`src/TelaDois.Core/TunnelStatusText.cs`:

```csharp
namespace TelaDois.Core;

/// <summary>Texto curto da bandeja (o Windows corta o tooltip do ícone em 63 caracteres).</summary>
public static class TunnelStatusText
{
    public static string For(TunnelStatus status) => status.State switch
    {
        TunnelState.ServiceMissing => "TelaDois: falta o Apple Mobile Device Service",
        TunnelState.WaitingForIPhone => "TelaDois: conecte o iPhone no cabo",
        TunnelState.WaitingForApp => "TelaDois: abra o app no iPhone",
        _ when status is { Hello: { } hello, RoundTrip: { } rtt } =>
            $"TelaDois: conectado · {hello.Width}×{hello.Height} · {Math.Round(rtt.TotalMilliseconds):0} ms",
        _ when status.Hello is { } hello => $"TelaDois: conectado · {hello.Width}×{hello.Height}",
        _ => "TelaDois: conectado",
    };
}
```

- [ ] **Step 4: Rodar — DEVE passar** (`dotnet test windows/TelaDois.sln`, 3 vezes)

- [ ] **Step 5: Commit e push**

```bash
git add windows
git commit -F <arquivo com: "feat(windows): serviço do túnel (espera iPhone, reconecta e acompanha a sessão)">
git push
```

---

### Task 7: App de bandeja (`TelaDois.exe`)

**Files:**
- Create: `windows/src/TelaDois.Tray/TelaDois.Tray.csproj` (via `dotnet new winforms`)
- Create: `windows/src/TelaDois.Tray/Program.cs`, `TrayContext.cs`, `FileLog.cs`

**Interfaces:**
- Consumes: `UsbTunnelService`, `TunnelStatus`, `TunnelStatusText`, `UsbmuxClient.DefaultEndpoint`, `LinkSessionOptions.Default`.
- Produces: `TelaDois.exe` (ícone na bandeja com o status no tooltip; balão ao conectar e quando falta o serviço da Apple; menu "Abrir registro" e "Sair"); registro em `%LOCALAPPDATA%\TelaDois\tela-dois.log`; uma instância só.

- [ ] **Step 1: Criar o projeto**

```bash
cd windows
dotnet new winforms -n TelaDois.Tray -o src/TelaDois.Tray -f net10.0
dotnet sln TelaDois.sln add src/TelaDois.Tray/TelaDois.Tray.csproj
dotnet add src/TelaDois.Tray/TelaDois.Tray.csproj reference src/TelaDois.Core/TelaDois.Core.csproj
rm src/TelaDois.Tray/Form1.cs src/TelaDois.Tray/Form1.Designer.cs
```

Em `TelaDois.Tray.csproj`, dentro do `PropertyGroup`: `<AssemblyName>TelaDois</AssemblyName>`.

- [ ] **Step 2: `FileLog.cs`**

```csharp
using System.Diagnostics;

namespace TelaDois.Tray;

/// <summary>Registro em texto para diagnosticar a conexão sem depurador.</summary>
internal sealed class FileLog(string path)
{
    private readonly Lock _gate = new();

    public static FileLog Default()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelaDois");
        Directory.CreateDirectory(folder);
        return new FileLog(Path.Combine(folder, "tela-dois.log"));
    }

    public void Write(string message)
    {
        lock (_gate)
        {
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }

    public void Open() => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
```

- [ ] **Step 3: `TrayContext.cs`**

```csharp
using TelaDois.Core;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja que mostra o estado do túnel com o iPhone.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private TunnelState? _lastState;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir registro", null, (_, _) => _log.Open());
        menu.Items.Add("Sair", null, (_, _) => ExitThread());
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "TelaDois: iniciando",
            ContextMenuStrip = menu,
            Visible = true,
        };

        var ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write);
        service.StatusChanged += status => ui.Post(_ => Show(status), null);
        _log.Write("TelaDois iniciado");
        _ = Task.Run(() => service.RunAsync(_cts.Token));
    }

    private void Show(TunnelStatus status)
    {
        var text = TunnelStatusText.For(status);
        _icon.Text = text;
        if (status.State == _lastState)
        {
            return;
        }
        _lastState = status.State;
        _log.Write(text);
        if (status.State == TunnelState.Connected)
        {
            _icon.ShowBalloonTip(3000, "TelaDois", "iPhone conectado pelo cabo", ToolTipIcon.Info);
        }
        else if (status.State == TunnelState.ServiceMissing)
        {
            _icon.ShowBalloonTip(10000, "TelaDois",
                "Instale o iTunes baixado do site da Apple (não o da Microsoft Store) para o PC enxergar o iPhone.",
                ToolTipIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        _cts.Cancel();
        _icon.Visible = false;
        _icon.Dispose();
        _log.Write("TelaDois encerrado");
        base.ExitThreadCore();
    }
}
```

- [ ] **Step 4: `Program.cs`** (substitui o do modelo)

```csharp
namespace TelaDois.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var single = new Mutex(true, @"Local\TelaDois.Tray", out var isFirst);
        if (!isFirst)
        {
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
```

- [ ] **Step 5: Compilar, testar e publicar**

Run: `dotnet test windows/TelaDois.sln` (todos verdes, zero avisos) e
`dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows`
Expected: `builds/windows/TelaDois.exe` (pasta ignorada pelo git).

- [ ] **Step 6: Fumaça sem iPhone** — rode `builds/windows/TelaDois.exe`, espere 3 s e leia o registro.
Expected (iPhone fora do cabo): `TelaDois iniciado` e `TelaDois: conecte o iPhone no cabo`. Feche pelo menu "Sair" (ou `Stop-Process -Name TelaDois`).

- [ ] **Step 7: Commit e push**

```bash
git add windows
git commit -F <arquivo com: "feat(windows): app de bandeja TelaDois com status do túnel e registro">
git push
```

---

### Task 8: Ponta a ponta com o iPhone, documentação e `main`

**Files:**
- Modify: `CLAUDE.md` (Estado atual, Como rodar localmente, Regras)
- Modify: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8, linha da Etapa 4)

**Interfaces:**
- Consumes: tudo acima com CI verde (`ios-test`, `ios-build`, `windows-test`) no SHA final do branch.

- [ ] **Step 1: `.ipa` do branch** — baixe `TelaDois-ipa` do `ios-build` verde do SHA final para `builds/<sha curto>/`, inspecione com o `inspect_ipa.py` (Python isolado, pasta nova do scratchpad). Expected: `CFBundleShortVersionString` = `0.3.0`, arm64, sem `LC_CODE_SIGNATURE`.

- [ ] **Step 2: Usuário instala** pelo Sideloadly (mesmo Apple ID) e abre o app com o iPhone no cabo e desbloqueado.

- [ ] **Step 3: Rodar `builds/windows/TelaDois.exe`** e ler `%LOCALAPPDATA%\TelaDois\tela-dois.log`.
Expected, em até 5 s: `iPhone no cabo (DeviceID …)`, `túnel aberto até o app do iPhone`, `TelaDois: conectado · <largura>×<altura> · <n> ms`; no iPhone, "PC conectado". Teste também: mandar o app para o fundo (registro: `sessão encerrada: Closed`, depois espera o app) e voltar (reconecta); tirar o cabo (`iPhone saiu do cabo`). Anotar a latência medida.

- [ ] **Step 4: Documentação** — em `CLAUDE.md`:
  - Estado atual: Etapa 4 pronta (data, latência medida no aparelho), próximos passos = Etapa 5.
  - Como rodar localmente: `dotnet test windows/TelaDois.sln`; publicar com `dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows`; registro em `%LOCALAPPDATA%\TelaDois\tela-dois.log`.
  - Regras: o protocolo existe em Swift e C# — mudou um lado, atualize os bytes de referência nos dois testes; juntar ao `main` exige também `windows-test` verde.
  - Na spec §8, linha da Etapa 4: " — concluída em <data>; testada no iPhone pelo cabo (<n> ms)".

- [ ] **Step 5: Commit, push, CI verde e `main`**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md
git commit -F <arquivo com: "docs: Etapa 4 concluída (túnel USB)">
git push
git switch main && git merge --ff-only etapa-4-tunel-usb && git push origin main
```

Expected: `ios-test`, `ios-build` e `windows-test` verdes no `main`.
