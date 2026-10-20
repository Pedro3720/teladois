# Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design

- **Data:** 2026-10-19
- **Status:** desenho aprovado em conversa (Partes 1, 2 revisada e 3); aguardando revisão do documento
- **Contexto:** especificação geral `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md`; Etapa 5a (`docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md`), cujo código é a base deste branch (`etapa-5b-monitor-virtual`, saído do `etapa-5a-video`).
- **Decisões do usuário:** driver = **Virtual Display Driver** (opção A); liga sozinho ao plugar; atalho **Win+Shift+]** e item "Modo monitor" na bandeja; sem driver, duplica um monitor existente como na 5a; a 5b sai do `etapa-5a-video` sem juntar a 5a ao `main` antes (o teste da 5a no PC AMD entra no teste final da 5b).

## 1. Objetivo

Com o app aberto no iPhone e o cabo plugado, o Windows ganha **um monitor a mais em Estender**, com a resolução do iPhone, e esse monitor aparece no iPhone. As janelas podem ser arrastadas para ele como para um monitor de verdade. Desligar (atalho, bandeja, tirar o cabo ou fechar o app) **remove o monitor do desktop**: o Windows devolve as janelas aos monitores que sobraram e o iPhone volta ao relógio.

### Comportamento (Parte 1, aprovada)
- **Ao plugar com o app aberto:** liga sozinho — monitor extra em Estender, resolução igual à do iPhone (vinda do `HELLO`), à direita do monitor mais à direita.
- **Win+Shift+]** e o item **"Modo monitor"** da bandeja ligam/desligam. Desligado com o cabo plugado: o monitor sai do desktop e o iPhone mostra o relógio com "PC conectado". Ligar de novo: o monitor volta e o iPhone volta a mostrar a tela.
- **Tirar o cabo / fechar o app no iPhone / sair do TelaDois:** o monitor sai do desktop.
- **Sem o driver instalado:** a bandeja avisa e o app duplica um monitor existente (comportamento da 5a); o atalho liga/desliga a duplicação.
- **Atalho sem iPhone conectado:** balão "Conecte o iPhone pelo cabo com o TelaDois aberto"; nada muda.

### Fora do escopo da 5b
- Escala (DPI) do monitor novo: o Windows não tem API pública para isso; ele escolhe pelo tamanho que o driver informa. O usuário ajusta em Configurações → Tela se quiser.
- Toque/mouse a partir do iPhone, áudio, Wi-Fi, mais de um iPhone.
- Iniciar com o Windows, ícone próprio da bandeja, revisão de segurança do pipe do driver → Etapa 6.

## 2. O driver (lido no código-fonte da release 25.7.23)

- Repositório: https://github.com/VirtualDrivers/Virtual-Display-Driver, tag `25.7.23`, `Virtual Display Driver (HDR)/MttVDD/Driver.cpp`.
- Configuração: `C:\VirtualDisplayDriver\vdd_settings.xml` (`<monitors><count>`, `<resolutions>` com `width`/`height`/`refresh_rate`).
- Pipe `\\.\pipe\MTTVirtualDisplayPipe`, comandos UTF-16: `PING`, `RELOAD_DRIVER`, `SETDISPLAYCOUNT n`, entre outros. O pipe aceita qualquer usuário do PC (`D:(A;;GA;;;WD)`).
- **`SETDISPLAYCOUNT 0` não remove o monitor:** o comando grava `n` no XML e reinicia o driver (linha 1226); ao carregar, `count` 0 vira 1 (linha 1543). Por isso o monitor **fica sempre instalado** (`count` 1) e o TelaDois só o **coloca e tira do desktop** com `ChangeDisplaySettingsEx` (não pede administrador). O pipe é usado **só para o `PING`** (saber se o driver está instalado).

