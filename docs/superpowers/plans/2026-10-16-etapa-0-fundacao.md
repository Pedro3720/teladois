# Etapa 0 — Fundação: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provar o caminho completo "código no Windows → GitHub Actions (macOS) compila → `.ipa` sem assinatura → Sideloadly instala no iPhone" com um app "Olá" em paisagem, com testes unitários e de UI rodando no CI.

**Architecture:** O projeto Xcode não é versionado; ele é gerado no CI pelo XcodeGen a partir de `ios/project.yml`. Dois workflows: `ios-test.yml` (testes unitários + XCUITest no simulador, exporta prints) e `ios-build.yml` (compila para dispositivo sem assinatura e empacota o `.ipa`). A assinatura acontece no Windows, no Sideloadly, com Apple ID grátis.

**Tech Stack:** Swift 5 / SwiftUI, XcodeGen, XCTest/XCUITest, GitHub Actions (`macos-15`), GitHub CLI (`gh`), Sideloadly, REA (inspeção do `.ipa`).

Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md`

## Global Constraints

- Não existe Mac: **nunca** rodar `xcodebuild`/`xcodegen` localmente; o CI é o único verificador do código iOS.
- iOS mínimo: **17.0**. Apenas iPhone (`TARGETED_DEVICE_FAMILY = 1`).
- Orientação: **somente paisagem** (`UIInterfaceOrientationLandscapeLeft`, `UIInterfaceOrientationLandscapeRight`).
- Nome provisório no código: **`TelaDois`**. Bundle ID: **`com.pedrovviana.teladois`**.
- Build de dispositivo **sem assinatura** (`CODE_SIGNING_ALLOWED=NO`).
- Repositório GitHub **público**.
- Documentos para o usuário em **português**; identificadores de código em inglês.
- Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `.gitignore` | Ignora artefatos de build iOS/.NET e o `.xcodeproj` gerado |
| `.gitattributes` | Força LF em Swift/YAML/shell (o CI roda em macOS) |
| `CLAUDE.md` | Guia do projeto para o Claude: estrutura, regras, como verificar |
| `README.md` | Visão geral para humanos |
| `docs/INSTALACAO.md` | Passo a passo para instalar o `.ipa` no iPhone com Sideloadly |
| `ios/project.yml` | Definição XcodeGen: app + testes unitários + testes de UI |
| `ios/TelaDois/TelaDoisApp.swift` | Ponto de entrada SwiftUI |
| `ios/TelaDois/ContentView.swift` | Tela "Olá" em paisagem |
| `ios/TelaDois/Greeting.swift` | Texto de boas-vindas (unidade testável mínima) |
| `ios/TelaDoisTests/GreetingTests.swift` | Teste unitário |
| `ios/TelaDoisUITests/LaunchUITests.swift` | Teste de UI: abre o app, confere o texto, salva print |
| `.github/scripts/pick-simulator.sh` | Escolhe o UDID do iPhone simulador mais recente disponível |
| `.github/workflows/ios-test.yml` | CI de testes + exportação de prints |
| `.github/workflows/ios-build.yml` | CI de build do `.ipa` |

---

### Task 1: Fundação do repositório

**Files:**
- Create: `.gitignore`, `.gitattributes`, `CLAUDE.md`, `README.md`

**Interfaces:**
- Consumes: nada
- Produces: regras de ignore que as Tasks 3–4 assumem (`ios/*.xcodeproj` não versionado; `build/` ignorado)

- [ ] **Step 1: Criar `.gitignore`**

```gitignore
# Xcode (projeto é gerado pelo XcodeGen no CI)
ios/*.xcodeproj/
ios/*.xcworkspace/
build/
DerivedData/
*.xcresult
*.ipa
Payload/
screenshots/

# .NET
windows/**/bin/
windows/**/obj/
*.user

# Node / Playwright (protótipo)
node_modules/
playwright-report/
test-results/

