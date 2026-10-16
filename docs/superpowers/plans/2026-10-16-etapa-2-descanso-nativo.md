# Etapa 2 — Modo Descanso nativo (SwiftUI): Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir o app "Olá" pelo Modo Descanso de verdade: relógio estilo StandBy, data, temperatura (Open-Meteo), anel de bateria real, tela preta + brilho 0 após 60 s e acordar com 1 toque — testado no CI (unitário + XCUITest com prints).

**Architecture:** A lógica pura (estado de escurecimento, formatação de hora, URL/parse do Open-Meteo) vai para um pacote Swift local `TelaDoisCore` (Swift 6, testado com `swift test` no macOS do CI, sem app host). O app SwiftUI só monta a tela e liga os serviços do iOS (bateria, localização, brilho, tela sempre acesa). Testes de UI controlam hora, temperatura, bateria e tempo de escurecimento por argumentos de lançamento.

**Tech Stack:** Swift 6 (pacote) / Swift 5 com concorrência estrita (app), SwiftUI, Observation, CoreLocation (`CLLocationUpdate.liveUpdates`), XCTest/XCUITest, XcodeGen 2.46.0, Xcode 16.4 / iOS SDK 18.5, GitHub Actions `macos-15`.

Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§4.1, §4.2, §5, §7). Design aprovado: `docs/design/tokens.md` e protótipo `prototype/`.

**Fora desta etapa:** página de música, mini player e Spotify (Etapa 3 — a página do relógio fica sozinha, sem paginação); tudo da Segunda Tela (Etapas 4–5); validação no iPhone real (aguarda cabo de dados — ver `docs/INSTALACAO.md`). A fonte do relógio usa o SF Pro comprimido do sistema em vez de embutir a Sofia Sans do protótipo (sem download de fonte); se o print do Task 4 não ficar parecido, a troca vira um ajuste de design separado.

## Global Constraints

