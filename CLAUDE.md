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
| Instalação no iPhone | `.ipa` sem assinatura → **xtool** (no Ubuntu do WSL, usando o usbmuxd do Windows) + Apple ID grátis; Sideloadly só como alternativa |
| Protótipo | HTML/CSS/JS puro em `prototype/` + testes **Playwright (WebKit)**, publicado no GitHub Pages |
| PC (Etapas 4–5) | C# / .NET 10 (app de bandeja `TelaDois.exe`, WinForms) + xUnit; Etapa 5: FFmpeg + Virtual Display Driver; túnel pelo usbmuxd |
| Clima | Open-Meteo (grátis, sem chave) |

### Estrutura de pastas
- `ios/TelaDois/` — telas e serviços do app (SwiftUI)
- `ios/TelaDoisUITests/` — testes de UI (XCUITest) que geram os prints
- `ios/Packages/TelaDoisCore/` — lógica pura testável (`DimState`, `ClockFormatter`, `OpenMeteo`, `LinkProtocol`, `LinkSessionState`)
- `ios/TelaDois/UsbLink.swift` — escuta o PC em `127.0.0.1:7100` (túnel do usbmuxd)
- `ios/TelaDois/Assets.xcassets/` — ícone do app
- `prototype/` — protótipo do Modo Descanso (`index.html`, `styles.css`, `app.js`, `serve.mjs`, `tests/`)
- `windows/` — solução .NET (`TelaDois.sln`, SDK fixo em `global.json`, avisos = erros em `Directory.Build.props`)
  - `src/TelaDois.Core/Link/` — protocolo PC ↔ iPhone e `LinkSession` (HELLO, PING/PONG, BYE, tempo-limite)
  - `src/TelaDois.Core/Usbmux/` — plist e cliente do usbmuxd (`Listen`, `Connect`)
  - `src/TelaDois.Core/UsbTunnelService.cs` — espera o iPhone, reconecta, acompanha a sessão
  - `src/TelaDois.Tray/` — app de bandeja (`TelaDois.exe`)
  - `tests/TelaDois.Core.Tests/` — xUnit com usbmuxd e iPhone falsos (`Fakes/`)
- `.github/workflows/` — `ios-test.yml`, `ios-build.yml`, `windows-test.yml`, `prototype-test.yml`, `pages.yml`
- `.github/scripts/` — instalar XcodeGen, escolher simulador, renomear prints
- `docs/` — especificação, planos, design, `INSTALACAO.md`
- `builds/` — (ignorado pelo git) `.ipa` entregue ao usuário

### Como rodar localmente
- **Protótipo:** `cd prototype && npm ci && npx playwright install webkit` (uma vez); `node serve.mjs 5173` → http://localhost:5173 ; testes: `npx playwright test`.
- **App iOS:** não roda localmente (não há Mac). Fluxo: push no branch `etapa-*` → CI roda testes e build.
  - `gh workflow run ios-test.yml --ref <branch>` / `gh run watch <id>` — testes + prints do simulador (artefato `screenshots`, arquivos `01-relogio.png` etc.).
  - `gh workflow run ios-build.yml --ref <branch>` — artefato `TelaDois-ipa` (`TelaDois.ipa` + `BUILD_INFO.txt`).
- **App do PC:** `dotnet test windows/TelaDois.sln` (rápido, local); publicar com `dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows` → `builds/windows/TelaDois.exe`. Registro em `%LOCALAPPDATA%\TelaDois\tela-dois.log` (é ele que mostra se o túnel abriu).
- **Instalar no iPhone:** `wsl -d Ubuntu -- bash -lc 'xtool install --usb /mnt/d/Downloads/Teladois/builds/<pasta>/TelaDois.ipa'` (o `bash -lc` carrega o `USBMUXD_SOCKET_ADDRESS`; sem ele o xtool fica em "Waiting for device"). Guia: `docs/INSTALACAO.md`.
- **Testar o túnel sem o app do PC:** `windows/tools/probe-usbmux.ps1 -DeviceId <n> -Ports 62078,7100` — pede ao usbmuxd um `Connect` por porta; código 0 = aceito, 2 = DeviceID errado (ele muda a cada vez que o cabo é reconectado; o script lista o atual), 3 = ninguém escuta nessa porta. A 62078 é do próprio iOS e sempre aceita: serve de controle.

