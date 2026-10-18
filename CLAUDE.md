# TelaDois — guia do projeto

App de iPhone que (1) vira **segunda tela do PC Windows via cabo USB** e (2) tem um **Modo Descanso** estilo StandBy sem precisar de carregador (relógio, data, clima, bateria, depois Spotify; tela preta após 60 s e acende com 1 toque).

- Especificação: `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md`
- Planos por etapa: `docs/superpowers/plans/`
- Design (cores, tipografia, layout): `docs/design/tokens.md` · ícone: `docs/design/icone-1024.png`
- Repositório: https://github.com/Pedro3720/teladois (público) · protótipo no ar: https://pedro3720.github.io/teladois/

## Visão geral

### Stack
| Parte | Tecnologia |
|---|---|
| App iPhone | Swift / SwiftUI, iOS 17+, só paisagem, projeto gerado pelo **XcodeGen** (`ios/project.yml`) |
| Lógica pura | Pacote Swift local `ios/Packages/TelaDoisCore` (Swift 6) |
| Build iOS | **GitHub Actions `macos-15`**, Xcode 16.4 / iOS SDK 18.5 fixos, XcodeGen 2.46.0 com SHA-256 verificado |
| Instalação no iPhone | `.ipa` sem assinatura → **Sideloadly** + Apple ID grátis no Windows |
| Protótipo | HTML/CSS/JS puro em `prototype/` + testes **Playwright (WebKit)**, publicado no GitHub Pages |
| PC (Etapas 4–5) | C#/.NET (app de bandeja) + FFmpeg + Virtual Display Driver + usbmuxd |
| Clima | Open-Meteo (grátis, sem chave) |

### Estrutura de pastas
- `ios/TelaDois/` — telas e serviços do app (SwiftUI)
- `ios/TelaDoisUITests/` — testes de UI (XCUITest) que geram os prints
- `ios/Packages/TelaDoisCore/` — lógica pura testável (`DimState`, `ClockFormatter`, `OpenMeteo`)
- `ios/TelaDois/Assets.xcassets/` — ícone do app
- `prototype/` — protótipo do Modo Descanso (`index.html`, `styles.css`, `app.js`, `serve.mjs`, `tests/`)
- `windows/` — app de bandeja C#/.NET (a partir da Etapa 4; ainda vazio)
- `.github/workflows/` — `ios-test.yml`, `ios-build.yml`, `prototype-test.yml`, `pages.yml`
- `.github/scripts/` — instalar XcodeGen, escolher simulador, renomear prints
- `docs/` — especificação, planos, design, `INSTALACAO.md`
- `builds/` — (ignorado pelo git) `.ipa` entregue ao usuário

### Como rodar localmente
- **Protótipo:** `cd prototype && npm ci && npx playwright install webkit` (uma vez); `node serve.mjs 5173` → http://localhost:5173 ; testes: `npx playwright test`.
- **App iOS:** não roda localmente (não há Mac). Fluxo: push no branch `etapa-*` → CI roda testes e build.
  - `gh workflow run ios-test.yml --ref <branch>` / `gh run watch <id>` — testes + prints do simulador (artefato `screenshots`, arquivos `01-relogio.png` etc.).
  - `gh workflow run ios-build.yml --ref <branch>` — artefato `TelaDois-ipa` (`TelaDois.ipa` + `BUILD_INFO.txt`).
- **Instalar no iPhone:** `docs/INSTALACAO.md` (Sideloadly; precisa de cabo USB **de dados**).

