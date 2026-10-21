# iPhone como Segunda Tela + Modo Descanso — Especificação de Design

- **Data:** 2026-10-16
- **Status:** design aprovado em conversa; aguardando revisão do documento
- **Nome do app:** a definir na etapa 1 (nome provisório no código: `TelaDois`)

## 1. Objetivo

Um app de iPhone com duas funções:

1. **Segunda Tela via cabo USB** — ao conectar o iPhone no PC Windows pelo cabo, o iPhone vira um monitor extra do Windows. Ao desconectar, o monitor extra desaparece.
2. **Modo Descanso** — imitação do StandBy do iOS, mas **sem precisar estar no carregador**: relógio grande, data, temperatura, bateria e música do Spotify. Após 60 s sem toque a tela fica totalmente preta; 1 toque acende de novo.

### Fora do escopo da v1

- Notificações de apps externos (WhatsApp, Instagram etc.) — o iOS não permite a nenhum app ler notificações ou badges de outros apps. **Decisão: não implementar.**
- Controle de volume, alarme, toque/mouse na Segunda Tela, modo noturno vermelho, Wi-Fi como transporte.
- Abrir o app automaticamente ao virar o iPhone (o iOS não permite; o usuário abre o app ou usa uma automação do app Atalhos).
- Publicação na App Store.

## 2. Restrições do ambiente

| Restrição | Consequência |
|---|---|
| Usuário só tem Windows (sem Mac) | Projeto Xcode gerado por **XcodeGen** a partir de `project.yml`; compilação no **GitHub Actions (macOS)** |
| Sem conta Apple Developer paga | Build **sem assinatura** no CI; assinatura/instalação no Windows com **Sideloadly** (ou AltStore) e **Apple ID grátis**. O app expira a cada **7 dias**; iPhone precisa do **Modo Desenvolvedor** ativo |
| Sem conta paga → sem WeatherKit | Clima via **Open-Meteo** (grátis, sem chave) |
| Repositório **público** | Minutos de macOS ilimitados no GitHub Actions |
| iPhone 15/16/17 (OLED, 2556×1179 em paisagem) | Tela preta = pixels apagados; resolução nativa do monitor virtual: 2556×1179 |
| iOS mínimo | iOS 17 |

## 3. Arquitetura

```
┌─────────────── PC (Windows) ───────────────┐          ┌──────────── iPhone ────────────┐
│  App de bandeja (C#/.NET)                  │          │  App (Swift/SwiftUI)           │
│  DeviceWatcher ── detecta pluga/despluga   │  cabo    │  UsbLink ── escuta porta TCP   │
│  DisplayManager ─ liga/desliga monitor     │  USB     │  StreamPlayer ─ decodifica     │
│       virtual (Virtual Display Driver)     │ (usbmuxd)│       H.264 (VideoToolbox)     │
│  CaptureEncoder ─ FFmpeg: captura monitor  │ ───────► │  AppState ── Descanso ⇄ 2ª tela│
│       virtual + H.264 pela GPU             │          │  RestScreen ─ UI do Descanso   │
│  StreamSender ── envia vídeo pelo túnel    │          │  DimController ─ tela preta    │
└────────────────────────────────────────────┘          └────────────────────────────────┘
```

### 3.1 Transporte USB

- O Windows tem o serviço **Apple Mobile Device Service** (vem com o **iTunes versão web**, do site da Apple, não o da Microsoft Store), que expõe o **usbmuxd** em `127.0.0.1:27015`.
- O PC envia mensagens plist `Listen` (para receber eventos `Attached`/`Detached`) e `Connect` (para abrir um túnel TCP até uma porta do iPhone).
- O app do iPhone escuta numa porta TCP fixa (**7100**) com `NWListener` (Network.framework), apenas enquanto está em primeiro plano.

### 3.2 Componentes do iPhone (Swift/SwiftUI)

| Módulo | Responsabilidade | Depende de |
|---|---|---|
| `AppState` | Máquina de estados: `rest` ⇄ `secondScreen` | `UsbLink` |
| `UsbLink` | Escuta a porta 7100, faz o enquadramento das mensagens do protocolo | Network.framework |
| `StreamPlayer` | Decodifica H.264 e exibe em tela cheia | VideoToolbox, `AVSampleBufferDisplayLayer` |
| `RestScreen` | Interface do Modo Descanso (páginas Relógio e Música) | serviços abaixo |
| `DimController` | Timer de inatividade, tela preta + brilho 0, restauração no toque | `Clock` injetável (para testes) |
| `SpotifyService` | Estado da música e controles | Spotify iOS SDK (App Remote) via SPM |
| `WeatherService` | Temperatura atual | Open-Meteo + CoreLocation |
| `BatteryService` | Nível e estado de carga | `UIDevice` |

### 3.3 Componentes do PC (C#/.NET, app de bandeja)