### Instalação (uma vez por PC, com administrador)
Script do projeto `tools/vdd/instalar-vdd.ps1`:
1. Baixa a release `25.7.23` (versão fixa) e confere o **SHA-256** gravado no script; confere a assinatura Authenticode antes de instalar. O download é feito pelo usuário rodando o script (o Claude pede autorização antes de baixar qualquer coisa durante o desenvolvimento).
2. Instala o driver pelo método que a release oferece para linha de comando (a Tarefa 1 do plano escolhe entre o instalador oficial e `pnputil` + criação do dispositivo, conferindo os arquivos da release).
3. Grava o `vdd_settings.xml` com `count` 1 e as resoluções em paisagem dos iPhones 15/16/17: 2556×1179, 2796×1290, 2622×1206, 2868×1320, 2736×1260, todas a 60 Hz (a lista é conferida na Tarefa 1).
4. Ao terminar, tira o monitor novo do desktop (o Windows o liga sozinho ao instalar).

## 3. Arquitetura

```
PC (TelaDois.exe na bandeja)                                    iPhone (TelaDois)
┌───────────────────────────────────────────────┐   cabo   ┌──────────────────────────────────┐
│ Hotkey (Win+Shift+]) ─┐                       │  (túnel  │ UsbLink                          │
│ Bandeja "Modo monitor"├─► MonitorMode         │  usbmuxd)│   CONFIG → Segunda Tela          │
│ Túnel (HELLO / fim) ──┘   liga/desliga        │          │   VIDEO  → VideoRenderer         │
│                            │                  │ ───────► │   VIDEO_END → relógio,           │
│ VirtualDisplay ◄───────────┤ (Estender)       │          │     continua "PC conectado"      │
│   PING no pipe; põe/tira   │                  │          │   desconectou → Descanso         │
│   do desktop               ▼                  │ ◄─────── │ PONG / BYE                       │
│ VideoFeedFactory (5a) → FfmpegCapture → ...   │          │                                  │
│ LinkSession: CONFIG+1ª imagem, VIDEO, VIDEO_END│         │                                  │
└───────────────────────────────────────────────┘          └──────────────────────────────────┘
```

### 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)

| Unidade | Responsabilidade |
|---|---|
| `VirtualDisplay` (novo) | `IsInstalledAsync()`: `PING` no pipe com tempo-limite curto. `Find()`: acha o monitor do driver pelo nome do adaptador (`EnumDisplayDevices`; nome confirmado na Tarefa 1). `Attach(largura, altura)`: põe no desktop em Estender, à direita do monitor mais à direita, no modo pedido ou, se não existir, no modo do driver com a proporção mais próxima (escolha pura, testada). `Detach()`: tira do desktop (tamanho 0). `DetachLeftover()`: ao abrir o TelaDois, tira o monitor se ele ficou no desktop. As chamadas Win32 ficam atrás de uma interface (`IDisplayApi`) para testar com um Windows falso. Base: `DisplayManager` do branch `etapa-5-segunda-tela`. |
| `MonitorMode` (novo, lógica pura) | Estados `Desligado`, `Estendido`, `Duplicando`. Eventos: sessão aberta (com `HELLO`), sessão encerrada, atalho/bandeja, driver ausente, falha ao ligar, monitor saiu do desktop. Saídas: ligar ou desligar o vídeo e em qual modo. Uma operação por vez: um pedido que chega durante outra operação espera ela terminar; dois pedidos iguais seguidos viram um. Testado em tabela (estado × evento). |
| `Hotkey` (novo, bandeja) | `RegisterHotKey` com Win+Shift e a tecla do caractere `]` no layout atual (`VkKeyScanEx`; no ABNT2 a tecla é outra que no US). Se o registro falhar (atalho em uso), avisa na bandeja e o item do menu continua valendo. |
| `VideoFeedFactory` (5a, extensão) | Em `Estendido`: `VirtualDisplay.Attach(HELLO)`, acha a saída DXGI do monitor virtual pelo `\\.\DISPLAYn` no `DisplayCatalog`, escolhe a compressão e abre a captura. **Descartar o feed tira o monitor do desktop.** Em `Duplicando`: igual à 5a. Se o monitor ligar mas não aparecer no `DisplayCatalog` (adaptador DXGI diferente do 0), tira o monitor, registra o motivo e cai para `Duplicando`. |
| `LinkSession` (5a, extensão) | Em vez de abrir o vídeo uma vez, recebe um controle liga/desliga do `MonitorMode`. Ligar: abre o feed e **manda `CONFIG` junto com a 1ª imagem** (correção I1). Desligar: descarta o feed e manda **`VIDEO_END`**; `PING`/`PONG` continuam. Ligar de novo: novo feed, novo `CONFIG`. Uma captura que falha (feed termina sozinho) vira desligado + `VIDEO_END`, não derruba o túnel. |
| `AccessUnitReader` (5a, correção I3) | Guarda o último SPS e PPS vistos; numa imagem com IDR que veio sem eles, reinsere-os logo depois do AUD. Medido em 2026-10-19 no PC AMD: `h264_amf` manda SPS/PPS só no começo (1 de 3 IDRs); com `-header_spacing` sairia em todas, mas a correção no leitor vale para qualquer compressão. |
| Bandeja | Item **"Modo monitor"** (marcado quando ligado; desabilitado sem iPhone). Status: "estendido · 2556×1179 · amf-download · 60 fps" ou "duplicando DISPLAY1 · …". Sem driver: balão uma vez por execução — "Monitor virtual não instalado: duplicando um monitor. Para estender, rode tools\vdd\instalar-vdd.ps1 como administrador". O menu "Monitor transmitido" (5a) continua valendo para o modo `Duplicando`. |