- Não existe Mac: **nunca** rodar `xcodebuild`/`xcodegen`/`swift` localmente; o CI é o único verificador do código iOS. gh: `"/c/Program Files/GitHub CLI/gh.exe"`, rodado da pasta do repositório.
- iOS mínimo **17.0**; só iPhone (`TARGETED_DEVICE_FAMILY = 1`); **somente paisagem**.
- Bundle ID **`com.pedrovviana.teladois`**; nome no código **`TelaDois`**; build de dispositivo **sem assinatura**.
- Trabalhar no branch **`etapa-2-descanso`**; só juntar ao `main` com `ios-test` **e** `ios-build` verdes no SHA final.
- Escurecer: **60 s** sem toque → tela preta (fade 1,2 s) + brilho 0; **1 toque** acorda na hora e o toque **não** aciona nada embaixo.
- Cores: `clock #7FA9F2`, `ink #F5F5F7`, `muted #8E8E93`, `battery #34C759`, `track #2C2C2E`, fundo `#000000`.
- Folga da coluna da direita: **≥ 48 pt** da borda direita e **≥ 24 pt** do topo (cantos arredondados), além da área segura.
- Clima: Open-Meteo `https://api.open-meteo.com/v1/forecast?latitude=…&longitude=…&current=temperature_2m`, atualização a cada **30 min**; sem permissão/rede → temperatura escondida.
- Textos para o usuário em português; identificadores em inglês.
- Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Antes de editar YAML de workflow, releia o arquivo depois da edição (continuações `\` e indentação). Não use `sed` para edições de várias linhas; use a ferramenta Edit.
- PowerShell 5.1 corrompe UTF-8 ao regravar arquivos com `Get-Content`/`Set-Content`: não use isso para editar arquivos do repositório.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `.github/scripts/pick-simulator.sh` | Escolhe "iPhone 16 Pro" no runtime igual ao SDK do Xcode (18.5) |
| `.github/scripts/rename-screenshots.py` | Renomeia prints exportados para `<nome do anexo>.png` usando o `manifest.json` |
| `.github/workflows/ios-test.yml` | + gatilho em `etapa-*`, + `swift test` do pacote, + renomear prints |
| `.github/workflows/ios-build.yml` | + gatilho em `etapa-*` |
| `ios/Packages/TelaDoisCore/Package.swift` | Pacote Swift local (Swift 6) |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/DimState.swift` | Estado de escurecer/acordar (puro) |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/ClockFormatter.swift` | Hora, minuto, "QUA.", dia |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/OpenMeteo.swift` | URL, parse e rótulo "23°C" |
| `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/*.swift` | Testes unitários do pacote |
| `ios/project.yml` | Pacote local, remove alvo `TelaDoisTests`, permissão de localização, concorrência estrita |
| `ios/TelaDois/TelaDoisApp.swift` | Abre `RestScreenView` |
| `ios/TelaDois/Theme.swift` | Tokens de cor e folgas |
| `ios/TelaDois/AppSettings.swift` | Ajustes vindos dos argumentos de lançamento (testes) |
| `ios/TelaDois/BatteryRingView.swift` | Anel da bateria |
| `ios/TelaDois/ClockPageView.swift` | Página do relógio (layout) |
| `ios/TelaDois/BatteryMonitor.swift` | Nível/carga reais (ou fixos nos testes) |
| `ios/TelaDois/WeatherProvider.swift` | Localização + Open-Meteo (ou fixo nos testes) |
| `ios/TelaDois/ScreenBrightness.swift` | Guarda/zera/restaura o brilho |
| `ios/TelaDois/RestScreenView.swift` | Tela raiz: relógio, escurecer, ciclo de vida |
| `ios/TelaDoisUITests/RestScreenUITests.swift` | Testes de UI + prints |
| Removidos | `ios/TelaDois/ContentView.swift`, `ios/TelaDois/Greeting.swift`, `ios/TelaDoisTests/`, `ios/TelaDoisUITests/LaunchUITests.swift` |

---

### Task 1: CI estável — simulador fixo, prints com nome legível e gatilho nos branches de etapa

**Files:**
- Modify: `.github/scripts/pick-simulator.sh`
- Create: `.github/scripts/rename-screenshots.py`
- Modify: `.github/workflows/ios-test.yml`, `.github/workflows/ios-build.yml`

**Interfaces:**
- Consumes: workflows atuais (Xcode 16.4 fixo por `DEVELOPER_DIR`).
- Produces: `ios-test` e `ios-build` rodam em push para `main` e `etapa-*`; artefato `screenshots` contém `<nome do anexo>.png` (ex.: `01-launch.png`); simulador = "iPhone 16 Pro" no runtime iOS 18.5.

- [ ] **Step 1: Criar o branch**

```bash
cd "/c/Pedro/Arquivos Pedro/Aplicativo celular" && git switch main && git pull -q && git switch -c etapa-2-descanso
```

- [ ] **Step 2: Reescrever `.github/scripts/pick-simulator.sh`**

```bash
#!/usr/bin/env bash
# Imprime o UDID do simulador de iPhone a usar nos testes.
# Estabilidade dos prints: usa o runtime iOS igual ao SDK do Xcode fixado (ex.: 18.5) e o modelo
# "iPhone 16 Pro". Sem esse par, cai para o primeiro iPhone do runtime mais novo e avisa no log.
set -euo pipefail
PREFERRED_MODEL="iPhone 16 Pro"
SDK_VERSION="$(xcrun --sdk iphonesimulator --show-sdk-version)"
export PREFERRED_MODEL SDK_VERSION
xcrun simctl list devices available -j | python3 -c '
import json, os, re, sys
preferred = os.environ["PREFERRED_MODEL"]
sdk = tuple(int(p) for p in os.environ["SDK_VERSION"].split(".")[:2])
devices = json.load(sys.stdin)["devices"]
def version(key):
    m = re.search(r"iOS-(\d+)-(\d+)", key)
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)
runtimes = sorted((k for k in devices if "iOS" in k), key=version, reverse=True)
for runtime in runtimes:
    if version(runtime) == sdk:
        for d in devices[runtime]:
            if d["name"] == preferred:
                print("simulador: %s (%s) = SDK %s" % (d["name"], runtime, os.environ["SDK_VERSION"]), file=sys.stderr)
                print(d["udid"])
                sys.exit(0)
for runtime in runtimes:
    iphones = [d for d in devices[runtime] if d["name"].startswith("iPhone")]
    if iphones:
        print("::warning::%s no runtime do SDK %s indisponível; usando %s (%s)"
              % (preferred, os.environ["SDK_VERSION"], iphones[0]["name"], runtime), file=sys.stderr)
        print(iphones[0]["udid"])
        sys.exit(0)
sys.exit("nenhum simulador de iPhone disponível")
'
```

- [ ] **Step 3: Criar `.github/scripts/rename-screenshots.py`**

```python
#!/usr/bin/env python3
"""Renomeia os prints exportados pelo xcresulttool para '<nome do anexo>.png'.

Uso: python3 rename-screenshots.py <pasta com manifest.json>
O export grava '<UUID>.png' e um manifest.json com 'suggestedHumanReadableName'
no formato '<nome>_<índice>_<UUID>.png'. Mantém o manifest.
"""
import json
import pathlib
import re
import sys

folder = pathlib.Path(sys.argv[1])
manifest = folder / "manifest.json"
if not manifest.exists():
    sys.exit(f"manifest.json não encontrado em {folder}")

pattern = re.compile(r"^(?P<name>.+)_\d+_[0-9A-Fa-f-]+\.png$")
used = set()
for test in json.loads(manifest.read_text(encoding="utf-8")):
    for att in test.get("attachments", []):
        src = folder / att["exportedFileName"]
        if not src.exists() or src.suffix != ".png":
            continue
        m = pattern.match(att.get("suggestedHumanReadableName", ""))
        base = m.group("name") if m else src.stem
        target = f"{base}.png"
        n = 2
        while target in used:
            target = f"{base}-{n}.png"
            n += 1
        used.add(target)
        src.rename(folder / target)
        print(f"{att['exportedFileName']} -> {target}")
```

- [ ] **Step 4: Ajustar `.github/workflows/ios-test.yml`**

Trocar o bloco `on:` por:

```yaml
on:
  push:
    branches: [main, "etapa-*"]
    paths:
      - "ios/**"
      - ".github/workflows/ios-test.yml"
      - ".github/scripts/**"
  workflow_dispatch:
```

E trocar o passo "Exportar prints" por:

```yaml
      - name: Exportar prints
        if: always()
        run: |
          mkdir -p screenshots
          if [ -d TestResults.xcresult ]; then
            xcrun xcresulttool export attachments --path TestResults.xcresult --output-path screenshots
            python3 .github/scripts/rename-screenshots.py screenshots
          fi
```

- [ ] **Step 5: Ajustar o `on:` de `.github/workflows/ios-build.yml`**

```yaml
on:
  push:
    branches: [main, "etapa-*"]
    paths:
      - "ios/**"
      - ".github/workflows/ios-build.yml"
      - ".github/scripts/**"
  workflow_dispatch:
```

- [ ] **Step 6: Reler os dois YAML editados e o script, depois commit e push**

```bash
git add .github/scripts/pick-simulator.sh .github/scripts/rename-screenshots.py .github/workflows/ios-test.yml .github/workflows/ios-build.yml
git commit -m "ci: simulador fixo no runtime do SDK, prints com nome legível e gatilho em etapa-*"
git push -u origin etapa-2-descanso
```

- [ ] **Step 7: Verificar no CI**

Pegue as execuções do SHA (`gh run list --branch etapa-2-descanso --json databaseId,workflowName,headSha,conclusion`), acompanhe com `gh run watch <id> --exit-status`.
Expected: `ios-test` e `ios-build` verdes; no log do `ios-test`, a linha `simulador: iPhone 16 Pro (…iOS-18-5) = SDK 18.5` (se aparecer `::warning::`, registre no relatório qual runtime/modelo foi usado — não é falha); artefato `screenshots` contém `01-launch.png` (baixe numa pasta nova do scratchpad e liste).

---

### Task 2: Pacote `TelaDoisCore` com `DimState` (TDD) e `swift test` no CI

**Files:**
- Create: `ios/Packages/TelaDoisCore/Package.swift`
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/DimState.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/DimStateTests.swift`
- Modify: `.github/workflows/ios-test.yml` (novo passo antes de "Gerar projeto")

**Interfaces:**
- Consumes: gatilhos do Task 1.
- Produces (público, módulo `TelaDoisCore`):
  - `struct DimState: Equatable, Sendable` com `init(now: Date, timeout: TimeInterval)`, `let timeout: TimeInterval` (mínimo 1), `private(set) var isDimmed: Bool`, `mutating func update(now: Date)`, `@discardableResult mutating func touch(now: Date) -> TouchResult`
  - `enum DimState.TouchResult: Equatable, Sendable { case passThrough, wokeUp }`

- [ ] **Step 1: Criar `ios/Packages/TelaDoisCore/Package.swift`**

```swift
// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "TelaDoisCore",
    platforms: [.iOS(.v17), .macOS(.v14)],
    products: [.library(name: "TelaDoisCore", targets: ["TelaDoisCore"])],
    targets: [
        .target(name: "TelaDoisCore"),
        .testTarget(name: "TelaDoisCoreTests", dependencies: ["TelaDoisCore"]),
    ]
)
```

O SwiftPM exige ao menos um arquivo em `Sources/TelaDoisCore`. Crie `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/DimState.swift` agora só com a linha `import Foundation` (o Step 5 preenche o conteúdo), para o vermelho do Step 4 ser pela falta do tipo e não pela estrutura do pacote.

- [ ] **Step 2: Escrever o teste (vai falhar: `DimState` não existe)**

`ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/DimStateTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class DimStateTests: XCTestCase {
    private let t0 = Date(timeIntervalSince1970: 1_000_000)

    func testStaysAwakeBeforeTimeout() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(59.9))
        XCTAssertFalse(state.isDimmed)
    }

    func testDimsAtTimeout() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(60))
        XCTAssertTrue(state.isDimmed)
    }

    func testTouchWhileAwakeRestartsCountdownAndPassesThrough() {
        var state = DimState(now: t0, timeout: 60)
        XCTAssertEqual(state.touch(now: t0.addingTimeInterval(50)), .passThrough)
        state.update(now: t0.addingTimeInterval(100))
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(110))
        XCTAssertTrue(state.isDimmed)
    }

    func testTouchWhileDimmedWakesIsConsumedAndRestartsCountdown() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(61))
        XCTAssertEqual(state.touch(now: t0.addingTimeInterval(70)), .wokeUp)
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(129))
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(130))
        XCTAssertTrue(state.isDimmed)
    }

    func testTimeoutHasMinimumOfOneSecond() {
        XCTAssertEqual(DimState(now: t0, timeout: 0).timeout, 1)
    }
}
```

- [ ] **Step 3: Adicionar o passo de testes do pacote em `ios-test.yml`, logo depois de "Versão do Xcode"**

```yaml
      - name: Testes do pacote TelaDoisCore (swift test)
        run: swift test --package-path ios/Packages/TelaDoisCore