# SO / editores
.DS_Store
Thumbs.db
.vs/
.vscode/
```

- [ ] **Step 2: Criar `.gitattributes`**

```gitattributes
* text=auto
*.swift text eol=lf
*.yml   text eol=lf
*.yaml  text eol=lf
*.sh    text eol=lf
*.md    text eol=lf
*.png   binary
*.ipa   binary
```

- [ ] **Step 3: Criar `CLAUDE.md`**

```markdown
# TelaDois — guia do projeto

App de iPhone que (1) vira segunda tela do PC Windows via cabo USB e (2) tem um Modo Descanso estilo StandBy sem carregador.
Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md`. Planos: `docs/superpowers/plans/`.

## Regras
- O usuário fala português; documentos para ele em português, código em inglês.
- Não há Mac. Nunca rode `xcodebuild`/`xcodegen` localmente. O código iOS é verificado **só** pelo GitHub Actions:
  - `gh workflow run ios-test.yml` / `gh run watch` — testes + prints do simulador (artefato `screenshots`).
  - `gh workflow run ios-build.yml` — gera o artefato `TelaDois-ipa`.
- O `.xcodeproj` é gerado a partir de `ios/project.yml` (XcodeGen). Nunca versione o `.xcodeproj`.
- Instalação no iPhone: Sideloadly + Apple ID grátis (expira em 7 dias). Ver `docs/INSTALACAO.md`.
- Antes de entregar um `.ipa` ao usuário, inspecione-o com o REA (Info.plist, binário Mach-O).
- Quando houver código suficiente, use o `graphify` para navegar no repositório com menos tokens.

## Estrutura
- `ios/` — app Swift/SwiftUI (XcodeGen)
- `windows/` — app de bandeja C#/.NET (a partir da Etapa 4)
- `prototype/` — protótipo HTML do Descanso + Playwright (Etapa 1)
- `.github/workflows/` — CI
```

- [ ] **Step 4: Criar `README.md`**

```markdown
# TelaDois

Transforma o iPhone em **segunda tela do PC (via cabo USB)** e em um **Modo Descanso** estilo StandBy — relógio, clima, bateria e Spotify — sem precisar do carregador.

- Especificação: [docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md](docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md)
- Como instalar no iPhone: [docs/INSTALACAO.md](docs/INSTALACAO.md)

O app iOS é compilado pelo GitHub Actions (macOS) sem assinatura e instalado pelo Sideloadly com um Apple ID grátis.
```

- [ ] **Step 5: Verificar as regras de ignore**

Run (Bash): `cd "/c/Pedro/Arquivos Pedro/Aplicativo celular" && git check-ignore -v ios/TelaDois.xcodeproj/project.pbxproj build/x TelaDois.ipa windows/App/bin/x`
Expected: 4 linhas, cada uma citando a regra do `.gitignore` correspondente.

- [ ] **Step 6: Commit**

```bash
git add .gitignore .gitattributes CLAUDE.md README.md
git commit -m "chore: fundação do repositório (ignore, atributos, guias)"
```

---

### Task 2: Repositório público no GitHub

**Files:** nenhum arquivo novo (configuração remota).

**Interfaces:**
- Consumes: commits das Tasks 1 e da spec
- Produces: remoto `origin` em `github.com/<conta-do-usuário>/teladois` (a conta é a que aparecer no `gh auth status`), `gh` autenticado — as Tasks 3–5 usam `gh workflow run`, `gh run watch` e `gh run download`

> **Ações do usuário:** instalar software e fazer login exigem confirmação. Peça permissão antes do `winget install`. O login (`gh auth login`) é feito **pelo próprio usuário** no terminal/navegador — nunca digite credenciais por ele. Criar o repositório público é uma ação externa: confirme o nome com o usuário antes.

- [ ] **Step 1: Instalar o GitHub CLI (com permissão do usuário)**

Run (PowerShell): `winget install --id GitHub.cli -e --accept-source-agreements`
Expected: "Instalado com êxito". Reabra o terminal para o `gh` entrar no PATH.

