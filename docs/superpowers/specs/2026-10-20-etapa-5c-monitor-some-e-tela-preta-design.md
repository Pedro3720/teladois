# Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design

- **Data:** 2026-10-20
- **Status:** desenho aprovado em conversa (Partes A e B); aguardando revisão do documento
- **Contexto:** Etapa 5b (`docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md`) testada no iPhone em 2026-10-20 no PC Intel: estender, atalho Win+Shift+] e tirar o cabo funcionam. Branch: continua no `etapa-5b-monitor-virtual` (a 5b ainda não foi para o `main`).
- **Fora desta etapa:** atraso, fluidez e sincronia som/imagem — viram uma rodada de experimentos medidos e especificação própria (decisão do usuário: "A+B juntos, C em seguida").

## 1. Objetivos

- **A.** Com o modo monitor desligado (atalho, menu, cabo tirado, app fechado, TelaDois fechado), o Windows **não lista** o 3º monitor (hoje aparece o "3" cinza em Configurações → Sistema → Tela, porque o monitor só sai do desktop).
- **B.** Um atalho **Win+Shift+[** deixa a tela do iPhone totalmente preta (brilho mínimo) com o monitor continuando a funcionar no Windows; volta pelo mesmo atalho ou com **1 toque** no iPhone.

## 2. Parte A — o monitor só existe enquanto está em uso

### 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20)
- Tirar o monitor do desktop (`ChangeDisplaySettingsEx`, tamanho 0) não o remove da lista do Windows.
- O pipe do driver não remove o monitor (`SETDISPLAYCOUNT 0` vira 1 ao recarregar — lido no código do driver).
- Remover de verdade = **desativar o dispositivo** `Root\MttVDD` (precisa de administrador). Decisão do usuário: **tarefas agendadas** criadas uma vez pelo instalador (alternativas recusadas: serviço sempre rodando; trocar para o Parsec VDD).

### 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC)
Além do que já faz:
1. Acha o `InstanceId` do dispositivo pelo hardware ID `Root\MttVDD` (não supõe `ROOT\DISPLAY\0000`).
2. Cria (ou recria) duas tarefas na pasta `\TelaDois\` do Agendador:
   - `Ligar monitor virtual` → `pnputil.exe /enable-device "<InstanceId>"`
   - `Desligar monitor virtual` → `pnputil.exe /disable-device "<InstanceId>"`
   - Rodam como `SYSTEM`, sem gatilho (só sob demanda), com permissão de **leitura e execução para os usuários do PC** (grupo Usuários, `BU`, no descritor de segurança da tarefa — o PowerShell de administrador pode ser outra conta que não a do dia a dia), tempo-limite de 1 min, sem várias instâncias ao mesmo tempo.
3. No fim, desativa o dispositivo (o "3" some) e escreve o que fez.
4. Rodar de novo é seguro (idempotente): recria as tarefas com o `InstanceId` atual.

### 2.3 TelaDois (sem administrador)
- Peça nova `VirtualDeviceSwitch` (atrás de uma interface testável): `IsAvailable` (as duas tarefas existem), `EnableAsync(ct)`, `DisableAsync(ct)`. Dispara a tarefa pelo Agendador do Windows (`schtasks /run /tn "\TelaDois\…"` ou a API COM) e espera o resultado olhando o próprio sistema: depois de ligar, até o monitor do driver aparecer em `VirtualDisplay.Find()` (limite 20 s); depois de desligar, até ele sumir (limite 5 s). Nunca lança para quem chama; registra tempo e falhas no registro.
- **Ligar** (o vídeo vai abrir em Estender): `EnableAsync` → `Attach` (gravando no registro, como hoje) → espera da DXGI → captura. Tudo dentro da abertura do vídeo, que já roda fora do laço do PING: o iPhone fica no relógio com "PC conectado" e a conexão não cai.
- **Desligar** (descarte do `VirtualMonitorFeed`, sair do TelaDois): `Detach` → `DisableAsync`.
- **Ao abrir o TelaDois:** se o dispositivo estiver ativo (sobra de travamento ou reinício), `DetachLeftover` + `DisableAsync`.
- **Sem as tarefas** (instalação antiga): comportamento da 5b (monitor fora do desktop, mas listado) e um balão uma vez por execução: "Rode o instalador do monitor virtual de novo para o Windows esconder o monitor quando ele não estiver em uso".
- **Driver desativado não é "driver ausente":** a decisão Estender/Duplicar passa a considerar "tarefas disponíveis" — com o dispositivo desativado o pipe do driver não responde, mas Estender continua possível (liga primeiro). Sem driver e sem tarefas: Duplicar, como hoje.

### 2.4 Riscos a validar no 1º passo do plano (driver real)
- Tempo real de `enable-device` até o monitor aparecer e até a DXGI vê-lo (estimativa 2–4 s).
- Se o Windows recoloca o monitor no desktop sozinho ao reativar (com a topologia salva) — o `Attach` com registro cobre os dois casos.
- `pnputil /disable-device` em dispositivo de vídeo pode pedir reinício em algum caso → registrar e tratar como "não desligou".
- Segurança: qualquer usuário do PC pode disparar as duas tarefas, mas elas só ligam/desligam esse dispositivo — entra na revisão de segurança da Etapa 6, junto com o pipe do driver.

## 3. Parte B — tela preta no iPhone

### 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes)
- Mensagem nova **`SCREEN` (tipo 10)**, 1 byte de dados: `1` = apagar, `0` = acender. Vale nos dois sentidos:
  - PC → iPhone: o atalho/menu mandam apagar ou acender.
  - iPhone → PC: ao acender por toque, o iPhone manda `SCREEN(0)` (a bandeja fica certa).
- Referência: `SCREEN(1)` = `0A0000000101`, `SCREEN(0)` = `0A0000000100`.

### 3.2 PC
- Atalho **Win+Shift+[**, tecla achada pelo caractere `[` no layout atual (como o `]` da 5b).
- Item da bandeja "Tela preta no iPhone (Win+Shift+[)", com marcação.
- Só age com o vídeo ligado (Estender ou Duplicar); senão registra "tela preta: modo monitor desligado" e não faz nada.
- O vídeo continua normal enquanto apagado (acender é instantâneo).
- O estado volta a "aceso" sozinho quando o vídeo desliga/religa ou a sessão termina.
- Se o atalho já estiver em uso por outro programa: balão + registro; o item do menu continua funcionando.

### 3.3 iPhone
- `SCREEN(1)` com a Segunda Tela ativa: capa preta por cima do vídeo + brilho mínimo (mesmo mecanismo do escurecer do Modo Descanso); o vídeo continua decodificando por baixo.
- 1 toque ou `SCREEN(0)`: acende na hora e restaura o brilho; o toque que acende não passa para baixo; se foi toque, manda `SCREEN(0)` ao PC.
- `VIDEO_END`, desconexão ou volta ao Descanso: a capa some; o Descanso começa com a contagem de 60 s zerada.
- `SCREEN` sem Segunda Tela ativa: ignorado.

## 4. Erros

| Situação | Comportamento |
|---|---|
| Tarefa "Ligar" falha ou o monitor não aparece em 20 s | Registra o tempo e o motivo; abre em Duplicar (como sem driver) |
| Tarefa "Desligar" falha | Registra; o monitor fica fora do desktop (como na 5b) |
| Tarefas ausentes | Comportamento da 5b + balão único pedindo para rodar o instalador |
| Atalho Win+Shift+[ ocupado | Balão + registro; item do menu funciona |
| `SCREEN` fora da Segunda Tela | iPhone ignora; PC não manda |

## 5. Testes

- **PC (xUnit):** `VirtualDeviceSwitch` com um agendador falso (dispara, espera aparecer/sumir, limites de tempo, nunca lança); fábrica de vídeo liga o dispositivo antes do `Attach` e desliga depois do `Detach`; abertura do TelaDois desliga sobra; protocolo `SCREEN` (bytes); estado da tela preta (atalho só com vídeo, reset ao desligar, `SCREEN(0)` vindo do iPhone).
- **Pacote Swift:** bytes do `SCREEN`, `LinkSessionState` não responde a ele.
- **UI (CI):** o teste age como o PC: `CONFIG` + vídeo, `SCREEN(1)` → capa preta (print **`08-tela-preta`**, conferindo brilho baixo), toque → acende e o iPhone manda `SCREEN(0)`.
- **Driver real (local, `TELADOIS_LOCAL_DRIVER_TEST=1`):** desligar o dispositivo pela tarefa → o monitor some de `Win32DisplayApi.Devices()`; ligar → volta e entra na DXGI; anotar os tempos.
- **No iPhone (critério de pronto):** (1) com o modo monitor desligado, Configurações → Tela mostra só os 2 monitores físicos; (2) ligar pelo atalho/cabo: o monitor aparece e o vídeo chega (anotar o tempo); (3) Win+Shift+[ apaga o iPhone e o monitor continua usável no Windows; toque no iPhone acende e a marcação da bandeja desmarca; (4) reiniciar o Windows com o modo desligado: o "3" não volta.