## graphify (navegar gastando menos tokens)
O projeto tem um grafo de conhecimento em `graphify-out/` (código pela AST + spec, CLAUDE.md, design, workflows). Neste repositório, **`graphify` = `uv tool run --from graphifyy python -m graphify`** (funciona nos dois PCs; o `graphify.exe` é bloqueado pelo Windows no PC `C:\Pedro`).
- Antes do primeiro grep, busca ou leitura de código para uma pergunta sobre o projeto, rode `graphify query "<pergunta>"` (use `--budget 1500` se vier truncado). Leia arquivos crus só nas linhas que o grafo ou uma leitura anterior já localizou. Para relações use `graphify path "<A>" "<B>"`; para um conceito, `graphify explain "<conceito>"`. Coloque esta regra em todo prompt de subagente que explore código.
- `graphify-out/GRAPH_REPORT.md` só para visão geral da arquitetura ou quando query/path/explain não bastarem.
- Depois de mudar código, rode `graphify update .` (só AST, sem custo). Documentos mudaram muito → `/graphify . --update`.
- `.claude/settings.json` tem os hooks `PreToolUse` do graphify (Bash/Grep/Read/Glob): eles lembram de consultar o grafo e mostram os nós relevantes. Não remova.
- `graphify-out/graph.json` é reescrito inteiro a cada build: diff grande nele é esperado.
- O que fica fora do grafo está em `.graphifyignore` (planos antigos, ícones, `package-lock.json`).

## Regras
- O usuário fala português; documentos para ele em português, código em inglês.
- Não há Mac. Nunca rode `xcodebuild`/`xcodegen`/`swift` localmente. O código iOS é verificado **só** pelo GitHub Actions.
- O `.xcodeproj` é gerado a partir de `ios/project.yml` (XcodeGen). Nunca versione o `.xcodeproj`.
- Trabalhar em branch (`etapa-N-...`) e só juntar ao `main` (fast-forward) com `ios-test`, `ios-build` **e** `windows-test` verdes no SHA final.
- O protocolo PC ↔ iPhone existe em Swift (`TelaDoisCore/LinkProtocol.swift`) e em C# (`TelaDois.Core/Link/`). Mudou um lado, mude o outro e os **bytes de referência** nos dois testes (`HELLO(2556,1179)` = `010000000A0001000009FC0000049B`; `PING(1234567890123)` = `05000000080000011F71FB04CB`).
- Antes de concluir que algo "não funciona no iPhone", confirme que a versão nova **foi instalada** (o instalador terminou com sucesso). Na Etapa 4 o túnel parecia quebrado, mas era a versão antiga aberta: o Sideloadly tinha falhado no login.
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
| App escuta só em `127.0.0.1:7100` | O túnel do usbmuxd chega no loopback do iPhone; nada fica exposto no Wi-Fi e não há pedido de "rede local". Testado no aparelho em 2026-10-18 |
| xtool (no Ubuntu do WSL) em vez do Sideloadly | Em 2026-10-18 o login do Sideloadly na Apple falhou (404). O xtool roda no Linux e usa o usbmuxd do Windows pela rede espelhada do WSL (`networkingMode=mirrored`), sem passar o USB para o Linux. Instalar pelo xtool revogou o certificado antigo: apps instalados pelo Sideloadly precisam ser reinstalados |