| Módulo | Responsabilidade |
|---|---|
| `DeviceWatcher` | Cliente usbmuxd: eventos de conexão/desconexão do iPhone |
| `DisplayManager` | Controla o **Virtual Display Driver** (open-source, já assinado): cria/remove o monitor na resolução recebida no `HELLO`; ao iniciar, remove monitores que sobraram |
| `CaptureEncoder` | Executa **FFmpeg** com `ddagrab` apontado para o monitor virtual + codificador H.264 por hardware (`h264_nvenc` / `h264_qsv` / `h264_amf`; fallback `libx264 -preset ultrafast -tune zerolatency`), saída Annex-B no stdout |
| `StreamSender` | Abre o túnel (`Connect` para a porta 7100), envia as mensagens, mede a latência |
| Ícone de bandeja | Status, resolução, fps, latência, "iniciar com o Windows" |

### 3.4 Protocolo PC ↔ iPhone

Cada mensagem: `[tipo: 1 byte][tamanho: 4 bytes, big-endian][dados]`.

| Tipo | Direção | Conteúdo |
|---|---|---|
| `HELLO` | iPhone → PC | versão do protocolo, largura e altura em pixels da tela |
| `CONFIG` | PC → iPhone | largura, altura, fps escolhidos |
| `VIDEO` | PC → iPhone | unidades NAL H.264 (Annex-B) |
| `KEYFRAME_REQ` | iPhone → PC | pede um quadro completo (após erro de decodificação) |
| `PING` / `PONG` | ambos | timestamp, para medir latência e detectar conexão travada (timeout: 3 s) |
| `BYE` | ambos | encerramento limpo |

Padrões da v1: 2556×1179, 60 fps (o PC pode reduzir para 30 fps se a latência passar de 100 ms).

## 4. Comportamento do app no iPhone

### 4.1 Estados

1. Ao abrir o app → **Descanso** (orientação travada em paisagem; `isIdleTimerDisabled = true`).
2. **Descanso ativo**: conteúdo visível por **60 s** desde o último toque.
3. Após 60 s sem toque → **Descanso escurecido**: tela 100% preta, brilho 0 (o brilho anterior é guardado).
4. **1 toque** em qualquer lugar → volta ao Descanso ativo na hora, com o brilho anterior; o timer de 60 s reinicia. Esse toque só acorda a tela — não aciona botões (ex.: não pausa a música por acidente).
5. PC conectado (`HELLO`/`CONFIG` concluídos), em qualquer estado → **Segunda Tela**; timer de escurecer desligado; brilho restaurado.
6. Conexão encerrada (`BYE`, erro de socket ou timeout do `PING`) → **Descanso ativo**.
7. App sai do primeiro plano → brilho original restaurado; listener e conexão são encerrados.

### 4.2 Telas do Descanso (deslizar horizontalmente, como o StandBy)

- **Página 1 — Relógio** (referência: print do StandBy com relógio):
  - Esquerda: horas e minutos gigantes (fonte arredondada condensada).
  - Direita: dia da semana + data, temperatura, anel de bateria (verde, com raio quando carregando).
  - Rodapé: mini indicador "música — artista" apenas quando o Spotify está tocando.
- **Página 2 — Música** (referência: print do StandBy com o player):
  - Esquerda: capa do álbum.
  - Direita: título, artista, barra de progresso com tempo decorrido/restante, botões ⏮ ⏯ ⏭.
  - Sem música tocando: "Nada tocando". Spotify não conectado: botão "Conectar Spotify".
- Fundo da página Música: gradiente escuro derivado das cores da capa.

## 5. Tratamento de erros

| Situação | Comportamento |
|---|---|
| Cabo removido durante o uso | PC recebe `Detached` → remove o monitor virtual em < 1 s; o Windows devolve as janelas ao monitor principal; o iPhone volta ao Descanso |
| Usuário sai do app (Home) | O socket fecha → o PC remove o monitor (nenhuma janela fica presa em tela invisível) |
| App do PC trava ou fecha | Na próxima abertura, `DisplayManager` remove monitores virtuais remanescentes |
| Sem codificador de hardware | Fallback para `libx264 ultrafast zerolatency` |
| Erro de decodificação no iPhone | Descarta até o próximo keyframe e envia `KEYFRAME_REQ` |
| Spotify não instalado / sem login / desconectado | Mostra "Conectar Spotify"; 1 toque tenta reconectar |
| Localização negada ou Open-Meteo fora do ar | Temperatura escondida; o resto funciona. Atualização a cada 30 min |
| Apple Mobile Device Service ausente | Ícone de bandeja mostra o aviso com a instrução de instalar o **iTunes versão web** (do site da Apple, não o da Microsoft Store) |

## 6. Estrutura do repositório

```
/
├── ios/                    app Swift (project.yml do XcodeGen, fontes, testes, UI tests)
├── windows/                solução C#/.NET (app de bandeja + testes xUnit)
├── prototype/              protótipo HTML do Descanso + testes Playwright
├── docs/                   especificações, planos, ADRs
└── .github/workflows/
    ├── ios-build.yml       XcodeGen → xcodebuild (CODE_SIGNING_ALLOWED=NO) → .ipa como artefato
    └── ios-test.yml        testes unitários + XCUITest no simulador → prints como artefato
```