- [ ] **Step 2: Usuário faz login**

O usuário roda no terminal: `gh auth login` → GitHub.com → HTTPS → "Login with a web browser".
Verificar (PowerShell): `gh auth status`
Expected: `Logged in to github.com account <usuario>`

- [ ] **Step 3: Criar o repositório público e enviar (após confirmar o nome com o usuário)**

Run (PowerShell): `gh repo create teladois --public --source . --remote origin --push`
Expected: URL `https://github.com/<usuario>/teladois` e `branch 'main' set up to track 'origin/main'`.

- [ ] **Step 4: Verificar**

Run: `git ls-remote origin main`
Expected: um hash igual ao de `git rev-parse HEAD`.

---

### Task 3: Esqueleto do app iOS + CI de testes (vermelho → verde no CI)

**Files:**
- Create: `ios/project.yml`, `ios/TelaDois/TelaDoisApp.swift`, `ios/TelaDois/ContentView.swift`, `ios/TelaDois/Greeting.swift`, `ios/TelaDoisTests/GreetingTests.swift`, `ios/TelaDoisUITests/LaunchUITests.swift`, `.github/scripts/pick-simulator.sh`, `.github/workflows/ios-test.yml`

**Interfaces:**
- Consumes: remoto `origin` e `gh` autenticado (Task 2)
- Produces:
  - `enum Greeting { static let text: String }` (valor `"Olá, TelaDois"`)
  - Identificador de acessibilidade `"greeting"` no texto da `ContentView`
  - Scheme XcodeGen `TelaDois` com alvos de teste `TelaDoisTests` e `TelaDoisUITests`
  - Workflow `ios-test.yml` (disparo manual e em push), artefatos `test-results` (`.xcresult`) e `screenshots` (PNGs)

> Por não haver Mac, o ciclo TDD roda no CI: o primeiro push **deve falhar** (prova que o CI detecta erro) e o segundo **deve passar**.

- [ ] **Step 1: Criar `ios/project.yml`**

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
    MARKETING_VERSION: "0.1.0"
    CURRENT_PROJECT_VERSION: "1"
targets:
  TelaDois:
    type: application
    platform: iOS
    sources: [TelaDois]
    settings:
      base:
        PRODUCT_BUNDLE_IDENTIFIER: com.pedrovviana.teladois
        GENERATE_INFOPLIST_FILE: YES
        INFOPLIST_KEY_CFBundleDisplayName: TelaDois
        INFOPLIST_KEY_UISupportedInterfaceOrientations: "UIInterfaceOrientationLandscapeLeft UIInterfaceOrientationLandscapeRight"
        INFOPLIST_KEY_UIRequiresFullScreen: YES
        INFOPLIST_KEY_UILaunchScreen_Generation: YES
        TARGETED_DEVICE_FAMILY: "1"
  TelaDoisTests:
    type: bundle.unit-test
    platform: iOS
    sources: [TelaDoisTests]
    dependencies:
      - target: TelaDois
    settings:
      base:
        GENERATE_INFOPLIST_FILE: YES
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
        - TelaDoisTests
        - TelaDoisUITests
```

- [ ] **Step 2: Escrever o teste unitário (falhará — `Greeting` ainda não existe)**

`ios/TelaDoisTests/GreetingTests.swift`:

```swift
import XCTest
@testable import TelaDois

final class GreetingTests: XCTestCase {
    func testGreetingText() {
        XCTAssertEqual(Greeting.text, "Olá, TelaDois")
    }
}
```

- [ ] **Step 3: Escrever o teste de UI**

`ios/TelaDoisUITests/LaunchUITests.swift`:

```swift
import XCTest

final class LaunchUITests: XCTestCase {
    override func setUp() {
        continueAfterFailure = false
        XCUIDevice.shared.orientation = .landscapeLeft
    }