## Estado atual (atualizado em 2026-10-18)
**Pronto**
- Etapa 0 — Fundação: repositório, CI, app "Olá" gerado e inspecionado.
- Etapa 1 — Design: protótipo aprovado e publicado, ícone (Canva, opção "celular + monitor"), tokens em `docs/design/tokens.md`.
- Etapa 2 — Modo Descanso nativo: pacote `TelaDoisCore` (`DimState`, `ClockFormatter`, `OpenMeteo`, 13 testes); página do relógio (hora, "QUA., 14", temperatura, anel de bateria); tela preta + brilho 0 após 60 s, tela sempre ligada, acordar com 1 toque (fade só ao escurecer; acordar é instantâneo). Prints `01`–`03` revisados. Plano: `docs/superpowers/plans/2026-10-16-etapa-2-descanso-nativo.md`.
- **Primeira instalação no iPhone em 2026-10-18** (cabo de dados, Sideloadly, build do `9cce6d6`): instalou e abriu. Modo Desenvolvedor e confiança no Apple ID já feitos; renovar a cada 7 dias.
- Etapa 4 — Túnel USB (2026-10-18): app do iPhone escuta em `127.0.0.1:7100`, manda `HELLO`, responde `PING`; `TelaDois.exe` na bandeja detecta o iPhone, abre o túnel pelo usbmuxd, mede a latência e reconecta sozinho. **Testado no iPhone pelo cabo** (build 24 do `45528d9`, instalado pelo xtool): "PC conectado" no iPhone, `conectado · 2556×1179 · poucos ms` na bandeja, reconexão ao sair/voltar do app e ao tirar/colocar o cabo. 33 testes no PC + 26 no pacote + 5 de UI. Plano: `docs/superpowers/plans/2026-10-18-etapa-4-tunel-usb.md`.

**Pendências que o usuário decidiu deixar para depois**
- Etapa 3 (Spotify): adiada em 2026-10-18 para chegar antes na Segunda Tela.
- Clima: tentar de novo em ~1 min após falha, tempo-limite na primeira localização e checar permissão antes (hoje pode ficar 30 min sem temperatura).
- O teste "relógio ocupa > 50% da altura" mede a caixa do texto, não os dígitos; revisão de prints é a garantia real.

**Bloqueio**
- Nenhum. O cabo de dados chegou; o PC tem iTunes 12.13 64-bit (versão web), iCloud web, Apple Mobile Device Support 20 e Sideloadly.
- PC `C:\Pedro\Arquivos Pedro\Aplicativo celular` (2026-10-19): graphify, uv 0.13.1 e .NET 10 SDK instalados (33 testes do PC passando). WSL instalado, `.wslconfig` com `networkingMode=mirrored` e o xtool 1.22.2 já baixado e conferido em `C:\Users\Contabil 01\Downloads\xtool\xtool-x86_64.AppImage`. **Falta (depois de reiniciar uma vez):** abrir o Ubuntu e criar o usuário Linux; rodar `wsl -d Ubuntu -- bash "/mnt/c/Pedro/Arquivos Pedro/Aplicativo celular/tools/wsl/instalar-xtool.sh" "/mnt/c/Users/Contabil 01/Downloads/xtool/xtool-x86_64.AppImage"`; `wsl -d Ubuntu -- bash -lc 'xtool auth login'`. Até lá, instalar no iPhone por este PC só pelo Sideloadly.

