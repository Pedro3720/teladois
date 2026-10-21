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
- `ios/Packages/TelaDoisCore/` — lógica pura testável (`DimState`, `ClockFormatter`, `OpenMeteo`, `LinkProtocol`, `LinkSessionState`, `H264Parser`; vídeo de teste em `Tests/TelaDoisCoreTests/Fixtures/bars-64x64.h264`)
- `ios/TelaDois/UsbLink.swift` — escuta o PC em `127.0.0.1:7100` (túnel do usbmuxd); `CONFIG` liga a Segunda Tela, `VIDEO` vai para o renderer
- `ios/TelaDois/VideoRenderer.swift` + `SecondScreenView.swift` — decodificam o H.264 (`AVSampleBufferDisplayLayer`) e mostram em tela cheia
- `ios/TelaDois/Assets.xcassets/` — ícone do app
- `prototype/` — protótipo do Modo Descanso (`index.html`, `styles.css`, `app.js`, `serve.mjs`, `tests/`)
- `windows/` — solução .NET (`TelaDois.sln`, SDK fixo em `global.json`, avisos = erros em `Directory.Build.props`)
  - `src/TelaDois.Core/Link/` — protocolo PC ↔ iPhone e `LinkSession` (HELLO, PING/PONG, BYE, tempo-limite)
  - `src/TelaDois.Core/Usbmux/` — plist e cliente do usbmuxd (`Listen`, `Connect`)
  - `src/TelaDois.Core/UsbTunnelService.cs` — espera o iPhone, reconecta, acompanha a sessão
  - `src/TelaDois.Core/Video/` — captura e compressão (Etapa 5a): `EncoderProbe` (escolhe a compressão da placa), `EncoderProfiles`, `FfmpegArguments`, `FfmpegCapture` (processo do FFmpeg, reinício 1/2/5 s), `AccessUnitReader` (divide o H.264 em imagens), `FrameQueue` (descarta acima de 100 ms), `DisplayCatalog` (monitores ↔ índice do `ddagrab`), `VideoFeedFactory`
  - `src/TelaDois.Tray/` — app de bandeja (`TelaDois.exe`); menu "Monitor transmitido", escolha salva em `%LOCALAPPDATA%\TelaDois\settings.json`
  - `tests/TelaDois.Core.Tests/` — xUnit com usbmuxd e iPhone falsos (`Fakes/`)