## Regras
- O usuário fala português; documentos para ele em português, código em inglês.
- Não há Mac. Nunca rode `xcodebuild`/`xcodegen`/`swift` localmente. O código iOS é verificado **só** pelo GitHub Actions.
- O `.xcodeproj` é gerado a partir de `ios/project.yml` (XcodeGen). Nunca versione o `.xcodeproj`.
- Trabalhar em branch (`etapa-N-...`) e só juntar ao `main` (fast-forward) com `ios-test` **e** `ios-build` verdes no SHA final.
- O `.ipa` entregue ao usuário deve vir de um commit cujas execuções `ios-test` **e** `ios-build` estão verdes.
- Antes de entregar um `.ipa`, inspecione-o com Python isolado (`py -3 -I`, `plistlib` no `Info.plist`; cabeçalho Mach-O arm64 e ausência de `LC_CODE_SIGNATURE`), extraindo numa pasta vazia do scratchpad. O REA não inspeciona artefatos Apple no Windows; continua útil para binários Windows (ex.: driver de monitor virtual). `python3` no PATH pode ser o stub da Microsoft Store — use `py -3`.
- Lógica pura fica no pacote `ios/Packages/TelaDoisCore` (Swift 6, testado com `swift test` no CI). O app só monta telas e liga serviços do iOS.
- Prints do CI saem com o nome do anexo (`01-relogio.png` etc.) no artefato `screenshots`; simulador fixo: iPhone 16 Pro no runtime do SDK.
- Antes de fotografar um estado com animação (ex.: o fade de 1,2 s do escurecer), o teste espera o estado final (ex.: `maxBrightness` do print abaixo de 5%); senão o print sai no meio da animação.
- Testes de UI controlam o app por argumentos de lançamento: `-TDFixedDate <ISO8601>`, `-TDTimeZone <id>`, `-TDFixedTemp <°C>`, `-TDFixedBattery <0…1>`, `-TDDimSeconds <s>`.
- Armadilha dos prints: `XCUIScreen.main.screenshot()` vem em retrato com `imageOrientation`; o export do xcresult ignora isso, por isso o teste redesenha a imagem (`ScreenshotHelpers.swift`).
- Os testes de UI não detectam texto cortado nem tamanho visual errado: **revisar os prints** é parte obrigatória da verificação.
- Zero avisos de compilação (app e testes) com `SWIFT_STRICT_CONCURRENCY: complete`.
- No Windows, **não** edite arquivos com PowerShell `Get-Content`/`Set-Content` (corrompe UTF-8: "Maré" vira "MarÃ©"). Use as ferramentas de edição ou Python.
- Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Decisões técnicas (e por quê)
| Decisão | Motivo |
|---|---|
| Build no GitHub Actions, sem Mac | O usuário só tem Windows; repositório público = minutos de macOS ilimitados |
| `.ipa` sem assinatura + Sideloadly + Apple ID grátis | Evita a conta Apple Developer paga (US$ 99/ano). Custo: reinstalar a cada 7 dias |
| XcodeGen em vez de `.xcodeproj` versionado | Sem Xcode local não dá para editar o projeto; YAML é editável e revisável |
| Xcode, XcodeGen e simulador fixos (iPhone 16 Pro / iOS 18.5) | Sem Mac para reproduzir, atualizações surpresa do runner seriam difíceis de diagnosticar; prints com tamanho estável |
| Pacote `TelaDoisCore` (Swift 6) separado do app (Swift 5 + concorrência estrita) | Lógica pura testada rápido com `swift test`, sem simulador |
| Swift nativo (não Flutter/React Native) | Vídeo de baixa latência (VideoToolbox/Metal) e SDK do Spotify exigem nativo |
| Open-Meteo para clima | WeatherKit exige conta paga |
| Sem notificações de WhatsApp/Instagram | O iOS não permite um app ler notificações/badges de outros apps |
| Relógio com fonte do sistema (SF Pro comprimida, esticada 1,5× na vertical) | Visual nativo sem embutir fonte; reproduz o protótipo aprovado |
| Tela preta + brilho 0 em vez de bloquear o iPhone | Bloqueado, o toque abriria a tela de bloqueio, não o app |
| Segunda tela via usbmuxd (serviço da Apple) + driver de monitor virtual pronto e assinado | Mesmo caminho do Duet Display; driver próprio exigiria assinatura da Microsoft |

## Estado atual (atualizado em 2026-10-18)
**Pronto**
- Etapa 0 — Fundação: repositório, CI, app "Olá" gerado e inspecionado.
- Etapa 1 — Design: protótipo aprovado e publicado, ícone (Canva, opção "celular + monitor"), tokens em `docs/design/tokens.md`.
- Etapa 2 — Modo Descanso nativo: pacote `TelaDoisCore` (`DimState`, `ClockFormatter`, `OpenMeteo`, 13 testes); página do relógio (hora, "QUA., 14", temperatura, anel de bateria); tela preta + brilho 0 após 60 s, tela sempre ligada, acordar com 1 toque (fade só ao escurecer; acordar é instantâneo). Prints `01`–`03` revisados. Plano: `docs/superpowers/plans/2026-10-16-etapa-2-descanso-nativo.md`.
- **Primeira instalação no iPhone em 2026-10-18** (cabo de dados, Sideloadly, build do `9cce6d6`): instalou e abriu. Modo Desenvolvedor e confiança no Apple ID já feitos; renovar a cada 7 dias.