```

- [ ] **Step 4: Commit e push — CI DEVE falhar no `swift test`**

```bash
git add ios/Packages/TelaDoisCore .github/workflows/ios-test.yml
git commit -m "test(core): DimState — escurecer após o tempo e acordar com toque (ainda sem implementação)"
git push
```

Expected: `ios-test` vermelho no passo "Testes do pacote TelaDoisCore" com `cannot find 'DimState' in scope`.

- [ ] **Step 5: Implementar `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/DimState.swift`**

```swift
import Foundation

/// Estado do Modo Descanso: escurece depois de `timeout` segundos sem toque; um toque com a
/// tela preta só acorda (o toque é consumido e não chega aos controles embaixo).
public struct DimState: Equatable, Sendable {
    public enum TouchResult: Equatable, Sendable {
        case passThrough
        case wokeUp
    }

    public let timeout: TimeInterval
    public private(set) var isDimmed = false
    private var lastInteraction: Date

    public init(now: Date, timeout: TimeInterval) {
        self.timeout = max(1, timeout)
        self.lastInteraction = now
    }

    public mutating func update(now: Date) {
        if !isDimmed, now.timeIntervalSince(lastInteraction) >= timeout {
            isDimmed = true
        }
    }

    @discardableResult
    public mutating func touch(now: Date) -> TouchResult {
        lastInteraction = now
        if isDimmed {
            isDimmed = false
            return .wokeUp
        }
        return .passThrough
    }
}
```

- [ ] **Step 6: Commit e push — CI DEVE passar**

```bash
git add ios/Packages/TelaDoisCore/Sources
git commit -m "feat(core): DimState"
git push
```

Expected: `ios-test` verde; log mostra `Executed 5 tests, with 0 failures` no passo do pacote.

---

### Task 3: Formatação de hora (`ClockFormatter`) e clima (`OpenMeteo`) no pacote (TDD)

**Files:**
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/ClockFormatter.swift`
- Create: `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/OpenMeteo.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/ClockFormatterTests.swift`
- Test: `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/OpenMeteoTests.swift`

**Interfaces:**
- Consumes: pacote do Task 2.
- Produces (público):
  - `struct ClockText: Equatable, Sendable { let hours, minutes, weekday, day: String; init(hours:minutes:weekday:day:) }`
  - `enum ClockFormatter { static func text(for date: Date, timeZone: TimeZone, locale: Locale = Locale(identifier: "pt_BR")) -> ClockText }`
  - `enum OpenMeteo { static func currentTemperatureURL(latitude: Double, longitude: Double) -> URL; static func parseCurrentTemperature(_ data: Data) throws -> Double; static func label(celsius: Double) -> String; struct MissingTemperature: Error }`

- [ ] **Step 1: Escrever os testes (vão falhar)**

`ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/ClockFormatterTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class ClockFormatterTests: XCTestCase {
    private let saoPaulo = TimeZone(identifier: "America/Sao_Paulo")!

    private func date(_ iso: String) -> Date {
        ISO8601DateFormatter().date(from: iso)!
    }

    func testReferenceStandByTime() {
        let text = ClockFormatter.text(for: date("2026-10-14T10:51:30-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "10", minutes: "51", weekday: "QUA.", day: "14"))
    }

    func testUses24HoursAndPadsMinutes() {
        let text = ClockFormatter.text(for: date("2026-10-18T21:05:00-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "21", minutes: "05", weekday: "DOM.", day: "18"))
    }

    func testMidnightAndSingleDigitDay() {
        let text = ClockFormatter.text(for: date("2026-11-02T00:07:00-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "00", minutes: "07", weekday: "SEG.", day: "2"))
    }

    func testUsesGivenTimeZone() {
        let text = ClockFormatter.text(for: date("2026-10-14T10:51:30-03:00"), timeZone: TimeZone(identifier: "UTC")!)
        XCTAssertEqual(text.hours, "13")
    }
}
```

`ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/OpenMeteoTests.swift`:

```swift
import XCTest
@testable import TelaDoisCore

final class OpenMeteoTests: XCTestCase {
    func testBuildsCurrentTemperatureURLWithThreeDecimals() {
        let url = OpenMeteo.currentTemperatureURL(latitude: -23.55052, longitude: -46.633308)
        XCTAssertEqual(
            url.absoluteString,
            "https://api.open-meteo.com/v1/forecast?latitude=-23.551&longitude=-46.633&current=temperature_2m"
        )
    }

    func testParsesCurrentTemperature() throws {
        let json = #"{"latitude":-23.5,"current_units":{"temperature_2m":"°C"},"current":{"time":"2026-10-14T10:45","interval":900,"temperature_2m":23.4}}"#
        XCTAssertEqual(try OpenMeteo.parseCurrentTemperature(Data(json.utf8)), 23.4, accuracy: 0.001)
    }

    func testRejectsResponseWithoutTemperature() {
        let json = #"{"error":true,"reason":"Latitude must be in range of -90 to 90°."}"#
        XCTAssertThrowsError(try OpenMeteo.parseCurrentTemperature(Data(json.utf8)))
    }

    func testLabelRoundsToWholeDegrees() {
        XCTAssertEqual(OpenMeteo.label(celsius: 23.4), "23°C")
        XCTAssertEqual(OpenMeteo.label(celsius: 22.5), "23°C")
        XCTAssertEqual(OpenMeteo.label(celsius: -0.4), "0°C")
        XCTAssertEqual(OpenMeteo.label(celsius: -3.6), "-4°C")
    }
}
```