### 3.2 iPhone (Swift)

| Unidade | Responsabilidade |
|---|---|
| `TelaDoisCore.LinkProtocol` | `videoEnd()` e reconhecimento do tipo 8. |
| `TelaDoisCore.LinkSessionState` | `VIDEO_END` → ação "parar vídeo"; a conexão continua (tempo-limite e `PONG` como antes). Novo `CONFIG` depois → vídeo de novo. |
| `UsbLink` / tela raiz | "Parar vídeo" → sai da Segunda Tela para o relógio com "PC conectado"; o escurecimento volta a contar. |
| `VideoRenderer` (correção I2) | `reset()` usa `flush(removingDisplayedImage: true)`: ao religar ou reconectar não aparece a última imagem da vez anterior. |

### 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#)

| Tipo | Dados | Referência |
|---|---|---|
| `VIDEO_END` (8) | nenhum | `VIDEO_END` = `0800000000` |

Ordem ao ligar: `CONFIG` e a 1ª imagem saem juntos (o `CONFIG` só é mandado quando a 1ª imagem já existe). Ao desligar: a última imagem, depois `VIDEO_END`. Os bytes de `CONFIG`/`VIDEO` não mudam.

## 4. Tratamento de erros (Parte 3, aprovada)

| Situação | Comportamento |
|---|---|
| Driver não instalado (`PING` sem resposta ou monitor não achado) | `Duplicando` (como a 5a); balão e registro apontam o `instalar-vdd.ps1`. O atalho liga/desliga a duplicação. |
| Resolução do iPhone fora da lista do driver | Usa o modo com a proporção mais próxima; registra. O iPhone mostra com faixas pretas (`resizeAspect`). |
| `ChangeDisplaySettingsEx` falha ao ligar | Registra o código; `Duplicando`. |
| Monitor ligado mas fora do `DisplayCatalog` (outro adaptador DXGI) | Tira o monitor, registra "monitor virtual fora do adaptador 0", `Duplicando`. |
| TelaDois fecha de repente / PC desliga com o monitor ligado | `DetachLeftover` ao abrir o TelaDois. Ligar **grava no registro do Windows** (`Win32DisplayApi.ForVirtualMonitor`): medido no driver 25.7.23 em 2026-10-20, sem gravar o Windows recusa ligar o monitor que está fora do desktop (`DISP_CHANGE_FAILED`, -1). Desligado assim, ele continua desligado depois de reiniciar; o `DetachLeftover` cobre o que sobrar ligado. |
| Atalho já registrado por outro programa | Balão "atalho Win+Shift+] em uso"; o item da bandeja continua valendo. |
| Cabo tirado e posto rápido / atalho apertado duas vezes | `MonitorMode` faz uma operação por vez; o ligar espera o desligar terminar. |
| Usuário desconecta o monitor virtual nas Configurações | A captura falha; o `MonitorMode` vê que o monitor saiu do desktop e trata como desligado (`VIDEO_END`, relógio no iPhone). |
| Captura falha depois de ligar | Reinício da 5a (1 s, 2 s, 5 s); o iPhone espera a próxima imagem completa. |
| iPhone recebe `VIDEO_END` sem estar no vídeo | Ignora. |