    func testLaunchShowsGreetingInLandscape() {
        let app = XCUIApplication()
        app.launch()

        let greeting = app.staticTexts["greeting"]
        XCTAssertTrue(greeting.waitForExistence(timeout: 5))
        XCTAssertEqual(greeting.label, "Olá, TelaDois")
        XCTAssertGreaterThan(app.windows.firstMatch.frame.width, app.windows.firstMatch.frame.height)

        let shot = XCTAttachment(screenshot: app.screenshot())
        shot.name = "01-launch"
        shot.lifetime = .keepAlways
        add(shot)
    }
}
```

- [ ] **Step 4: Criar o app mínimo SEM `Greeting.swift`**

`ios/TelaDois/TelaDoisApp.swift`:

```swift
import SwiftUI

@main
struct TelaDoisApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}
```

`ios/TelaDois/ContentView.swift`:

```swift
import SwiftUI

struct ContentView: View {
    var body: some View {
        ZStack {
            Color.black.ignoresSafeArea()
            Text(Greeting.text)
                .font(.system(size: 64, weight: .bold, design: .rounded))
                .foregroundStyle(.white)
                .accessibilityIdentifier("greeting")
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
    }
}
```

- [ ] **Step 5: Criar o seletor de simulador**

`.github/scripts/pick-simulator.sh`:

```bash
#!/usr/bin/env bash
# Imprime o UDID do iPhone simulador disponível no runtime iOS mais recente.
set -euo pipefail
xcrun simctl list devices available -j | python3 -c '
import json, re, sys
devices = json.load(sys.stdin)["devices"]
def version(key):
    m = re.search(r"iOS-(\d+)-(\d+)", key)
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)
for runtime in sorted(devices, key=version, reverse=True):
    if "iOS" not in runtime:
        continue
    for d in devices[runtime]:
        if d["name"].startswith("iPhone"):
            print(d["udid"])
            sys.exit(0)
sys.exit("nenhum simulador de iPhone disponível")
'
```

- [ ] **Step 6: Criar o workflow de testes**

`.github/workflows/ios-test.yml`:

```yaml
name: ios-test
on:
  push:
    branches: [main]
    paths: ["ios/**", ".github/**"]
  workflow_dispatch:

jobs:
  test:
    runs-on: macos-15
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v4
      - name: Instalar XcodeGen
        run: brew install xcodegen
      - name: Gerar projeto
        working-directory: ios
        run: xcodegen generate
      - name: Escolher simulador
        id: sim
        run: echo "udid=$(bash .github/scripts/pick-simulator.sh)" >> "$GITHUB_OUTPUT"
      - name: Rodar testes
        working-directory: ios
        run: |
          xcodebuild test \
            -project TelaDois.xcodeproj \
            -scheme TelaDois \
            -destination "id=${{ steps.sim.outputs.udid }}" \
            -resultBundlePath ../TestResults.xcresult \
            CODE_SIGNING_ALLOWED=NO
      - name: Exportar prints
        if: always()
        run: |
          mkdir -p screenshots
          if [ -d TestResults.xcresult ]; then
            xcrun xcresulttool export attachments --path TestResults.xcresult --output-path screenshots || true
          fi
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: screenshots
          path: screenshots
          if-no-files-found: ignore
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults.xcresult
          if-no-files-found: ignore