- `.github/workflows/` — `ios-test.yml`, `ios-build.yml`, `windows-test.yml`, `prototype-test.yml`, `pages.yml`
- `.github/scripts/` — instalar XcodeGen, escolher simulador, renomear prints
- `tools/` — scripts de instalação: `wsl/instalar-xtool.sh`; `vdd/instalar-vdd.ps1` + `vdd/vdd_settings.xml` (Virtual Display Driver, tarefas `\TelaDois\` que ligam/desligam o dispositivo; rodar como administrador uma vez por PC e de novo quando o script mudar)
- `windows/tools/TelaDois.LatencyBench/` — bancada de atraso da Etapa 5d (console, fora do app; testes em `windows/tests/TelaDois.LatencyBench.Tests/`); resultados em `docs/medicoes/`
- `windows/src/TelaDois.Core/Display/` — monitor virtual (`VirtualDisplay`, `Win32DisplayApi`, `DriverPipe`, `ScheduledTaskSwitch`), `MonitorMode` (liga/desliga), `KeyboardLayout`
- `docs/` — especificação, planos, design, `INSTALACAO.md`
- `builds/` — (ignorado pelo git) `.ipa` entregue ao usuário

### Como rodar localmente
- **Protótipo:** `cd prototype && npm ci && npx playwright install webkit` (uma vez); `node serve.mjs 5173` → http://localhost:5173 ; testes: `npx playwright test`.
- **App iOS:** não roda localmente (não há Mac). Fluxo: push no branch `etapa-*` → CI roda testes e build.
  - `gh workflow run ios-test.yml --ref <branch>` / `gh run watch <id>` — testes + prints do simulador (artefato `screenshots`, arquivos `01-relogio.png` etc.).
  - `gh workflow run ios-build.yml --ref <branch>` — artefato `TelaDois-ipa` (`TelaDois.ipa` + `BUILD_INFO.txt`).
- **App do PC:** `dotnet test windows/TelaDois.sln` (rápido, local); publicar com `dotnet publish windows/src/TelaDois.Tray/TelaDois.Tray.csproj -c Release -o builds/windows` → `builds/windows/TelaDois.exe`. Registro em `%LOCALAPPDATA%\TelaDois\tela-dois.log` (é ele que mostra se o túnel abriu).
- **Instalar no iPhone:** `wsl -d Ubuntu --exec bash -lc 'xtool install --usb /mnt/d/Downloads/Teladois/builds/<pasta>/TelaDois.ipa'` (o `bash -lc` carrega o `USBMUXD_SOCKET_ADDRESS`; sem ele o xtool fica em "Waiting for device"). Se der `appExtractionFailed`, copie o `.ipa` para a pasta do Ubuntu e instale de lá: `wsl -d Ubuntu --exec bash -lc 'cp "/mnt/<…>/TelaDois.ipa" ~/TelaDois.ipa'` e `wsl -d Ubuntu --cd ~ --exec bash -lc 'xtool install --usb ~/TelaDois.ipa'`. Guia: `docs/INSTALACAO.md`.
  - **Trocar de PC revoga o certificado:** o certificado de desenvolvimento do Apple ID fica no PC que o criou. Ao instalar pelo outro PC o xtool pergunta `The following certificates must be revoked … Continue? (yes/no)`: só o usuário responde (`yes` = o app instalado pelo outro PC deixa de abrir até reinstalar). Por isso rode a instalação numa aba do Terminal, não em segundo plano.
- **Segunda Tela (Etapa 5a):** precisa do FFmpeg no PATH (`winget install Gyan.FFmpeg`). Com o app aberto no iPhone e o cabo plugado, o `TelaDois.exe` transmite o monitor escolhido no menu da bandeja (padrão: o primeiro não principal). O registro mostra `compressão escolhida: <perfil>` e `transmitindo \\.\DISPLAYn (LxA) com <perfil>`.
- **Usar o app do PC de verdade:** abrir pelo atalho `TelaDois` da Área de Trabalho ou pelo Explorer (`Start-Process explorer.exe -ArgumentList '"<caminho>\TelaDois.exe"'`), nunca com `Start-Process` direto pelo Claude (fecharia junto com ele).
- **Bancada de atraso (5d):** só com o PC parado e o TelaDois fechado (abre janelas e liga o monitor virtual): `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- [--variants base,…] [--scenarios parado,rolagem,video] [--monitor \.\DISPLAYn] [--monitor-size 1460x674] --out builds/bancada-<nome>`. Relatório em `builds/bancada-*/relatorio.txt` + `resultados.csv` + `*-rolagem.png`.
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
- O usuário fala português; **toda resposta no chat em português** (inclusive resumos de subagentes, recaps e perguntas), documentos para ele em português, código em inglês.
- Não há Mac. Nunca rode `xcodebuild`/`xcodegen`/`swift` localmente. O código iOS é verificado **só** pelo GitHub Actions.
- O `.xcodeproj` é gerado a partir de `ios/project.yml` (XcodeGen). Nunca versione o `.xcodeproj`.
- Trabalhar em branch (`etapa-N-...`) e só juntar ao `main` (fast-forward) com `ios-test`, `ios-build` **e** `windows-test` verdes no SHA final.
- O protocolo PC ↔ iPhone existe em Swift (`TelaDoisCore/LinkProtocol.swift`) e em C# (`TelaDois.Core/Link/`). Mudou um lado, mude o outro e os **bytes de referência** nos dois testes (`HELLO(2556,1179)` = `010000000A0001000009FC0000049B`; `PING(1234567890123)` = `05000000080000011F71FB04CB`; `CONFIG(1366,768,60)` = `020000000A0000055600000300003C`; `VIDEO(completa, 1000 µs, 0000000109F0)` = `030000000F0100000000000003E80000000109F0`; `VIDEO_END` = `0800000000`; `STATS`(1..9) = `0900000024000000010000000200000003000000040000000500000006000000070000000800000009`; `SCREEN(1)` = `0A0000000101`, `SCREEN(0)` = `0A0000000100`).
- Espera do CI: não confie no código de saída do `gh run watch` (deu "falha" com a execução verde no PC de casa); confirme com `gh run view <id> --json status,conclusion`.
- Um push com mudança em `ios/**` cancela o `ios-test` que estiver rodando no mesmo branch (`cancel-in-progress`). Para ver o "vermelho" do TDD no CI, espere a execução anterior terminar antes de enviar a implementação.
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

| Vídeo: FFmpeg `ddagrab` → H.264 (`h264_amf` → `h264_qsv` → `h264_nvenc` → `h264_mf` → `libx264`), sem B-frames, imagem completa a cada 2 s, delimitador AUD | Compressão por hardware de qualquer placa sem SDK próprio; `EncoderProbe` testa na 1ª sessão e guarda a que funcionou. AUD permite dividir o fluxo em imagens sem decodificar |
| Fila do vídeo limitada por **tempo** (descarta quando a imagem mais antiga passa de 100 ms, até a próxima completa) | Decisão do usuário em 2026-10-19: a fila de 3 imagens travava até 2 s em rajadas normais; por tempo, rajadas passam e atraso nunca acumula |
| PC manda `PING` logo depois do `HELLO`, antes de abrir o vídeo | A sondagem da compressão leva segundos e o iPhone desconecta após 3 s sem notícia (era o "túnel abre e cai" do primeiro teste) |
| Etapa 5b: **Virtual Display Driver** (VirtualDrivers, assinado) para o 3º monitor estendido | Decisão do usuário em 2026-10-19 (opção A). Aceita a resolução exata do iPhone; Parsec VDD fica como reserva; adaptador HDMI fantasma descartado (sem 2556×1179, não some ao desligar) |
| O monitor virtual fica **sempre instalado** (`count` 1); ligar/desligar = pôr/tirar do desktop com `ChangeDisplaySettingsEx`, atrás de `IDisplayApi` | Lido no código do driver 25.7.23 (`Driver.cpp`, linhas 1226 e 1543): `SETDISPLAYCOUNT 0` grava 0 e reinicia o driver, mas ao carregar `count` 0 vira 1 — o monitor nunca some pelo pipe. Pôr/tirar do desktop não precisa de administrador e o Windows devolve as janelas. O pipe só é usado para `PING` |
| Instalador `tools/vdd/instalar-vdd.ps1` usa só `VirtualDisplayDriver-x86.Driver.Only.zip` (132 KB, SHA-256 `e2421069…3b3a`) e cria o dispositivo `Root\MttVDD` pela SetupAPI | O VDD Control (71 MB) é uma interface gráfica que o TelaDois não usa; o pacote só do driver é auditável e instala por linha de comando |
| 5b sai do `etapa-5a-video`, sem juntar a 5a ao `main` antes | Decisão do usuário em 2026-10-19: seguir para a 5b primeiro; o teste da 5a no PC AMD entra no teste final da 5b (Task 10 do plano) |
| PC reinsere SPS/PPS antes de toda IDR (`AccessUnitReader`, correção I3) | Medido em 2026-10-19 no PC AMD: `h264_amf` manda SPS/PPS só na 1ª IDR (1 de 3); se a fila descartar a 1ª imagem o iPhone ficaria preto. `-header_spacing` resolveria só o AMF; no leitor vale para qualquer compressão |
| `CONFIG` só junto com a 1ª imagem (I1) e `VIDEO_END` (tipo 8) para desligar sem desconectar | Uma captura que falha logo deixava o iPhone preto na Segunda Tela; o atalho precisa voltar o iPhone ao relógio com o cabo ligado |
| Atalho **Win+Shift+]** para ligar/desligar o modo monitor | Decisão do usuário em 2026-10-19: "]" sozinha impediria digitar "]"/"}" em todos os programas |
| Ligar o monitor virtual **gravando no registro** (`CDS_UPDATEREGISTRY`) | Medido em 2026-10-20: sem gravar, o driver 25.7.23 recusa (`DISP_CHANGE_FAILED`) com o monitor fora do desktop |
| Vídeo sempre com largura e altura **pares** (corte de 1 linha) | A altura do iPhone 15/16 (1179) é ímpar; `libx264` e `h264_mf` recusam (medido em 2026-10-20) |
| Etapa 5c: monitor **desativado** quando não usado, por **tarefas agendadas** `\TelaDois\` (SYSTEM, liberadas ao grupo Usuários) | Decisão do usuário em 2026-10-20: só desativar o dispositivo tira o "3" de Configurações; precisa de administrador, então o instalador cria as tarefas uma vez (recusados: serviço sempre rodando; trocar para o Parsec VDD) |
| Tela preta no iPhone por **Win+Shift+[** + mensagem `SCREEN` (tipo 10, nos dois sentidos) | Decisão do usuário em 2026-10-20: apagar só o iPhone com o monitor funcionando; 1 toque acende (o iPhone avisa o PC) |
| Ligar/desligar o dispositivo do monitor virtual **um de cada vez** (`SemaphoreSlim` no `VirtualDisplay`); a bandeja só abre o túnel depois do desligar de abrir | Revisão da 5c em 2026-10-21: o descarte de um feed (FFmpeg, `Detach`, `schtasks`, espera de 5 s) pode passar do `RetireLimit`; sem a fila, o ligar seguinte achava o monitor ainda listado, não disparava a tarefa e o desligar atrasado tirava o monitor do feed novo |
| Medidores `STATS` (tipo 9) + linha `vídeo/s` no registro, antes de mexer no atraso | 2026-10-20: mostraram que cabo e iPhone não são o gargalo (fila/escrita 1 ms, RTT 3 ms); **Wi-Fi descartado** (mais atraso e oscilação) |
| Tarefa "Ligar monitor virtual" com `Enable-PnpDevice` (o "Desligar" segue com `pnputil`) | Medido em 2026-10-21 nos dois PCs: depois de reiniciar, `pnputil /enable-device` falha com 1167 e `Enable-PnpDevice` funciona (`tools/vdd/diagnostico-1167.ps1`) |
| Processos para uso real abertos pelo **`explorer.exe`** (atalho na Área de Trabalho), nunca pelas ferramentas do Claude | O Claude Desktop é MSIX: o que ele abre morre quando ele fecha e vê o `%LOCALAPPDATA%` virtualizado (2026-10-21, o monitor ficou ligado e a tela parou) |
| Etapa 5d: medir com a **bancada** `TelaDois.LatencyBench` (marcador codificado + linha real do FFmpeg) | Decisão do usuário em 2026-10-21: números repetíveis no PC antes de mexer; a foto do cronômetro só confirma no fim |
| Intel: `qsv-hwmap` com **`-preset veryfast`** | Bancada 2026-10-21: p95 da rolagem 250 → 65 ms com a mesma nitidez; `low_power`, CBR, intra-refresh, GOP 4 s e `dup_frames=0` pioraram |
| Monitor do iPhone em **escala 100%** com resolução **÷ 1,75** (2556×1179 → 1460×674) | Decisão do usuário em 2026-10-21: o GraphOn GO-Global (32 bits, sem DPI por monitor) ficava preso no monitor 1 com escalas misturadas; o texto fica do tamanho de antes e cada imagem tem ~1/3 dos pixels (rolagem 34 → 25 ms) |
| Moldura `TD <tamanho>` antes de cada pacote no stdout do FFmpeg (`-stats_mux_pre`), sem contêiner | 2026-10-21: o `AccessUnitReader` esperava o AUD da imagem seguinte (+16,7 ms). FLV/NUT/MKV forçam o global header e mudariam o fluxo; a ordem linha → pacote foi conferida no código do FFmpeg 9.0 |

## Estado atual (atualizado em 2026-10-21, PC do trabalho)
**Pronto**
- Etapa 0 — Fundação: repositório, CI, app "Olá" gerado e inspecionado.
- Etapa 1 — Design: protótipo aprovado e publicado, ícone (Canva, opção "celular + monitor"), tokens em `docs/design/tokens.md`.
- Etapa 2 — Modo Descanso nativo: pacote `TelaDoisCore` (`DimState`, `ClockFormatter`, `OpenMeteo`); página do relógio (hora, "QUA., 14", temperatura, anel de bateria); tela preta + brilho 0 após 60 s, tela sempre ligada, acordar com 1 toque. Prints `01`–`03` revisados. Plano: `docs/superpowers/plans/2026-10-16-etapa-2-descanso-nativo.md`.
- Etapa 4 — Túnel USB (2026-10-18): app do iPhone escuta em `127.0.0.1:7100`, `HELLO`/`PING`; `TelaDois.exe` na bandeja detecta o iPhone, abre o túnel pelo usbmuxd e reconecta sozinho. Testado no iPhone. Plano: `docs/superpowers/plans/2026-10-18-etapa-4-tunel-usb.md`.
- **Etapa 5a — Vídeo pelo cabo (duplicar)** e **Etapa 5b — Monitor estendido + Win+Shift+]**: código pronto e revisado no branch **`etapa-5b-monitor-virtual`** (nada foi para o `main`). Testado no iPhone em 2026-10-20 no PC do trabalho (Intel, QSV). Specs `docs/superpowers/specs/2026-10-19-etapa-5a-…` e `…-5b-monitor-virtual-design.md`.
  - Achados no driver real (já corrigidos): `PING` do pipe termina em NUL (`"PING\0"` UTF-16) e o driver às vezes fecha sem responder (até 3 tentativas); ligar sem gravar no registro falha (`DISP_CHANGE_FAILED`); altura ímpar (1179) é cortada para par em todos os perfis; o nome `\\.\DISPLAYn` do monitor virtual muda a cada vez (sempre `Find()`); adaptador = `Virtual Display Driver`.
- **Etapa 5c — monitor some do Windows quando desligado + tela preta (Win+Shift+[)**: Tasks 1–8 e revisão final feitas (2026-10-21). O dispositivo `Root\MttVDD` fica desativado e o TelaDois o ativa só enquanto estende, pelas tarefas `\TelaDois\Ligar monitor virtual` / `\TelaDois\Desligar monitor virtual` (SYSTEM, liberadas ao grupo Usuários). Win+Shift+[ manda `SCREEN` e o iPhone apaga; 1 toque acende. **Usado no dia a dia no PC do trabalho desde 2026-10-21** (iPhone com o 0.5.0, build do `a63bed0`, instalado por este PC). Falta a Task 9 formal (abaixo).
  - **Erro 1167 resolvido (2026-10-21):** depois de reiniciar, `pnputil /enable-device` dava 1167 nos dois PCs; `diagnostico-1167.ps1` mostrou que `Enable-PnpDevice` funciona. A tarefa "Ligar" agora roda `Enable-PnpDevice -InstanceId 'ROOT\DISPLAY\0000'` (o "Desligar" continua com `pnputil /disable-device`). O instalador aceita os códigos 0/3010/259 do `pnputil`. Ligar leva ~3 s (53 s na 1ª vez depois de ligar o PC); o TelaDois espera até 20 s.

**Em andamento — Etapa 5d (atraso e engasgos)**, mesmo branch. Spec `docs/superpowers/specs/2026-10-21-etapa-5d-atraso-e-engasgos-design.md`, plano `docs/superpowers/plans/2026-10-21-etapa-5d-atraso-e-engasgos.md`, medições em `docs/medicoes/2026-10-21-bancada-5d.md`, controle dos subagentes em `.superpowers/sdd/progress.md` (só no PC do trabalho; resumo aqui).
- Decisões do usuário: PC principal = **trabalho (Intel QSV)**, AMD só precisa continuar funcionando; o som continua no PC; pode ceder um pouco de nitidez e resolução; medir com bancada automática e confirmar no fim com foto do cronômetro.
- **Feito:**
  - Tasks 1–3: `EncodeSettings` no Core e a **bancada** `windows/tools/TelaDois.LatencyBench` (marcador 64×64 que muda de código a cada imagem, captura com a linha real do FFmpeg, mede o atraso imagem a imagem; cenários parado/rolagem/vídeo).
  - Task 4: matriz 1 (2556×1179) → `qsv-hwmap` com **`-preset veryfast`** (corta o p95 de ~250 para ~65 ms; `f73161d`).
  - **Task 3b — monitor do iPhone em 100%:** o GraphOn GO-Global (programa de trabalho do usuário, 32 bits, sem DPI por monitor) ficava preso no monitor 1 com escalas misturadas (iPhone em 175%). O monitor virtual agora liga em **resolução do iPhone ÷ 1,75 (par)**, ex. 2556×1179 → **1460×674**, em escala 100%; o usuário confirmou que o GraphOn anda por todas as telas. `vdd_settings.xml` ganhou 5 modos; driver sem eles → liga em 2556×1179 e mostra balão "rode o instalador"; escala ≠ 100% → balão uma vez. Revisada e aprovada (`cbf449d`, `f4b314a`, `fb3d9b6`).
  - **Rodada 2 da bancada (1460×674, 2026-10-21):** rolagem 25/42 ms, vídeo 25/37 ms (mediana/p95), 0 buracos > 50 ms, 60 img/s → **cumpre as metas**. Parado (mouse parado que começa a mexer) continua **~280 ms**: não é a compressão (x264 243, mf 362) e a 30/s cai para ~30 ms → é o pipeline do FFmpeg a 60/s com a tela parada.
  - **Task 5 A+B (revisadas e aprovadas):** (A) 5 variantes de diagnóstico do parado na bancada (`passthrough`, `vfr`, `nobuffer`, `graph`, `grab120`; `07eca7c`); (B) o `AccessUnitReader` entrega cada imagem no último byte: o FFmpeg escreve `TD <tamanho>\n` antes de cada pacote no próprio stdout (`-flush_packets 1 -stats_mux_pre pipe:1 -stats_mux_pre_fmt "TD {size}"`, exige FFmpeg ≥ 6), tirando ~16 ms de tudo; o payload para o iPhone não muda (`c7414d1`). Último commit: `f7b9564`, `windows-test` verde, 526 testes.
- **Em uso provisório:** `builds/windows-5d-100/TelaDois.exe` (Task 3b, **sem** a Task 5B), aberto pelo atalho `TelaDois` da Área de Trabalho deste PC.
- **Falta (ordem):**
  1. Rodar com o PC parado (~8 min, sair do TelaDois antes): `dotnet run --project windows/tools/TelaDois.LatencyBench -c Release -- --scenarios parado --variants base,passthrough,vfr,nobuffer,graph,grab120 --out builds/bancada-parado-1` e anotar no doc de medições.
  2. Aplicar no app a variante que tirar o parado (vale para todos os perfis, inclusive AMD) + os 5 menores da revisão da Task 5: o `FfmpegCapture` pede o modo emoldurado explícito (`AccessUnitReader.cs:69`); dica no registro se o FFmpeg não tiver `-stats_mux_pre` (hoje entra em laço sondagem → falha); pacote de tamanho 0 não derruba; teste com FFmpeg real marcado como pulado no CI; comentário em `FfmpegArguments.Framing` (1 fluxo, nada de `-stats_enc_*` no `pipe:1`). Republicar e trocar o atalho.
  3. Revisão final do branch da 5d (inclui os menores anotados da Task 3b: não dar o balão de escala quando o driver é antigo; mensagem da bancada quando a lista de modos falha).
  4. Task 6: foto do cronômetro (PC + iPhone juntos), uso real (mouse parado → mexer, rolagem, podcast), docs e §8 da spec; CI verde nos 3 workflows.
- Testes instáveis vistos no CI (anotar na revisão final): `UsbTunnelServiceTests.KeepsTunnelAndReconnectsWhenVideoFails`, `UsbmuxTests.ConnectReturnsTunnelToThePhone`.

**Pendências que o usuário decidiu deixar para depois**
- **Task 9 da 5c** (teste formal): `$env:TELADOIS_LOCAL_DRIVER_TEST=1; dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~VirtualDisplayLocalTests"` (nunca rodou) + roteiro no iPhone (modo desligado = só monitores físicos; Win+Shift+[ / ]; reiniciar o Windows e o "3" não volta; "Sair" não deixa `ffmpeg.exe`; Win+Shift+] duas vezes rápido) + `docs/INSTALACAO.md`.
- 5e (atrasar o som só do programa no iPhone): decidir com o número da foto da Task 6. Plano já discutido: descobrir o programa no monitor virtual → desviar o som dele (função interna do Mixer, como o EarTrumpet) para um dispositivo de som virtual → atrasar e tocar nos alto-falantes. Recusados: Equalizer APO, ajuste no player, som no iPhone. Referência: som adiantado < ~45 ms quase não se nota; > ~80 ms incomoda.
- Etapa 3 (Spotify): adiada em 2026-10-18.
- Clima: tentar de novo em ~1 min após falha, tempo-limite na primeira localização e checar permissão antes.
- O teste "relógio ocupa > 50% da altura" mede a caixa do texto, não os dígitos; revisão de prints é a garantia real.
- Para a revisão de segurança/robustez da Etapa 6: pasta `\TelaDois` do Agendador sem SDDL própria; janela rara do desligar atrasado (> `RetireLimit` 5 s); `DarkChanged` não chamado se um assinante de `StateChanged` lançar; cada desligar roda até 3 `schtasks`; tarefas e pipe do driver que qualquer usuário do PC pode disparar.

**Bloqueio**
- Nenhum.
- **PC de casa (AMD):** rodar de novo `tools\vdd\instalar-vdd.ps1` como administrador (correção do 1167 + modos ÷ 1,75) antes de usar a 5c/5d lá; teste da 5a/5b no AMD (`amf-download`) continua pendente.
- **Certificado:** a última instalação no iPhone foi pelo PC do trabalho (0.5.0, `a63bed0`). Instalar pelo PC de casa pede para revogar o certificado (o usuário responde `yes` numa aba do Terminal).

**Próximos passos**
1. Etapa 5d: bancada do parado → aplicar a correção → revisão final → Task 6 (foto + docs).
2. Task 9 da 5c (e o teste no AMD em casa, depois de rodar o instalador lá).
3. Com `ios-test`, `ios-build` e `windows-test` verdes no SHA final, **perguntar ao usuário** se junta `etapa-5b-monitor-virtual` (5a–5d) ao `main`.
4. Decidir a 5e. 5. Etapa 3 — Spotify. 6. Etapa 6 — acabamento.

Há um projeto Android parecido em outra pasta (`C:\Pedro\Arquivos Pedro\Aplicativo Android`), com sessão e repositório próprios.

## Variáveis de ambiente
Nenhuma é necessária hoje. Credenciais ficam fora do repositório:
- GitHub: `gh auth login` (guardado no cofre do sistema, não em variável).
- Futuro (Etapa 3): `SPOTIFY_CLIENT_ID` e `SPOTIFY_REDIRECT_URI` (Spotify Developer). Nunca versionar valores; usar arquivo local ignorado (`.env`) ou segredo do GitHub Actions.

## Ambiente do Claude Code
Levantado dos arquivos de configuração em 2026-10-16 (`~/.claude/settings.json`, `~/.claude/plugins/`, `~/.claude/skills/`, `~/.claude.json`, plugins do app Claude Desktop) e conferido de novo no PC do trabalho em 2026-10-19 (`claude plugin list`, `claude mcp list`, `~/.claude/skills`, `wsl -l -v`, `xtool --version`): mesmos 4 plugins do CLI, MCP `rea` (rea-agents 6.5.0) e `apify` conectados, skills pessoais `graphify` e `reverse-engineer-anything`. **Novo desde a última versão:** FFmpeg 9 (Etapa 5a); tabela "O que está em cada PC" (casa conferido em 2026-10-19); Virtual Display Driver planejado (Etapa 5b). Não há `.mcp.json` no projeto; a skill de projeto é a `.claude/skills/encerrar`.
- No PC do trabalho o `uv` que o PATH acha primeiro é o de `~\.local\bin` (0.11.32) e funciona com `uv tool run`; se travar com "capacity overflow", veja as armadilhas abaixo.

### O que está em cada PC
PC de casa conferido em 2026-10-19 e de novo em 2026-10-21 (`Get-Command`, `dotnet --version`, `uv --version`, `claude plugin list`, `claude mcp list`, `~/.claude/skills`, `wsl -l -v`, `xtool --version`, `Get-PnpDevice -Class Display`); PC do trabalho pelo levantamento dele no mesmo dia.

| Item | Casa (`D:\Downloads\Teladois`, AMD) | Trabalho (`C:\Pedro\Arquivos Pedro\Aplicativo celular`, Intel) |
|---|---|---|
| git, gh 2.102 (logado), Node 24, Python 3.14 (`py`) | ✅ (git 2.55, Node 24.20) | ✅ (git 2.54, Node 24.17; conferido em 2026-10-21) |
| .NET SDK 10.0.402 | ✅ | ✅ |
| uv + graphify 0.9.92 (`uv tool run --from graphifyy python -m graphify`) | ✅ (uv 0.13.1; o `graphify.exe` também abre) | ✅ (uv 0.11.32; `graphify.exe` bloqueado pelo Windows) |
| Claude Code CLI | ✅ (2.1.303) | ✅ (2.1.195) |
| FFmpeg 9.0.2 (Gyan) | ✅ | ✅ |
| WSL Ubuntu + xtool 1.22.2 logado + `USBMUXD_SOCKET_ADDRESS` | ✅ | ✅ |
| iTunes/iCloud web + Apple Mobile Device Support 20 | ✅ | ✅ |
| **Virtual Display Driver 25.7.23** + tarefas `\TelaDois\` (Etapas 5b/5c) | ⚠️ instalado, mas **rodar o instalador de novo** (correção do 1167 + modos ÷ 1,75) | ✅ instalado; instalador rodado de novo em 2026-10-21 (tarefa "Ligar" com `Enable-PnpDevice` + modos ÷ 1,75) |
| Plugins `agent-skills`, `frontend-design` | ✅ | ✅ |
| Plugins `hyperframes`, `supabase` | ❌ (opcionais, não usados aqui) | ✅ |
| Skill pessoal `graphify` | ✅ | ✅ |
| Skill pessoal `reverse-engineer-anything` + MCP `rea` | ✅ (novo em 2026-10-21; MCP `rea` = `npx -y rea-agents@6.7.0 mcp`, que deu tempo-limite no `claude mcp list` da conferência) | ✅ |
| MCP `apify` | ❌ (não usado aqui) | ✅ |

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
| FFmpeg 9 (novo na Etapa 5a) | captura (`ddagrab`) e compressão H.264 da Segunda Tela; o `TelaDois.exe` acha pelo PATH | `winget install --id Gyan.FFmpeg -e` e abrir um terminal novo (ou reiniciar o `TelaDois.exe`) |
| WSL + Ubuntu | rodar o xtool | `wsl --install -d Ubuntu`; em `%USERPROFILE%\.wslconfig`: `[wsl2]` `networkingMode=mirrored` (o Ubuntu enxerga o `127.0.0.1:27015` do Windows) |
| xtool 1.22.2 (no Ubuntu) | instalar o `.ipa` no iPhone | Script `tools/wsl/instalar-xtool.sh` (confere o SHA-256, instala `zip`/`unzip`, extrai o AppImage em `/opt/xtool`, cria `/usr/local/bin/xtool` e `/etc/profile.d/xtool-usbmuxd.sh` com `USBMUXD_SOCKET_ADDRESS=127.0.0.1:27015`): `wsl -d Ubuntu -- bash "/mnt/<repo>/tools/wsl/instalar-xtool.sh" ["/mnt/<AppImage já baixado>"]`; depois `wsl -d Ubuntu -- bash -lc 'xtool auth login'` (Apple ID; o próprio usuário digita) |
| Virtual Display Driver 25.7.23 (Etapa 5b) | o monitor extra em Estender | uma vez por PC, PowerShell **como administrador**: `powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1` (script criado na Task 1 do plano da 5b; baixa só o pacote do driver e confere SHA-256 e assinatura) |
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
| Pessoais (`~/.claude/skills`) | `reverse-engineer-anything` (usa o MCP `rea`), `graphify` (instalada pelo `graphify install`, ver abaixo), `apple-hig-designer` (nova em 2026-10-20 no PC do trabalho; desenho no estilo Apple — copiar a pasta para o outro PC se for usar) |
| Projeto (`.claude/skills`) | `encerrar` — comando **`/encerrar`**: rotina de fim de sessão (atualiza este CLAUDE.md, verifica o ambiente de verdade, confere segredos, commit e push, lista o que falta no outro PC). Vai pelo Git, então funciona nos dois PCs |

- **`reverse-engineer-anything`**: não vale mover para o projeto — é genérica e depende do REA instalado globalmente (`npm install -g rea-agents`); copie a pasta `~/.claude/skills/reverse-engineer-anything` para o outro PC.
- **`graphify`** (mapa do código em grafo para navegar no projeto gastando menos tokens; comando `/graphify .`): o pacote certo é o **`graphifyy`** (dois "y") do PyPI, repositório oficial https://github.com/Graphify-Labs/graphify. O pacote npm `graphify` **não tem relação** com ele, não use. Instalação (uma vez por PC): `winget install --id astral-sh.uv -e` → terminal novo → `uv tool install graphifyy` → `uv tool update-shell` → `graphify install --platform claude`. Isso cria `~/.claude/skills/graphify/` e o `~/.claude/CLAUDE.md` global que registra a skill (sem hooks; hooks só com `--project`). Instalado em 2026-10-18 (versão 0.9.92) no PC onde o projeto fica em `D:\Downloads\Teladois` e em 2026-10-19 no PC onde fica em `C:\Pedro\Arquivos Pedro\Aplicativo celular`. Neste segundo PC o Windows (Controle de Aplicativos) **bloqueia o `graphify.exe`** criado pelo uv (não é assinado); a skill funciona assim mesmo, porque chama `python -m graphify` pelo Python do `uv tool run --from graphifyy`. Para rodar à mão: `uv tool run --from graphifyy python -m graphify <comando>`. Não desligue a proteção do Windows.

### Armadilhas de instalação já vistas
- `uv` antigo (0.11.x) travava ao abrir ("capacity overflow"): `winget upgrade --id astral-sh.uv -e --force`.
- `dotnet test` com `NU1100` ("Não foi possível resolver xunit…"): o NuGet do usuário estava sem nenhuma fonte. Corrigir com `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org`.
- `xtool auth login` parando em `[Downloading libraries] NN%` com `HTTPClientError.remoteConnectionClosed`: a rede espelhada do WSL corta downloads grandes (o mesmo arquivo baixa inteiro pelo Windows). O xtool pula esse download se `~/.config/xtool/Anisette/libs/` existir; o `tools/wsl/instalar-xtool.sh` já cria essa pasta com download retomável. Senha certa mas erro `-22406` = senha digitada errada (ela não aparece na tela).
- `wsl --install -d Ubuntu` antes do reinício só instala o WSL; se depois do reinício `wsl -l -v` disser "não tem distribuições instaladas", rode `wsl --install -d Ubuntu` de novo (sem reiniciar) e crie o usuário Linux na própria janela.
- Do PowerShell para o Ubuntu, prefira `wsl -d Ubuntu --exec bash -lc '…'`: sem `--exec`, o shell intermediário do WSL expande `$VARIAVEIS` e mexe nas aspas antes do `bash -lc`.

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
- [ ] Instalar o que a tabela "O que está em cada PC" marca com ❌ para aquele PC (se for usar).
- [ ] Protótipo: `cd prototype && npm ci` se o `package-lock.json` mudou.

O que **não** vai pelo git (fica em cada PC): a memória automática do Claude (`~/.claude/projects/.../memory`), o controle de tarefas dos subagentes (`.superpowers/sdd/`), a pasta `builds/` e os prints locais. Por isso o "Estado atual" é a fonte da verdade.