- [ ] **Step 2: Commit e push — CI DEVE falhar**

```bash
git add ios/Packages/TelaDoisCore/Tests
git commit -m "test(core): formatação de hora pt-BR e Open-Meteo (ainda sem implementação)"
git push
```

Expected: `ios-test` vermelho no passo do pacote com `cannot find 'ClockFormatter' in scope` / `cannot find 'OpenMeteo' in scope`.

- [ ] **Step 3: Implementar `ClockFormatter.swift`**

```swift
import Foundation

public struct ClockText: Equatable, Sendable {
    public let hours: String
    public let minutes: String
    public let weekday: String
    public let day: String

    public init(hours: String, minutes: String, weekday: String, day: String) {
        self.hours = hours
        self.minutes = minutes
        self.weekday = weekday
        self.day = day
    }
}

/// Textos do relógio do Modo Descanso: "10", "51", "QUA.", "14" (24 h, pt-BR).
public enum ClockFormatter {
    public static func text(for date: Date, timeZone: TimeZone, locale: Locale = Locale(identifier: "pt_BR")) -> ClockText {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = timeZone
        let parts = calendar.dateComponents([.hour, .minute, .day], from: date)

        let weekdayFormatter = DateFormatter()
        weekdayFormatter.locale = locale
        weekdayFormatter.timeZone = timeZone
        weekdayFormatter.dateFormat = "EEE"
        var weekday = weekdayFormatter.string(from: date).uppercased(with: locale)
        if !weekday.hasSuffix(".") { weekday += "." }

        return ClockText(
            hours: String(format: "%02d", parts.hour ?? 0),
            minutes: String(format: "%02d", parts.minute ?? 0),
            weekday: weekday,
            day: String(parts.day ?? 0)
        )
    }
}
```

- [ ] **Step 4: Implementar `OpenMeteo.swift`**

```swift
import Foundation

/// Temperatura atual pelo Open-Meteo (grátis, sem chave).
public enum OpenMeteo {
    public struct MissingTemperature: Error {}

    private struct Response: Decodable {
        struct Current: Decodable {
            let temperature: Double
            enum CodingKeys: String, CodingKey { case temperature = "temperature_2m" }
        }
        let current: Current?
    }

    public static func currentTemperatureURL(latitude: Double, longitude: Double) -> URL {
        let text = String(
            format: "https://api.open-meteo.com/v1/forecast?latitude=%.3f&longitude=%.3f&current=temperature_2m",
            latitude, longitude
        )
        return URL(string: text)!
    }

    public static func parseCurrentTemperature(_ data: Data) throws -> Double {
        guard let temperature = try JSONDecoder().decode(Response.self, from: data).current?.temperature else {
            throw MissingTemperature()
        }
        return temperature
    }

    public static func label(celsius: Double) -> String {
        "\(Int(celsius.rounded()))°C"
    }
}
```

- [ ] **Step 5: Commit e push — CI DEVE passar**

```bash
git add ios/Packages/TelaDoisCore/Sources
git commit -m "feat(core): ClockFormatter e OpenMeteo"
git push
```

Expected: `ios-test` verde; passo do pacote com `Executed 13 tests, with 0 failures`.

---

### Task 4: Página do relógio nativa (layout, bateria e clima) com teste de UI e prints

**Files:**
- Modify: `ios/project.yml`
- Create: `ios/TelaDois/Theme.swift`, `ios/TelaDois/AppSettings.swift`, `ios/TelaDois/BatteryRingView.swift`, `ios/TelaDois/ClockPageView.swift`, `ios/TelaDois/BatteryMonitor.swift`, `ios/TelaDois/WeatherProvider.swift`, `ios/TelaDois/RestScreenView.swift`
- Modify: `ios/TelaDois/TelaDoisApp.swift`
- Delete: `ios/TelaDois/ContentView.swift`, `ios/TelaDois/Greeting.swift`, `ios/TelaDoisTests/GreetingTests.swift`, `ios/TelaDoisUITests/LaunchUITests.swift`
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift` (usa `normalized(_:)` de `ScreenshotHelpers.swift`, que já existe)

**Interfaces:**
- Consumes: `ClockFormatter.text(for:timeZone:)`, `ClockText`, `OpenMeteo.label(celsius:)`, `OpenMeteo.currentTemperatureURL`, `OpenMeteo.parseCurrentTemperature` (Task 3).
- Produces:
  - `struct AppSettings { var dimSeconds: TimeInterval; var fixedStart: Date?; var fixedTemperature: Double?; var fixedBatteryLevel: Double?; var timeZone: TimeZone; static func fromLaunchArguments(_:) -> AppSettings }` — argumentos `-TDDimSeconds`, `-TDFixedDate` (ISO 8601), `-TDFixedTemp`, `-TDFixedBattery` (0…1), `-TDTimeZone`
  - `struct RestScreenView: View { init(settings: AppSettings) }` (o Task 5 acrescenta o escurecimento aqui)
  - Identificadores de acessibilidade: `clock` (rótulo "10:51"), `date` ("QUA., 14"), `temp` ("23°C"), `battery` (valor "80%")

- [ ] **Step 1: Escrever o teste de UI (vai falhar: os elementos não existem)**

Apague `ios/TelaDoisUITests/LaunchUITests.swift` e crie `ios/TelaDoisUITests/RestScreenUITests.swift`:

```swift
import XCTest
import UIKit