## 5. Testes

- **PC (xUnit, `windows-test` no CI):**
  - `MonitorMode`: tabela estado × evento, incluindo driver ausente → `Duplicando`, atalho sem iPhone, desligar durante o ligar, pedidos repetidos.
  - `VirtualDisplay` com `IDisplayApi` e pipe falsos: achar o monitor, posição à direita, escolha do modo mais próximo, `DetachLeftover`.
  - `LinkSession`: `VIDEO_END` com os bytes de referência; ligar → `CONFIG` só junto com a 1ª imagem (I1); desligar → `VIDEO_END` e `PING` continua; religar → novo `CONFIG`; captura que termina sozinha → `VIDEO_END` sem derrubar o túnel.
  - `AccessUnitReader`: IDR sem SPS/PPS recebe os últimos vistos (I3); IDR que já tem não é alterada.
- **PC local (`Category=Local`, fora do CI, PC com o driver):** achar o monitor real do driver; ligar/desligar; `ddagrab` capturando o monitor virtual (tamanho certo no log); tecla do `]` no layout ABNT2.
- **Pacote `TelaDoisCore` (`swift test`):** `VIDEO_END` com os bytes de referência; `LinkSessionState` com `VIDEO_END` (para o vídeo, mantém a conexão) e `CONFIG` depois (volta ao vídeo).
- **UI (XCUITest no CI):** o PC falso manda `CONFIG` + vídeo e depois `VIDEO_END`; o relógio volta com "PC conectado"; print novo **`07-fim-do-video.png`**. Revisar os prints é obrigatório.
- **No iPhone real, PC AMD (critério de pronto):**
  1. **Antes de instalar o driver:** duplicação como na 5a (cobre o teste da 5a no AMD: o registro mostra `amf-download` ou `amf-d3d11`).
  2. Com o driver: plugar → o monitor extra aparece com a resolução do iPhone em ≤ 3 s; arrastar uma janela até ele.
  3. Win+Shift+] → janelas voltam, iPhone mostra o relógio com "PC conectado"; de novo → monitor e imagem voltam.
  4. Tirar o cabo → o monitor sai do desktop; nenhum `ffmpeg.exe` sobra.
  5. Latência < 100 ms com a foto do cronômetro.

## 6. Riscos

- **Adaptador DXGI do monitor IddCx:** o `ddagrab` e o `DisplayCatalog` só olham o adaptador 0. Se o monitor virtual aparecer em outro, não é capturado. Mitigação: verificar na Tarefa 1 com o driver real; o `vdd_settings.xml` permite escolher a placa (`<gpu><friendlyname>`); senão, `Duplicando`.
- **`ChangeDisplaySettingsEx` com monitor IddCx:** ligar/desligar foi escrito no branch `etapa-5-segunda-tela`, mas nunca rodou com o driver real. Mitigação: Tarefa 1.
- **Controle de Aplicativos do Windows** no PC do trabalho pode bloquear a instalação do driver. Mitigação: sem driver, `Duplicando`.
- **Certificado do xtool:** cada PC que instala o `.ipa` revoga o certificado do outro; a próxima instalação pelo outro PC pede `yes` de novo.
- **Pipe do driver aceita qualquer usuário do PC:** o TelaDois só manda `PING`; registrar na revisão de segurança da Etapa 6.