**Próximos passos**
1. Etapa 5 — Segunda Tela (monitor virtual + FFmpeg + vídeo + desconexão automática). Já levantado: o Virtual Display Driver (VirtualDrivers/Virtual-Display-Driver, release 25.7.23) aceita comandos UTF-16 no pipe `\\.\pipe\MTTVirtualDisplayPipe` (`SETDISPLAYCOUNT n`, `RELOAD_DRIVER`, `PING`); resoluções em `C:\VirtualDisplayDriver\vdd_settings.xml`. O pipe aceita qualquer usuário do PC (`D:(A;;GA;;;WD)`) — anotar na revisão de segurança. GPU do PC: AMD Radeon → `h264_amf`.
2. Etapa 3 — Spotify (página de música, mini player).
3. Etapa 6 — acabamento.

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
| uv | instalar o `graphifyy` como ferramenta isolada | `winget install --id astral-sh.uv -e` |
| Claude Code CLI (`claude`) | instalar plugins (`claude plugin install …`) | `npm install -g @anthropic-ai/claude-code` |
| iTunes 64-bit **versão web** + iCloud **versão web** | driver USB do iPhone (Apple Mobile Device Support) | links na seção de requisitos de https://sideloadly.io (não usar Microsoft Store) |
| .NET 10 SDK | app de bandeja e testes do PC | `winget install --id Microsoft.DotNet.SDK.10 -e` |
| WSL + Ubuntu | rodar o xtool | `wsl --install -d Ubuntu`; em `%USERPROFILE%\.wslconfig`: `[wsl2]` `networkingMode=mirrored` (o Ubuntu enxerga o `127.0.0.1:27015` do Windows) |
| xtool 1.22.2 (no Ubuntu) | instalar o `.ipa` no iPhone | Script `tools/wsl/instalar-xtool.sh` (confere o SHA-256, instala `zip`/`unzip`, extrai o AppImage em `/opt/xtool`, cria `/usr/local/bin/xtool` e `/etc/profile.d/xtool-usbmuxd.sh` com `USBMUXD_SOCKET_ADDRESS=127.0.0.1:27015`): `wsl -d Ubuntu -- bash "/mnt/<repo>/tools/wsl/instalar-xtool.sh" ["/mnt/<AppImage já baixado>"]`; depois `wsl -d Ubuntu -- bash -lc 'xtool auth login'` (Apple ID; o próprio usuário digita) |
| Sideloadly (alternativa) | instalar o `.ipa` no iPhone | https://sideloadly.io |
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
| Pessoais (`~/.claude/skills`) | `reverse-engineer-anything` (usa o MCP `rea`), `graphify` (instalada pelo `graphify install`, ver abaixo) |
| Projeto (`.claude/skills`) | `encerrar` — comando **`/encerrar`**: rotina de fim de sessão (atualiza este CLAUDE.md, verifica o ambiente de verdade, confere segredos, commit e push, lista o que falta no outro PC). Vai pelo Git, então funciona nos dois PCs |

- **`reverse-engineer-anything`**: não vale mover para o projeto — é genérica e depende do REA instalado globalmente (`npm install -g rea-agents`); copie a pasta `~/.claude/skills/reverse-engineer-anything` para o outro PC.
- **`graphify`** (mapa do código em grafo para navegar no projeto gastando menos tokens; comando `/graphify .`): o pacote certo é o **`graphifyy`** (dois "y") do PyPI, repositório oficial https://github.com/Graphify-Labs/graphify. O pacote npm `graphify` **não tem relação** com ele, não use. Instalação (uma vez por PC): `winget install --id astral-sh.uv -e` → terminal novo → `uv tool install graphifyy` → `uv tool update-shell` → `graphify install --platform claude`. Isso cria `~/.claude/skills/graphify/` e o `~/.claude/CLAUDE.md` global que registra a skill (sem hooks; hooks só com `--project`). Instalado em 2026-10-18 (versão 0.9.92) no PC onde o projeto fica em `D:\Downloads\Teladois` e em 2026-10-19 no PC onde fica em `C:\Pedro\Arquivos Pedro\Aplicativo celular`. Neste segundo PC o Windows (Controle de Aplicativos) **bloqueia o `graphify.exe`** criado pelo uv (não é assinado); a skill funciona assim mesmo, porque chama `python -m graphify` pelo Python do `uv tool run --from graphifyy`. Para rodar à mão: `uv tool run --from graphifyy python -m graphify <comando>`. Não desligue a proteção do Windows.

### Armadilhas de instalação já vistas
- `uv` antigo (0.11.x) travava ao abrir ("capacity overflow"): `winget upgrade --id astral-sh.uv -e --force`.
- `dotnet test` com `NU1100` ("Não foi possível resolver xunit…"): o NuGet do usuário estava sem nenhuma fonte. Corrigir com `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org`.

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
**Antes de sair** — rode **`/encerrar`**: ele faz os itens abaixo e mostra o resumo.
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