final class RestScreenUITests: XCTestCase {
    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    /// Abre o app com hora, fuso, temperatura e bateria fixos e espera a paisagem.
    private func launch(dimSeconds: Int = 600) -> XCUIApplication {
        let app = XCUIApplication()
        app.launchArguments += [
            "-TDFixedDate", "2026-10-14T10:51:05-03:00",
            "-TDTimeZone", "America/Sao_Paulo",
            "-TDFixedTemp", "23",
            "-TDFixedBattery", "0.8",
            "-TDDimSeconds", String(dimSeconds),
        ]
        app.launch()
        XCUIDevice.shared.orientation = .landscapeLeft
        let window = app.windows.firstMatch
        XCTAssertTrue(window.waitForExistence(timeout: 5))
        let landscape = NSPredicate { _, _ in window.frame.width > window.frame.height }
        wait(for: [XCTNSPredicateExpectation(predicate: landscape, object: nil)], timeout: 10)
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 5))
        return app
    }

    private func element(_ app: XCUIApplication, _ id: String) -> XCUIElement {
        app.descendants(matching: .any)[id].firstMatch
    }

    private func attachScreenshot(_ name: String) {
        let image = Self.normalized(XCUIScreen.main.screenshot().image)
        XCTAssertGreaterThan(image.size.width, image.size.height, "print deve ser em paisagem")
        let shot = XCTAttachment(image: image)
        shot.name = name
        shot.lifetime = .keepAlways
        add(shot)
    }

    func testClockPageShowsTimeDateTemperatureAndBattery() {
        let app = launch()
        XCTAssertEqual(app.staticTexts["clock"].label, "10:51")
        XCTAssertEqual(element(app, "date").label, "QUA., 14")
        XCTAssertEqual(element(app, "temp").label, "23°C")
        XCTAssertEqual(element(app, "battery").value as? String, "80%")
        attachScreenshot("01-relogio")
    }

    func testLayoutFitsAndKeepsRightColumnAwayFromCorner() {
        let app = launch()
        let window = app.windows.firstMatch.frame
        let clock = app.staticTexts["clock"].frame
        let date = element(app, "date").frame
        let battery = element(app, "battery").frame
        for frame in [clock, date, battery] {
            XCTAssertTrue(window.contains(frame), "\(frame) fora da janela \(window)")
        }
        XCTAssertLessThan(clock.maxX, date.minX, "relógio invade a coluna da direita")
        XCTAssertGreaterThanOrEqual(window.maxX - battery.maxX, 44, "bateria colada na borda direita")
        XCTAssertGreaterThanOrEqual(battery.minY - window.minY, 24, "bateria colada no topo")
        XCTAssertGreaterThan(clock.height, window.height * 0.5, "relógio deve ocupar boa parte da altura")
    }
}
```

- [ ] **Step 2: Atualizar `ios/project.yml`**

Arquivo completo:

```yaml
name: TelaDois
options:
  bundleIdPrefix: com.pedrovviana
  deploymentTarget:
    iOS: "17.0"
  createIntermediateGroups: true
settings:
  base:
    SWIFT_VERSION: "5.0"
    SWIFT_STRICT_CONCURRENCY: complete
    MARKETING_VERSION: "0.2.0"
    CURRENT_PROJECT_VERSION: "1"
packages:
  TelaDoisCore:
    path: Packages/TelaDoisCore
targets:
  TelaDois:
    type: application
    platform: iOS
    sources: [TelaDois]
    dependencies:
      - package: TelaDoisCore
    settings:
      base:
        PRODUCT_BUNDLE_IDENTIFIER: com.pedrovviana.teladois
        GENERATE_INFOPLIST_FILE: YES
        INFOPLIST_KEY_CFBundleDisplayName: TelaDois
        INFOPLIST_KEY_UISupportedInterfaceOrientations: "UIInterfaceOrientationLandscapeLeft UIInterfaceOrientationLandscapeRight"
        INFOPLIST_KEY_UIRequiresFullScreen: YES
        INFOPLIST_KEY_UILaunchScreen_Generation: YES
        INFOPLIST_KEY_UIStatusBarHidden: YES
        INFOPLIST_KEY_NSLocationWhenInUseUsageDescription: "A temperatura do Modo Descanso usa a sua localização aproximada."
        ASSETCATALOG_COMPILER_APPICON_NAME: AppIcon
        TARGETED_DEVICE_FAMILY: "1"
  TelaDoisUITests:
    type: bundle.ui-testing
    platform: iOS
    sources: [TelaDoisUITests]
    dependencies:
      - target: TelaDois
    settings:
      base:
        GENERATE_INFOPLIST_FILE: YES
        TEST_TARGET_NAME: TelaDois
schemes:
  TelaDois:
    build:
      targets:
        TelaDois: all
    test:
      targets:
        - TelaDoisUITests
```

Apague `ios/TelaDoisTests/GreetingTests.swift` (a pasta `ios/TelaDoisTests/` deixa de existir), `ios/TelaDois/Greeting.swift` e `ios/TelaDois/ContentView.swift`. Troque `ios/TelaDois/TelaDoisApp.swift` por:

```swift
import SwiftUI

@main
struct TelaDoisApp: App {
    private let settings = AppSettings.fromLaunchArguments()

    var body: some Scene {
        WindowGroup {
            RestScreenView(settings: settings)
        }
    }
}
```

Crie um `ios/TelaDois/RestScreenView.swift` **provisório** que compila mas não mostra nada (para o vermelho do teste ser por falta de elementos, não por erro de compilação):

```swift
import SwiftUI

struct RestScreenView: View {
    let settings: AppSettings

    var body: some View {
        Color.black.ignoresSafeArea()
    }
}
```

E `ios/TelaDois/AppSettings.swift`:

```swift
import Foundation

/// Ajustes lidos dos argumentos de lançamento (usados pelos testes de UI), ex.: `-TDDimSeconds 3`.
struct AppSettings {
    var dimSeconds: TimeInterval = 60
    /// O relógio começa nesta data e anda normalmente a partir dela.
    var fixedStart: Date?
    var fixedTemperature: Double?
    /// Nível de bateria fixo entre 0 e 1.
    var fixedBatteryLevel: Double?
    var timeZone: TimeZone = .current

    static func fromLaunchArguments(_ defaults: UserDefaults = .standard) -> AppSettings {
        var settings = AppSettings()
        let seconds = defaults.double(forKey: "TDDimSeconds")
        if seconds > 0 { settings.dimSeconds = seconds }
        if let iso = defaults.string(forKey: "TDFixedDate") {
            settings.fixedStart = ISO8601DateFormatter().date(from: iso)
        }
        if defaults.object(forKey: "TDFixedTemp") != nil {
            settings.fixedTemperature = defaults.double(forKey: "TDFixedTemp")
        }
        if defaults.object(forKey: "TDFixedBattery") != nil {
            settings.fixedBatteryLevel = defaults.double(forKey: "TDFixedBattery")
        }
        if let id = defaults.string(forKey: "TDTimeZone"), let zone = TimeZone(identifier: id) {
            settings.timeZone = zone
        }
        return settings
    }
}
```

- [ ] **Step 3: Commit e push — CI DEVE falhar nos testes de UI**

```bash
git add -A ios
git commit -m "test(ui): página do relógio — hora, data, clima, bateria e layout (tela ainda vazia)"
git push
```

Expected: `ios-test` vermelho; `testClockPageShowsTimeDateTemperatureAndBattery` e `testLayoutFitsAndKeepsRightColumnAwayFromCorner` falham em `XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 5))`. O passo do pacote continua verde. Se falhar por compilação, corrija a compilação primeiro (`gh run view <id> --log-failed`).

- [ ] **Step 4: Criar `ios/TelaDois/Theme.swift`**

```swift
import SwiftUI