## 7. Testes

| Alvo | Como |
|---|---|
| Lógica do iPhone | XCTest no CI: `DimController` com relógio simulado, transições do `AppState`, parser do protocolo |
| Telas do iPhone | XCUITest no simulador: abrir Descanso, deslizar páginas, escurecer (timer encurtado por argumento de lançamento), tocar e verificar que acendeu; prints salvos como artefato e revisados |
| Visual antes do Swift | Protótipo HTML em viewport 2556×1179 (escala 1/3 aceitável) verificado com Playwright |
| PC | xUnit: protocolo, mensagens plist do usbmuxd, estados do `DisplayManager` |
| Ponta a ponta | Manual com o iPhone real; latência medida por `PING` exibida no ícone de bandeja. Meta: < 100 ms a 60 fps |
| Segurança | Cada `.ipa` é inspecionado com Python isolado (Info.plist, cabeçalho Mach-O, ausência de assinatura) — as ferramentas Apple do REA não rodam no Windows. O REA inspeciona o binário do Virtual Display Driver antes da instalação |

## 8. Etapas de entrega

Cada etapa termina em algo funcionando e verificado.

| # | Etapa | Entrega verificável |
|---|---|---|
| 0 | Fundação | Repositório no GitHub, CI gerando o `.ipa` de um app "Olá", app instalado no iPhone via Sideloadly |
| 1 | Design | Ícone (Canva), tokens de cor/tipografia, protótipo HTML aprovado |
| 2 | Descanso v1 | Relógio, data, bateria, clima, escurecer em 60 s e acender com 1 toque — testado no CI e no iPhone — concluída no CI em 2026-10-18; instalada e aberta no iPhone em 2026-10-18 (build do `9cce6d6`) |
| 3 | Spotify | Página Música com capa, progresso e controles |
| 4 | Túnel USB | App do PC detecta o iPhone e troca `HELLO`/`PING` pelo cabo — concluída em 2026-10-18; testada no iPhone pelo cabo (latência de poucos ms, reconexão automática) |
| 5 | Segunda Tela | Monitor virtual + transmissão + desconexão automática — **5a** (duplicar) e **5b** (monitor virtual estendido + Win+Shift+]) testadas no iPhone em 2026-10-20 no PC Intel (`qsv-hwmap`); **5c** (monitor some do Windows quando desligado + tela preta Win+Shift+[) com código pronto e revisado em 2026-10-21, falta o teste no driver real e no iPhone; **5c** em uso no PC do trabalho desde 2026-10-21; **5d** (atraso/fluidez) em andamento: monitor do iPhone em 100% (1460×674), rolagem/vídeo ~25 ms na bancada, falta o atraso com a tela parada e a foto do cronômetro; falta o teste no PC AMD e juntar ao `main` (branch `etapa-5b-monitor-virtual`) |
| 6 | Acabamento | Latência ajustada, ícone de bandeja completo, iniciar com o Windows, revisão de segurança |

## 9. Ferramentas do Claude por etapa

- **Todas as etapas:** `superpowers:writing-plans`, `superpowers:subagent-driven-development`, `superpowers:test-driven-development`, `superpowers:systematic-debugging`, `superpowers:verification-before-completion`, `superpowers:requesting-code-review`, `agent-skills:incremental-implementation`, `agent-skills:documentation-and-adrs`, `agent-skills:git-workflow-and-versioning`.
- **Etapa 0:** `init`, `agent-skills:constraints`, `agent-skills:ci-cd-and-automation`, inspeção do `.ipa` com Python isolado (o REA não inspeciona artefatos Apple no Windows).
- **Etapa 1:** Canva (ícone/artes), `ui-ux-pro-max`, `ui-ux-pro-max:design-system`, `ui-ux-pro-max:design`, `frontend-design`, Artifacts (mockups), Playwright (protótipo).
- **Etapas 2–3:** `agent-skills:source-driven-development`, `agent-skills:debugging-and-error-recovery`, XCUITest via CI.
- **Etapas 4–5:** `agent-skills:api-and-interface-design`, `agent-skills:doubt-driven-development`, `agent-skills:performance-optimization`, `agent-skills:observability-and-instrumentation`, `agent-skills:security-and-hardening`, agente `security-auditor`, REA (driver e usbmuxd).
- **Etapa 6:** `code-review`, `security-review`, `agent-skills:ship`, `simplify`, `graphify` (a partir do momento em que houver código), `hyperframes` (vídeo demo, opcional).

## 10. Pré-requisitos do usuário

- Conta no GitHub (etapa 0).
- **iTunes e iCloud versão web** (instaladores do site da Apple, não da Microsoft Store) instalados no Windows (etapa 0 — necessário para o Sideloadly e para o usbmuxd).
- **Sideloadly** instalado; Apple ID grátis; Modo Desenvolvedor ativo no iPhone (etapa 0).
- Cadastro grátis no **Spotify Developer** com um app criado (Client ID + Redirect URI) (etapa 3).