**Pendências que o usuário decidiu deixar para depois**
- Clima: tentar de novo em ~1 min após falha, tempo-limite na primeira localização e checar permissão antes (hoje pode ficar 30 min sem temperatura).
- O teste "relógio ocupa > 50% da altura" mede a caixa do texto, não os dígitos; revisão de prints é a garantia real.

**Bloqueio**
- Nenhum. O cabo de dados chegou; o PC tem iTunes 12.13 64-bit (versão web), iCloud web, Apple Mobile Device Support 20 e Sideloadly.

**Próximos passos**
1. Instalar o `.ipa` do `main` (com o acordar instantâneo) e conferir no iPhone: escurece após 60 s, acende com 1 toque, temperatura aparece.
2. Etapa 3 — Spotify (página de música, mini player).
3. Etapa 4 — túnel USB (app de bandeja C# detecta o iPhone, troca `HELLO`/`PING`).
4. Etapa 5 — Segunda Tela (monitor virtual + FFmpeg + vídeo + desconexão automática).
5. Etapa 6 — acabamento.

Há um projeto Android parecido em outra pasta (`C:\Pedro\Arquivos Pedro\Aplicativo Android`), com sessão e repositório próprios.

## Variáveis de ambiente
Nenhuma é necessária hoje. Credenciais ficam fora do repositório:
- GitHub: `gh auth login` (guardado no cofre do sistema, não em variável).
- Futuro (Etapa 3): `SPOTIFY_CLIENT_ID` e `SPOTIFY_REDIRECT_URI` (Spotify Developer). Nunca versionar valores; usar arquivo local ignorado (`.env`) ou segredo do GitHub Actions.

## Ambiente do Claude Code
Levantado dos arquivos de configuração deste PC em 2026-10-16 (`~/.claude/settings.json`, `~/.claude/plugins/`, `~/.claude/skills/`, `~/.claude.json`, plugins do app Claude Desktop). Não há `.mcp.json` nem `.claude/skills` no projeto.

### Programas no Windows
| Programa | Para quê | Como instalar |
|---|---|---|
| Git | versionamento | https://git-scm.com |
| GitHub CLI (`gh`) | CI, artefatos, PRs | `winget install --id GitHub.cli -e` e depois `gh auth login` |
| Node.js 24 | protótipo, Playwright, REA | https://nodejs.org |
| Python 3 (`py`) | inspecionar `.ipa`, scripts | https://python.org |
| iTunes 64-bit **versão web** + iCloud **versão web** | driver USB do iPhone (Apple Mobile Device Support) | links na seção de requisitos de https://sideloadly.io (não usar Microsoft Store) |
| Sideloadly | instalar o `.ipa` no iPhone | https://sideloadly.io |
| Claude Desktop (opcional) | traz os plugins e conectores da conta automaticamente | https://claude.ai/download |

### Plugins do Claude Code (instalados no CLI, escopo usuário)
| Plugin | Marketplace (origem) | Comandos |
|---|---|---|
| `agent-skills` (0.6.13) | `addy-agent-skills` — github.com/addyosmani/agent-skills | `/plugin marketplace add addyosmani/agent-skills` · `/plugin install agent-skills@addy-agent-skills` |
| `frontend-design` | `claude-plugins-official` — github.com/anthropics/claude-plugins-official | `/plugin marketplace add anthropics/claude-plugins-official` · `/plugin install frontend-design@claude-plugins-official` |
| `hyperframes` (0.8.157) | `hyperframes` — github.com/heygen-com/hyperframes | `/plugin marketplace add heygen-com/hyperframes` · `/plugin install hyperframes@hyperframes` |
| `supabase` (0.1.12) — não usado neste projeto | `claude-plugins-official` | `/plugin install supabase@claude-plugins-official` |

### Plugins vindos da conta (app Claude Desktop)
Aparecem sozinhos ao entrar no Claude Desktop com a mesma conta: **superpowers** 6.1.0 (github.com/obra/superpowers), **ui-ux-pro-max** 2.6.2, **productivity** 1.3.1.
Para tê-los também no CLI puro (os marketplaces já estão registrados neste PC):
- `/plugin marketplace add obra/superpowers` · `/plugin install superpowers@superpowers-dev`
- `/plugin marketplace add nextlevelbuilder/ui-ux-pro-max-skill` · depois `/plugin` para ver o nome exato e instalar o `ui-ux-pro-max`
- `productivity` não tem marketplace registrado neste PC — só pelo Claude Desktop.

### Skills
| Origem | Skills usadas neste projeto |
|---|---|
| Plugins | superpowers (`brainstorming`, `writing-plans`, `subagent-driven-development`, `test-driven-development`, `systematic-debugging`, `verification-before-completion`, `requesting-code-review`), agent-skills (`spec`, `plan`, `ci-cd-and-automation`, `api-and-interface-design`, `performance-optimization`, `security-and-hardening`…), ui-ux-pro-max, frontend-design, hyperframes (vídeo demo, opcional) |
| Pessoais (`~/.claude/skills`) | `reverse-engineer-anything` (usa o MCP `rea`) |
| Projeto (`.claude/skills`) | nenhuma |

- **`reverse-engineer-anything`**: não vale mover para o projeto — é genérica e depende do REA instalado globalmente (`npm install -g rea-agents`); copie a pasta `~/.claude/skills/reverse-engineer-anything` para o outro PC.
- **`graphify`**: o `~/.claude/CLAUDE.md` global manda usar `~/.claude/skills/graphify/SKILL.md`, mas **essa skill não existe neste PC** (só há um pacote npm `graphify@1.0.0`). Reinstalar a skill nos dois PCs antes de usar `/graphify`.

### MCP servers
| Nome | Para quê | Como configurar |
|---|---|---|
| `rea` (stdio, usuário) | engenharia reversa (binários Windows, driver) | `npm install -g rea-agents@6.5.0`; depois `claude mcp add --scope user rea -- node "<pasta do npm -g>\rea-agents\scripts\rea.mjs" mcp` (`npm root -g` mostra a pasta) |
| `apify` (http, usuário) | scraping/pesquisa web (não usado aqui) | `claude mcp add --scope user --transport http apify https://mcp.apify.com/` e autenticar em `/mcp` |
| `playwright` | navegar/printar o protótipo | não está nos arquivos de config deste PC (vem do ambiente do app). Se faltar: `claude mcp add --scope user playwright -- npx @playwright/mcp@latest` |
| Conectores da conta claude.ai | Canva (ícone/artes), Gmail, Google Calendar, Google Drive, Supabase, Claude Docs | vêm com a conta; autorizar em claude.ai → Configurações → Conectores |
| Do plugin `productivity` | Slack, Notion, Asana, Linear, Atlassian, monday, ClickUp | pedem autorização; não usados neste projeto |
| Embutidos do Claude Desktop | navegador integrado, Claude in Chrome, terminal | já vêm no app (Claude in Chrome precisa da extensão no Chrome) |

## Handoff entre computadores
**Antes de sair**
- [ ] Atualizar a seção **"Estado atual"** acima (pronto / em andamento / próximos passos / bloqueios).
- [ ] `git status` limpo: commitar tudo no branch da etapa.
- [ ] `git push` do branch (e do `main`, se mudou).
- [ ] Se havia agentes rodando, esperar terminarem ou anotar no "Estado atual" o último commit e o que falta.

**Ao chegar no outro PC**
- [ ] `git pull` (e `git switch <branch da etapa>` + `git pull`).
- [ ] Ler este `CLAUDE.md` inteiro antes de começar.
- [ ] `gh auth status` (logado no GitHub).
- [ ] Protótipo: `cd prototype && npm ci` se o `package-lock.json` mudou.

O que **não** vai pelo git (fica em cada PC): a memória automática do Claude (`~/.claude/projects/.../memory`), o controle de tarefas dos subagentes (`.superpowers/sdd/`), a pasta `builds/` e os prints locais. Por isso o "Estado atual" é a fonte da verdade.