/// Tokens de `docs/design/tokens.md`.
enum Theme {
    static let clock = Color(red: 0x7F / 255, green: 0xA9 / 255, blue: 0xF2 / 255)
    static let ink = Color(red: 0xF5 / 255, green: 0xF5 / 255, blue: 0xF7 / 255)
    static let muted = Color(red: 0x8E / 255, green: 0x8E / 255, blue: 0x93 / 255)
    static let battery = Color(red: 0x34 / 255, green: 0xC7 / 255, blue: 0x59 / 255)
    static let track = Color(red: 0x2C / 255, green: 0x2C / 255, blue: 0x2E / 255)

    /// Folgas mínimas até as bordas (cantos arredondados), somadas à área segura.
    static let rightClearance: CGFloat = 48
    static let topClearance: CGFloat = 24
    static let sideMargin: CGFloat = 16
    static let bottomMargin: CGFloat = 12
}
```

- [ ] **Step 5: Criar `ios/TelaDois/BatteryRingView.swift`**

```swift
import SwiftUI

struct BatteryRingView: View {
    /// Entre 0 e 1.
    let level: Double
    let isCharging: Bool

    private var percent: Int { Int((min(max(level, 0), 1) * 100).rounded()) }

    var body: some View {
        GeometryReader { geo in
            let side = min(geo.size.width, geo.size.height)
            let line = side * 4.5 / 44
            ZStack {
                Circle().stroke(Theme.track, lineWidth: line)
                Circle()
                    .trim(from: 0, to: CGFloat(percent) / 100)
                    .stroke(Theme.battery, style: StrokeStyle(lineWidth: line, lineCap: .round))
                    .rotationEffect(.degrees(-90))
                Group {
                    if isCharging {
                        Image(systemName: "bolt.fill")
                    } else {
                        Text("\(percent)")
                    }
                }
                .font(.system(size: side * 0.3, weight: .bold, design: .rounded))
                .foregroundStyle(Theme.battery)
            }
            .padding(line / 2)
        }
        .accessibilityElement(children: .ignore)
        .accessibilityIdentifier("battery")
        .accessibilityLabel("Bateria")
        .accessibilityValue("\(percent)%")
    }
}
```

- [ ] **Step 6: Criar `ios/TelaDois/ClockPageView.swift`**

```swift
import SwiftUI
import TelaDoisCore

/// Página 1 do Modo Descanso: relógio gigante à esquerda; data, temperatura e bateria à direita.
struct ClockPageView: View {
    let clock: ClockText
    let temperature: String?
    let batteryLevel: Double
    let isCharging: Bool
    /// Área segura da tela (a página em si ignora a área segura e aplica as folgas manualmente).
    let safeArea: EdgeInsets

    var body: some View {
        GeometryReader { geo in
            let height = geo.size.height
            let width = geo.size.width
            HStack(alignment: .center, spacing: width * 0.04) {
                Text("\(clock.hours):\(clock.minutes)")
                    .font(.system(size: height * 0.95, weight: .bold).width(.compressed))
                    .monospacedDigit()
                    .foregroundStyle(Theme.clock)
                    .lineLimit(1)
                    .minimumScaleFactor(0.3)
                    // Identificador no próprio Text (antes do .frame) para o teste medir o texto, não a coluna.
                    .accessibilityIdentifier("clock")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)

                VStack(alignment: .leading, spacing: 0) {
                    HStack(alignment: .top, spacing: 12) {
                        VStack(alignment: .leading, spacing: 0) {
                            (Text(clock.weekday).foregroundStyle(Theme.clock)
                                + Text(", \(clock.day)").foregroundStyle(Theme.ink))
                                .accessibilityIdentifier("date")
                            if let temperature {
                                Text(temperature)
                                    .foregroundStyle(Theme.ink)
                                    .accessibilityIdentifier("temp")
                            }
                        }
                        .font(.system(size: height * 0.095, weight: .semibold, design: .rounded))
                        .lineLimit(1)
                        Spacer(minLength: 0)
                        BatteryRingView(level: batteryLevel, isCharging: isCharging)
                            .frame(width: height * 0.22, height: height * 0.22)
                    }
                    Spacer(minLength: 0)
                }
                .frame(width: width * 0.3)
            }
            .padding(.leading, max(safeArea.leading + 12, Theme.sideMargin))
            .padding(.trailing, max(safeArea.trailing + 12, Theme.rightClearance))
            .padding(.top, max(safeArea.top + 12, Theme.topClearance))
            .padding(.bottom, max(safeArea.bottom + 12, Theme.bottomMargin))
        }
    }
}
```

- [ ] **Step 7: Criar `ios/TelaDois/BatteryMonitor.swift`**

```swift
import UIKit
import Observation

/// Nível e estado de carga da bateria (fixos quando o teste pede).
@MainActor
@Observable
final class BatteryMonitor {
    private(set) var level: Double = 1
    private(set) var isCharging = false

    @ObservationIgnored private let fixedLevel: Double?
    @ObservationIgnored private var observers: [NSObjectProtocol] = []

    init(fixedLevel: Double?) {
        self.fixedLevel = fixedLevel
    }

    func start() {
        if let fixedLevel {
            level = fixedLevel
            return
        }
        guard observers.isEmpty else { return }
        UIDevice.current.isBatteryMonitoringEnabled = true
        refresh()
        for name in [UIDevice.batteryLevelDidChangeNotification, UIDevice.batteryStateDidChangeNotification] {
            let token = NotificationCenter.default.addObserver(forName: name, object: nil, queue: .main) { [weak self] _ in
                MainActor.assumeIsolated { self?.refresh() }
            }
            observers.append(token)
        }
    }

    private func refresh() {
        let device = UIDevice.current
        // O simulador informa -1 (desconhecido): mostra cheio.
        level = device.batteryLevel < 0 ? 1 : Double(device.batteryLevel)
        isCharging = device.batteryState == .charging || device.batteryState == .full
    }
}
```

- [ ] **Step 8: Criar `ios/TelaDois/WeatherProvider.swift`**

```swift
import CoreLocation
import Observation
import TelaDoisCore

/// Temperatura atual: localização (quando permitida) + Open-Meteo a cada 30 min.
/// Sem permissão, sem rede ou com resposta inválida, `temperatureLabel` fica `nil` (a tela esconde).
@MainActor
@Observable
final class WeatherProvider {
    private(set) var temperatureLabel: String?