```

- [ ] **Step 7: Commit e push — o CI DEVE falhar**

```bash
git add ios .github
git commit -m "test: esqueleto iOS e CI de testes (Greeting ainda ausente)"
git push
```

Run: `gh run watch $(gh run list --workflow ios-test.yml --limit 1 --json databaseId -q '.[0].databaseId') --exit-status`
Expected: FAIL no passo "Rodar testes" com `cannot find 'Greeting' in scope`. Se falhar por outro motivo (XcodeGen, simulador), corrija esse motivo primeiro com `superpowers:systematic-debugging` (`gh run view --log-failed`).

- [ ] **Step 8: Implementação mínima**

`ios/TelaDois/Greeting.swift`:

```swift
enum Greeting {
    static let text = "Olá, TelaDois"
}
```

- [ ] **Step 9: Commit e push — o CI DEVE passar**

```bash
git add ios/TelaDois/Greeting.swift
git commit -m "feat: tela inicial Olá em paisagem"
git push
```

Run: `gh run watch $(gh run list --workflow ios-test.yml --limit 1 --json databaseId -q '.[0].databaseId') --exit-status`
Expected: PASS — `GreetingTests.testGreetingText` e `LaunchUITests.testLaunchShowsGreetingInLandscape` passam.

- [ ] **Step 10: Conferir o print**

Run (Bash): `cd "/c/Pedro/Arquivos Pedro/Aplicativo celular" && rm -rf "$SCRATCHPAD/shots" && gh run download -n screenshots -D "$SCRATCHPAD/shots" && ls "$SCRATCHPAD/shots"`
(`$SCRATCHPAD` = diretório de rascunho da sessão; o `gh` descobre o repositório pelo remoto `origin` da pasta do projeto.) Abra o PNG com a ferramenta Read.
Expected: tela preta em paisagem com "Olá, TelaDois" em branco, centralizado, sem barra de status.

---

### Task 4: Build do `.ipa` sem assinatura + inspeção com REA

**Files:**
- Create: `.github/workflows/ios-build.yml`

**Interfaces:**
- Consumes: `ios/project.yml` e scheme `TelaDois` (Task 3)
- Produces: artefato `TelaDois-ipa` contendo `TelaDois.ipa` (estrutura `Payload/TelaDois.app`), usado na Task 5

- [ ] **Step 1: Criar o workflow de build**

`.github/workflows/ios-build.yml`:

```yaml
name: ios-build
on:
  push:
    branches: [main]
    paths: ["ios/**", ".github/workflows/ios-build.yml"]
  workflow_dispatch:

jobs:
  build:
    runs-on: macos-15
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v4
      - name: Instalar XcodeGen
        run: brew install xcodegen
      - name: Gerar projeto
        working-directory: ios
        run: xcodegen generate
      - name: Compilar para iPhone (sem assinatura)
        working-directory: ios
        run: |
          xcodebuild build \
            -project TelaDois.xcodeproj \
            -scheme TelaDois \
            -configuration Release \
            -sdk iphoneos \
            -destination "generic/platform=iOS" \
            -derivedDataPath ../build \
            CODE_SIGNING_ALLOWED=NO \
            CODE_SIGNING_REQUIRED=NO \
            CODE_SIGN_IDENTITY=""
      - name: Empacotar .ipa
        run: |
          mkdir -p Payload
          cp -R build/Build/Products/Release-iphoneos/TelaDois.app Payload/
          zip -qry TelaDois.ipa Payload
          ls -lh TelaDois.ipa
      - uses: actions/upload-artifact@v4
        with:
          name: TelaDois-ipa
          path: TelaDois.ipa
          if-no-files-found: error
