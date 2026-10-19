# Etapa 5a — Vídeo pelo cabo (monitor existente → iPhone) — Especificação de Design

- **Data:** 2026-10-19
- **Status:** desenho aprovado em conversa; aguardando revisão do documento
- **Contexto:** especificação geral `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§3.1–§3.4, §4.1, §5). Túnel USB pronto na Etapa 4 (`docs/superpowers/plans/2026-10-18-etapa-4-tunel-usb.md`).
- **Decisões do usuário:** uso "um pouco de tudo" (texto nítido parado + até 60 fps em movimento); Etapa 5 dividida em **5a** (este documento) e **5b** (monitor virtual, documento próprio depois).

## 1. Objetivo

Com o app aberto no iPhone e o cabo plugado, o PC transmite **um monitor que já existe** para o iPhone, em tela cheia, com latência < 100 ms. Ao desconectar, o iPhone volta ao Modo Descanso e o PC encerra a captura. Sem instalar driver nenhum.

### Fora do escopo da 5a
- Monitor virtual (driver, criar/remover monitor) → **5b**.
- Toque/mouse a partir do iPhone, áudio, Wi-Fi.
- Pedir imagem completa sob demanda (`KEYFRAME_REQ`): reservado; na 5a o iPhone espera a próxima imagem completa (≤ 2 s).

## 2. Ambiente de referência

| PC | Placa de vídeo | Compressão esperada | Monitores |
|---|---|---|---|
| `C:\Pedro\Arquivos Pedro\Aplicativo celular` (trabalho) | Intel UHD Graphics | `h264_qsv` | `\\.\DISPLAY2` 1920×1080 (principal) + `\\.\DISPLAY1` 1366×768 |
| `D:\Downloads\Teladois` (casa) | AMD Radeon | `h264_amf` | ver no PC |

FFmpeg: instalado pelo winget (`Gyan.FFmpeg`, 9.x) e encontrado pelo `PATH`. iPhone de teste: iPhone 15/16/17 (paisagem 2556×1179).

## 3. Arquitetura

```
PC (TelaDois.exe na bandeja)                                   iPhone (TelaDois)
┌──────────────────────────────────────────────┐   cabo   ┌───────────────────────────────────┐
│ EncoderProbe → escolhe a compressão da placa │  (túnel  │ UsbLink recebe as mensagens       │
│ DisplayCatalog → monitores (nome ↔ índice)   │  usbmuxd)│   CONFIG → Segunda Tela           │
│ FfmpegCapture → ffmpeg ddagrab do monitor    │ ───────► │   VIDEO  → H264Parser (Core) →    │
│   → H.264 Annex-B no stdout                  │          │   StreamPlayer (hardware do       │
│ AccessUnitReader → 1 imagem por VIDEO        │          │   iPhone, tela cheia, aspecto)    │
│ FrameQueue → descarta até a próxima completa │ ◄─────── │ PONG / BYE (Etapa 4)              │
│ LinkSession (Etapa 4) + CONFIG + VIDEO       │          │ desconectou → Modo Descanso       │
└──────────────────────────────────────────────┘          └───────────────────────────────────┘
```

### 3.1 PC (C#, `windows/src/TelaDois.Core`)

| Unidade | Responsabilidade |
|---|---|
| `EncoderProbe` | Na primeira sessão, testa as compressões nesta ordem e guarda a primeira que codifica 1 imagem com sucesso: `h264_amf`, `h264_qsv`, `h264_nvenc`, `h264_mf`; senão `libx264`. Registra a escolhida no log. Executa o FFmpeg por uma abstração de processo (testável com FFmpeg falso). |
| `DisplayCatalog` | Lista os monitores com nome do Windows (`\\.\DISPLAYn`), resolução, se é o principal e o **índice de saída usado pelo `ddagrab`** (mapeado pela enumeração DXGI de saídas, não pela ordem do `Screen.AllScreens`). |
| `FfmpegArguments` | Monta a linha de comando a partir de (compressão, índice, fps). Base: `-f lavfi -i ddagrab=output_idx=<n>:framerate=60:draw_mouse=1`, compressão escolhida com opções de baixa latência, `-bf 0`, `-g 120` (imagem completa a cada 2 s a 60 fps), `-b:v 12M -maxrate 20M`, `-bsf:v h264_metadata=aud=insert`, saída `-f h264 pipe:1`. A conversão de formato entre a captura e a compressão (`hwdownload`/`hwmap`) é a que funcionar para cada compressão, validada no plano com o FFmpeg real nos dois PCs. |
| `FfmpegCapture` | Inicia/encerra o processo (encerra a árvore inteira), lê o stdout, reinicia com espera 1 s → 2 s → 5 s se o processo morrer, não iniciar ou a leitura falhar (sempre registrado no log; a captura nunca para calada). Guarda o stderr no log (últimas linhas). Encerrar sempre termina: mata o processo para destravar a leitura do pipe e cancela quem espera imagem. |
| `AccessUnitReader` | Divide o fluxo Annex-B em imagens (unidades de acesso) pelos delimitadores `AUD`; marca a imagem como completa quando contém um NAL IDR (tipo 5). |
| `FrameQueue` | Fila entre o FFmpeg e o socket, limitada por **tempo**: descarta quando a imagem mais antiga esperando passa de 100 ms (rajadas curtas, várias imagens de uma leitura só depois de um engasgo do PC, passam inteiras). Ao descartar: joga fora as pendentes; se a nova é completa, entra; senão, descarta as próximas incompletas até chegar uma completa. Teto de memória: 240 imagens esperando ⇒ mesmo descarte. Avisa o início de cada episódio (uma linha no log por episódio). |
| `LinkSession` (extensão) | Depois do `HELLO`: manda `CONFIG(largura, altura, fps)` do monitor capturado e as imagens como `VIDEO`, intercaladas com `PING` (mesmo canal com a trava de escrita existente). Ao sair, `BYE` e fim da captura. |
| Bandeja | Status ganha "transmitindo · <monitor> · <compressão> · <fps>". Menu "Monitor transmitido" lista o `DisplayCatalog`; padrão = primeiro monitor **não principal**, senão o principal. Escolha salva em `%LOCALAPPDATA%\TelaDois\settings.json`. |

### 3.2 iPhone (Swift)

| Unidade | Responsabilidade |
|---|---|
| `TelaDoisCore.LinkProtocol` (extensão) | `config(width:height:fps:)` / `parseConfig`, `video(keyframe:timestamp:accessUnit:)` / `parseVideo`. |
| `TelaDoisCore.H264Parser` | Divide Annex-B em NALs; extrai SPS (7) e PPS (8); converte a imagem para o formato com prefixo de tamanho de 4 bytes (AVCC); informa se há IDR. Puro e testado. |
| `StreamPlayer` (app) | `UIView` com `AVSampleBufferDisplayLayer` (`videoGravity = .resizeAspect`, fundo preto). Cria o `CMVideoFormatDescription` a partir de SPS/PPS, monta um `CMSampleBuffer` por imagem com "exibir imediatamente". Em erro (`status == .failed`) faz `flush` e ignora tudo até a próxima imagem completa. Processa fora da thread principal. |
| `UsbLink` (extensão) | `CONFIG` → estado "segunda tela"; `VIDEO` → entregue ao `StreamPlayer` sem passar pela thread principal. Desconexão → volta ao Descanso. |
| Tela raiz | Estado `secondScreen`: mostra o `StreamPlayer` em tela cheia, esconde o relógio, **desliga o escurecimento** (sem tela preta após 60 s, brilho do usuário). Volta ao Descanso ao desconectar. |

### 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#)

Mensagem: `[tipo 1 byte][tamanho 4 bytes big-endian][dados]` (Etapa 4). Novos dados:

| Tipo | Dados | Referência |
|---|---|---|
| `CONFIG` (2) | largura u32, altura u32, fps u16 (big-endian) | `CONFIG(1366, 768, 60)` = `020000000A0000055600000300003C` |
| `VIDEO` (3) | marcadores u8 (bit 0 = imagem completa/IDR), carimbo u64 em µs desde o início da transmissão, imagem H.264 Annex-B (com SPS/PPS antes do IDR) | `VIDEO(completa, 1000 µs, 00 00 00 01 09 F0)` = `030000000F0100000000000003E80000000109F0` |

Limite de dados por mensagem: o `maxPayload` existente (16 MiB). O tempo-limite de 3 s continua valendo (vídeo e `PING` contam como "notícia").

## 4. Tratamento de erros

| Situação | Comportamento |
|---|---|
| FFmpeg não encontrado | Bandeja: "Instale o FFmpeg (winget install Gyan.FFmpeg)"; túnel continua como na Etapa 4, sem vídeo (o iPhone não recebe `CONFIG` e fica no Descanso com "PC conectado") |
| Compressão da placa falha | Tenta a próxima da lista; por último `libx264 -preset ultrafast -tune zerolatency` |
| FFmpeg fecha/trava | Reinicia (1 s, 2 s, 5 s); o iPhone mantém a última imagem até chegar uma completa |
| Cabo não dá conta | `FrameQueue` descarta quando a imagem mais antiga esperando passa de 100 ms, até a próxima imagem completa (sem atraso acumulado); log: "cabo atrasado: descartando até a próxima imagem completa (N descartadas até agora)", uma vez por episódio |
| Monitor escolhido sumiu | Volta ao padrão e registra no log |
| Erro de decodificação no iPhone | `flush` e espera a próxima imagem completa (≤ 2 s) |
| Cabo removido, app fechado, `BYE` ou tempo-limite | PC encerra o FFmpeg (árvore inteira); iPhone volta ao Descanso em < 1 s |

## 5. Testes

- **Pacote `TelaDoisCore` (`swift test` no CI):** `H264Parser` com um vídeo de teste minúsculo gerado pelo FFmpeg e versionado (ex.: 4 imagens 64×64 de barras coloridas, Annex-B com AUD); bytes de referência de `CONFIG`/`VIDEO`.
- **PC (xUnit, local e `windows-test` no CI):** `FfmpegArguments`, `EncoderProbe` com FFmpeg falso, `AccessUnitReader` com o mesmo vídeo de teste, `FrameQueue` (descarte), `LinkSession` mandando `CONFIG`/`VIDEO`; bytes de referência iguais aos do Swift. Um teste de integração com o FFmpeg real (`lavfi testsrc` → compressão por software → `AccessUnitReader`) que é pulado se o FFmpeg não estiver no `PATH`.
- **UI (XCUITest no CI):** o teste age como o PC — conecta em `127.0.0.1:7100` no simulador, lê o `HELLO`, manda `CONFIG` e o vídeo de teste; print `04-segunda-tela.png` mostra as barras em tela cheia; depois do `BYE` o relógio volta (print `05-volta-descanso.png`). Revisar os prints é obrigatório.
- **No iPhone real (critério de pronto):**
  1. App aberto + cabo: o monitor de 1366×768 aparece no iPhone em ≤ 3 s.
  2. Latência < 100 ms: cronômetro na tela do PC, foto do PC e do iPhone juntos.
  3. Rolar página e vídeo do YouTube fluidos; texto parado nítido.
  4. Tirar o cabo/fechar o app: iPhone volta ao relógio; nenhum `ffmpeg.exe` sobra no Gerenciador de Tarefas.
  5. Repetir 1–4 no PC de casa (AMD, `h264_amf`).

## 6. Riscos

- **Formato entre captura e compressão:** `ddagrab` entrega superfícies D3D11; cada compressão aceita formatos diferentes. Mitigação: validar a linha de comando de cada compressão com o FFmpeg real nos dois PCs no primeiro task do plano.
- **Índice do `ddagrab`:** a ordem de saídas DXGI pode diferir do `\\.\DISPLAYn`. Mitigação: `DisplayCatalog` com enumeração DXGI e teste manual capturando cada monitor.
- **Vazão do túnel USB:** ~20 Mbps é pouco para USB 2.0, mas o caminho passa pelo usbmuxd e pelo `NWConnection`. Mitigação: medir no log (bytes/s e imagens descartadas).
- **Decodificação no simulador:** o CI usa decodificação por software do simulador; o comportamento real (hardware) só é provado no iPhone.