    @ObservationIgnored private let fixedTemperature: Double?
    @ObservationIgnored private let manager = CLLocationManager()
    @ObservationIgnored private var task: Task<Void, Never>?

    static let refreshInterval: Duration = .seconds(30 * 60)

    init(fixedTemperature: Double?) {
        self.fixedTemperature = fixedTemperature
    }

    func start() {
        if let fixedTemperature {
            temperatureLabel = OpenMeteo.label(celsius: fixedTemperature)
            return
        }
        guard task == nil else { return }
        manager.requestWhenInUseAuthorization()
        task = Task { [weak self] in
            while !Task.isCancelled {
                await self?.refresh()
                try? await Task.sleep(for: WeatherProvider.refreshInterval)
            }
        }
    }

    private func refresh() async {
        do {
            for try await update in CLLocationUpdate.liveUpdates() {
                guard let location = update.location else { continue }
                let url = OpenMeteo.currentTemperatureURL(
                    latitude: location.coordinate.latitude,
                    longitude: location.coordinate.longitude
                )
                let (data, _) = try await URLSession.shared.data(from: url)
                temperatureLabel = OpenMeteo.label(celsius: try OpenMeteo.parseCurrentTemperature(data))
                return
            }
        } catch {
            // Mantém o último valor (ou nenhum); tenta de novo no próximo ciclo.
        }
    }
}
```

- [ ] **Step 9: Trocar o `RestScreenView.swift` provisório pela página do relógio**

```swift
import SwiftUI
import TelaDoisCore

/// Tela raiz do Modo Descanso.
struct RestScreenView: View {
    let settings: AppSettings

    @State private var now: Date
    @State private var battery: BatteryMonitor
    @State private var weather: WeatherProvider
    /// Diferença entre o relógio exibido e o real (só quando o teste fixa a hora).
    private let clockOffset: TimeInterval
    private let ticker = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    init(settings: AppSettings) {
        self.settings = settings
        let real = Date()
        let offset = settings.fixedStart.map { $0.timeIntervalSince(real) } ?? 0
        clockOffset = offset
        _now = State(initialValue: real.addingTimeInterval(offset))
        _battery = State(initialValue: BatteryMonitor(fixedLevel: settings.fixedBatteryLevel))
        _weather = State(initialValue: WeatherProvider(fixedTemperature: settings.fixedTemperature))
    }

    var body: some View {
        GeometryReader { geo in
            ZStack {
                Color.black
                ClockPageView(
                    clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                    temperature: weather.temperatureLabel,
                    batteryLevel: battery.level,
                    isCharging: battery.isCharging,
                    safeArea: geo.safeAreaInsets
                )
            }
            .ignoresSafeArea()
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
        .onReceive(ticker) { tick in
            now = tick.addingTimeInterval(clockOffset)
        }
        .onAppear {
            battery.start()
            weather.start()
        }
    }
}
```

- [ ] **Step 10: Commit e push — CI DEVE passar**

```bash
git add -A ios
git commit -m "feat: página do relógio do Modo Descanso (hora, data, clima, bateria)"
git push
```

Expected: `ios-test` e `ios-build` verdes. Se só a asserção `clock.height > 50%` falhar, o SF Pro comprimido ficou baixo demais: acrescente `.scaleEffect(x: 1, y: 1.3)` ao `Text` do relógio logo depois de `.minimumScaleFactor(0.3)` (antes do `.accessibilityIdentifier`), faça push de novo e registre o valor medido no relatório. Se o layout falhar de outro jeito, use `systematic-debugging` com os números do assert.

- [ ] **Step 11: Revisar o print**

Baixe o artefato `screenshots` (pasta nova do scratchpad) e abra `01-relogio.png` com Read.
Expected: fundo preto; "10:51" grande em azul `#7FA9F2` à esquerda; "QUA., 14" (QUA. em azul) e "23°C" à direita no topo; anel verde com "80" afastado do canto; nada cortado. Compare com o protótipo (`.playwright-mcp/proto-bateria.png` ou https://pedro3720.github.io/teladois/?intro=0&temp=23) e descreva as diferenças no relatório.

---

### Task 5: Escurecer após 60 s, brilho 0, tela sempre ligada e acordar com 1 toque

**Files:**
- Create: `ios/TelaDois/ScreenBrightness.swift`
- Modify: `ios/TelaDois/RestScreenView.swift`
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift` (novo teste)

**Interfaces:**
- Consumes: `DimState` (Task 2), `RestScreenView`/`AppSettings.dimSeconds` (Task 4).
- Produces: com a tela preta existe um botão de acessibilidade `dim-overlay`; um toque nele acorda (o elemento some).

- [ ] **Step 1: Escrever o teste de UI (vai falhar)**

Acrescente em `RestScreenUITests`:

```swift
    func testDimsAfterTimeoutAndOneTapWakes() {
        let app = launch(dimSeconds: 4)
        let overlay = app.buttons["dim-overlay"]
        XCTAssertTrue(overlay.waitForExistence(timeout: 10), "tela deveria escurecer após 4 s sem toque")
        attachScreenshot("02-escurecido")

        overlay.tap()
        let gone = XCTNSPredicateExpectation(predicate: NSPredicate(format: "exists == false"), object: overlay)
        wait(for: [gone], timeout: 3)
        XCTAssertTrue(app.staticTexts["clock"].isHittable, "relógio deveria voltar a aparecer")
        attachScreenshot("03-acordado")
    }
```

- [ ] **Step 2: Commit e push — CI DEVE falhar**

```bash
git add ios/TelaDoisUITests/RestScreenUITests.swift
git commit -m "test(ui): escurecer após o tempo e acordar com um toque"
git push
```

Expected: `ios-test` vermelho só em `testDimsAfterTimeoutAndOneTapWakes` ("tela deveria escurecer após 4 s sem toque").

- [ ] **Step 3: Criar `ios/TelaDois/ScreenBrightness.swift`**

```swift
import UIKit

/// Guarda o brilho do usuário ao escurecer e devolve ao acordar ou ao sair do app.
@MainActor
final class ScreenBrightness {
    private var saved: CGFloat?

    private var screen: UIScreen? {
        UIApplication.shared.connectedScenes.compactMap { ($0 as? UIWindowScene)?.screen }.first
    }

    func dim() {
        guard saved == nil, let screen else { return }
        saved = screen.brightness
        screen.brightness = 0
    }

    func restore() {
        guard let value = saved, let screen else { return }
        screen.brightness = value
        saved = nil
    }
}
```

- [ ] **Step 4: Ligar o escurecimento em `RestScreenView.swift`** (arquivo completo)

```swift
import SwiftUI
import TelaDoisCore

/// Tela raiz do Modo Descanso.
struct RestScreenView: View {
    let settings: AppSettings