```

- [ ] **Step 2: Commit, push e acompanhar**

```bash
git add .github/workflows/ios-build.yml
git commit -m "ci: build do .ipa sem assinatura"
git push
```

Run: `gh run watch $(gh run list --workflow ios-build.yml --limit 1 --json databaseId -q '.[0].databaseId') --exit-status`
Expected: PASS; o passo "Empacotar .ipa" mostra um arquivo de alguns centenas de KB.

- [ ] **Step 3: Baixar e extrair num diretório próprio (dado não confiável)**

Run (Bash):
```bash
cd "/c/Pedro/Arquivos Pedro/Aplicativo celular" && rm -rf "$SCRATCHPAD/ipa-download" "$SCRATCHPAD/ipa-extracted" && \
gh run download -n TelaDois-ipa -D "$SCRATCHPAD/ipa-download" && \
mkdir "$SCRATCHPAD/ipa-extracted" && unzip -q "$SCRATCHPAD/ipa-download/TelaDois.ipa" -d "$SCRATCHPAD/ipa-extracted" && \
ls "$SCRATCHPAD/ipa-extracted/Payload/TelaDois.app"
```
Expected: lista contendo `TelaDois` (binário) e `Info.plist`.

- [ ] **Step 4: Inspecionar com REA**

Use `mcp__rea__inspect_plist` em `$SCRATCHPAD/ipa-extracted/Payload/TelaDois.app/Info.plist` e `mcp__rea__inspect_macho` em `$SCRATCHPAD/ipa-extracted/Payload/TelaDois.app/TelaDois`.
Expected:
- `CFBundleIdentifier` = `com.pedrovviana.teladois`
- `MinimumOSVersion` = `17.0`
- `UISupportedInterfaceOrientations` contém só `LandscapeLeft` e `LandscapeRight`
- `UIDeviceFamily` = `[1]`
- Mach-O `arm64`, sem assinatura de código (o Sideloadly assina)
Se algo divergir, corrija `ios/project.yml` e repita os Steps 2–4.

---

### Task 5: Guia de instalação e instalação no iPhone

**Files:**
- Create: `docs/INSTALACAO.md`

**Interfaces:**
- Consumes: artefato `TelaDois-ipa` (Task 4)
- Produces: app instalado no iPhone do usuário; guia reutilizado em todas as etapas

> Os passos de instalação são feitos **pelo usuário** (o Sideloadly pede o Apple ID e a senha dele — nunca digite por ele). O Claude prepara o `.ipa` e o guia, e acompanha.

- [ ] **Step 1: Escrever `docs/INSTALACAO.md`**

```markdown
# Instalar o TelaDois no iPhone (sem conta paga)

## Uma vez só
1. Instale o app **Dispositivos Apple** (Microsoft Store) — ou o iTunes. Ele traz o driver USB do iPhone.
2. Instale o **Sideloadly**: https://sideloadly.io (versão Windows).
3. Conecte o iPhone no cabo, desbloqueie e toque em **Confiar neste computador**.

## A cada nova versão
1. Pegue o `TelaDois.ipa` mais recente (o Claude baixa do GitHub Actions para você, ou: GitHub → Actions → `ios-build` → última execução → artefato `TelaDois-ipa`, e descompacte o .zip).
2. Abra o Sideloadly, arraste o `TelaDois.ipa`, escolha seu iPhone, digite seu **Apple ID** e clique **Start**.
3. Na primeira instalação, no iPhone:
   - **Ajustes → Privacidade e Segurança → Modo Desenvolvedor → ativar** (o iPhone reinicia).
   - **Ajustes → Geral → VPN e Gerenciamento de Dispositivos →** toque no seu Apple ID → **Confiar**.
4. Abra o TelaDois.

## Validade
Com Apple ID grátis, o app **para de abrir após 7 dias**. Para renovar, repita o passo 2 (os dados do app são mantidos).
```

- [ ] **Step 2: Commit e push**

```bash
git add docs/INSTALACAO.md
git commit -m "docs: guia de instalação via Sideloadly"
git push
```

- [ ] **Step 3: Entregar o `.ipa` ao usuário**

Copie `$SCRATCHPAD/ipa-download/TelaDois.ipa` para uma pasta que o usuário escolher (por padrão `C:\Pedro\Arquivos Pedro\Aplicativo celular\builds\TelaDois.ipa` — `builds/` está fora do git porque `*.ipa` é ignorado) e envie com `SendUserFile`.

- [ ] **Step 4: Usuário instala e confirma**

O usuário segue `docs/INSTALACAO.md`.
Expected (confirmado pelo usuário, de preferência com um print do iPhone): o app abre em paisagem, tela preta com "Olá, TelaDois", sem barra de status.

- [ ] **Step 5: Encerrar a etapa**

Use `superpowers:verification-before-completion`: as evidências são (a) execução verde do `ios-test`, (b) execução verde do `ios-build`, (c) relatório do REA, (d) confirmação/print do usuário. Só então a Etapa 0 está concluída e o plano da Etapa 1 (Design) pode ser escrito.