    @State private var now: Date
    @State private var dim: DimState
    @State private var battery: BatteryMonitor
    @State private var weather: WeatherProvider
    @State private var brightness = ScreenBrightness()
    @Environment(\.scenePhase) private var scenePhase
    /// Diferença entre o relógio exibido e o real (só quando o teste fixa a hora).
    private let clockOffset: TimeInterval
    private let ticker = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    init(settings: AppSettings) {
        self.settings = settings
        let real = Date()
        let offset = settings.fixedStart.map { $0.timeIntervalSince(real) } ?? 0
        clockOffset = offset
        _now = State(initialValue: real.addingTimeInterval(offset))
        _dim = State(initialValue: DimState(now: real, timeout: settings.dimSeconds))
        _battery = State(initialValue: BatteryMonitor(fixedLevel: settings.fixedBatteryLevel))
        _weather = State(initialValue: WeatherProvider(fixedTemperature: settings.fixedTemperature))
    }

    var body: some View {
        GeometryReader { geo in
            ZStack {
                Color.black
                ClockPageView(
                    clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                    temperature: weather.temperatureLabel,
                    batteryLevel: battery.level,
                    isCharging: battery.isCharging,
                    safeArea: geo.safeAreaInsets
                )
                if dim.isDimmed {
                    // Camada preta por cima de tudo: recebe o toque, então nada embaixo é acionado.
                    Color.black
                        .contentShape(Rectangle())
                        .onTapGesture { wake() }
                        .transition(.opacity.animation(.easeIn(duration: 1.2)))
                        .accessibilityElement()
                        .accessibilityIdentifier("dim-overlay")
                        .accessibilityLabel("Tela escurecida")
                        .accessibilityHint("Toque para acender")
                        .accessibilityAddTraits(.isButton)
                }
            }
            .ignoresSafeArea()
            // Qualquer toque com a tela acesa reinicia a contagem de 60 s.
            .simultaneousGesture(
                DragGesture(minimumDistance: 0).onChanged { _ in
                    if !dim.isDimmed { dim.touch(now: Date()) }
                }
            )
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
        .onReceive(ticker) { tick in
            now = tick.addingTimeInterval(clockOffset)
            let wasDimmed = dim.isDimmed
            dim.update(now: tick)
            if dim.isDimmed, !wasDimmed { brightness.dim() }
        }
        .onAppear {
            battery.start()
            weather.start()
            UIApplication.shared.isIdleTimerDisabled = true
        }
        .onChange(of: scenePhase) { _, phase in
            UIApplication.shared.isIdleTimerDisabled = (phase == .active)
            if phase == .active {
                dim.touch(now: Date())
            } else {
                brightness.restore()
            }
        }
    }

    private func wake() {
        dim.touch(now: Date())
        brightness.restore()
    }
}
```

- [ ] **Step 5: Commit e push — CI DEVE passar**

```bash
git add ios/TelaDois/ScreenBrightness.swift ios/TelaDois/RestScreenView.swift
git commit -m "feat: tela preta com brilho 0 após o tempo, tela sempre ligada e acordar com um toque"
git push
```

Expected: `ios-test` (3 testes de UI + 13 do pacote) e `ios-build` verdes.

- [ ] **Step 6: Revisar os prints**

Baixe `screenshots` (pasta nova) e abra `02-escurecido.png` e `03-acordado.png`.
Expected: `02` totalmente preto; `03` igual ao `01-relogio.png` (hora pode ter avançado alguns segundos, mas não o minuto).

---

### Task 6: Entrega da etapa — `.ipa` inspecionado, documentação e `main`

**Files:**
- Modify: `CLAUDE.md` (seção Regras)
- Modify: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8, linha da Etapa 2: marcar "concluída no CI; validar no iPhone quando houver cabo de dados")

**Interfaces:**
- Consumes: tudo acima, CI verde no SHA final do branch.
- Produces: `main` atualizado; `builds/TelaDois.ipa` + `builds/BUILD_INFO.txt` do build final.

- [ ] **Step 1: Atualizar `CLAUDE.md`** — acrescente às Regras:

```markdown
- Lógica pura fica no pacote `ios/Packages/TelaDoisCore` (Swift 6, testado com `swift test` no CI). O app só monta telas e liga serviços do iOS.
- Testes de UI controlam o app por argumentos de lançamento: `-TDFixedDate <ISO8601>`, `-TDTimeZone <id>`, `-TDFixedTemp <°C>`, `-TDFixedBattery <0…1>`, `-TDDimSeconds <s>`.
- Prints do CI saem com o nome do anexo (`01-relogio.png` etc.) no artefato `screenshots`; simulador fixo: iPhone 16 Pro no runtime do SDK.
```

Atualize a linha da Etapa 2 na tabela do §8 da spec acrescentando " — concluída no CI em 2026-10-16; falta validar no iPhone (cabo de dados)".

- [ ] **Step 2: Commit e push; confirmar CI verde no SHA final**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md
git commit -m "docs: regras da Etapa 2 e status na especificação"
git push
```

Expected: como só docs mudaram, os workflows iOS não rodam neste SHA — o SHA final para o `.ipa` é o último que rodou `ios-test` e `ios-build` verdes (anote os dois IDs).

- [ ] **Step 3: Inspecionar o `.ipa` final** (pasta nova do scratchpad; Python real com `-I`)

Baixe `TelaDois-ipa` do `ios-build` verde, extraia e confira com `plistlib` e leitura do cabeçalho Mach-O:
Expected: `CFBundleIdentifier` = `com.pedrovviana.teladois`; `MinimumOSVersion` = `17.0`; orientações só `LandscapeLeft`/`LandscapeRight`; `UIDeviceFamily` = `[1]`; `CFBundleShortVersionString` = `0.2.0`; `CFBundleIconName` = `AppIcon`; `NSLocationWhenInUseUsageDescription` presente; `UIStatusBarHidden` = `True`; Mach-O `0xfeedfacf` arm64; sem `LC_CODE_SIGNATURE`.

- [ ] **Step 4: Juntar ao `main` (fast-forward) e enviar**

```bash
git switch main && git merge --ff-only etapa-2-descanso && git push origin main
```

Expected: `ios-test` e `ios-build` rodam no `main` e ficam verdes.

- [ ] **Step 5: Separar o `.ipa` para o usuário**

Copie `TelaDois.ipa` e `BUILD_INFO.txt` do `ios-build` verde do `main` para `builds/` (ignorado pelo git) e envie com `SendUserFile`. A instalação no iPhone fica para quando houver cabo de dados (`docs/INSTALACAO.md`).
