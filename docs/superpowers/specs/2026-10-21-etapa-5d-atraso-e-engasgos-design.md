# Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design

- **Data:** 2026-10-21
- **Status:** desenho aprovado em conversa (Partes 1–3); aguardando revisão do documento
- **Branch:** `etapa-5b-monitor-virtual` (contém 5a, 5b e 5c; nada foi para o `main`)
- **Contexto:** queixas do usuário no iPhone (2026-10-20, PC do trabalho, Intel QSV, monitor virtual 2556×1179@60): atraso ao começar a mexer o mouse depois de parado; a rolagem de sites ainda engasga um pouco; num podcast o som (que sai no PC) vem antes da boca no iPhone. Medido com os medidores `STATS`/`vídeo/s` (Etapa 5b): fila no PC 1 ms, escrita no cabo 1 ms, ida e volta 3 ms, iPhone recebendo→decodificando 2 ms, 0 descartes no iPhone; a captura às vezes cai para 20–49 imagens/s. Descartado: buffer do stdout do FFmpeg (1 leitura por imagem). **Não medidos:** captura + compressão no PC e decodificação até a imagem aparecer no iPhone.

## 1. Decisões do usuário

| # | Decisão |
|---|---|
| 1 | 5d (atraso e engasgos) antes da 5e (atrasar o som só do programa no iPhone, decidida depois com o número que sobrar). |
| 2 | O som continua saindo no PC, não no iPhone. |
| 3 | Uso principal: vídeo e podcast; rolar sites também precisa ficar bom. |
| 4 | **PC principal: o do trabalho (Intel, QSV)** — é nele que o ajuste fino acontece. A AMD (PC de casa) só precisa continuar funcionando. |
| 5 | Medir com uma **bancada automática no PC** e confirmar no fim com 1 foto do cronômetro (PC + iPhone juntos) e o uso normal. |
| 6 | Pode ceder **um pouco de nitidez** e **resolução de compressão menor** (o usuário usa escala de 175% no Windows; o texto continua legível). Não aceita só "manter tudo". |

## 2. Bancada de medição (ferramenta, não vai no TelaDois)

Projeto de console `windows/tools/TelaDois.LatencyBench/` (na solução, sem ir para a bandeja), que usa as mesmas peças do app (`EncoderProfiles`, `FfmpegArguments`, `AccessUnitReader`, `DisplayCatalog`, `SystemProcessRunner`).

1. **Marcador:** abre uma janela sem borda, sempre por cima, 64×64 pixels, num canto do monitor capturado, que troca de cor a cada imagem (60/s) seguindo um código ligado ao relógio do PC (`Stopwatch`/QPC): a cor codifica o número da troca (por exemplo 4 blocos 32×32 preto/branco = 4 bits, mais blocos se precisar), e a bancada guarda a hora de cada troca.
2. **Captura real:** roda a mesma linha do FFmpeg do TelaDois para o perfil e a saída escolhidos e lê o `pipe:1` com o `AccessUnitReader`, anotando a hora de chegada de cada imagem comprimida.
3. **Decodificação só para medir:** um segundo FFmpeg (`-f h264 -i pipe:0 … -vf crop=<marcador> -f rawvideo pipe:1`) descomprime as mesmas imagens, na mesma ordem, e a bancada lê o código do marcador em cada uma. Atraso de cada imagem = chegada − hora da troca daquela cor (a decodificação não entra na conta: a hora de chegada já foi anotada antes).
4. **Cenários (cada um ~15 s):**
   - **parado → muda:** tela parada 2 s, depois o marcador muda (o "mouse parado que começa a mexer");
   - **rolagem:** uma faixa de texto de ~1/3 da tela rolando continuamente;
   - **vídeo:** uma área grande (~1/2 da tela) com conteúdo mudando a cada imagem.
5. **Relatório por configuração** (tabela no console e um `.csv` em `builds/bancada/`): atraso mediano, p95 e máximo; imagens por segundo; buracos (intervalos entre imagens > 33 ms e > 50 ms); tamanho médio e máximo das imagens completas; taxa de dados (Mbit/s); e um `.png` de uma imagem decodificada do cenário "rolagem" para conferir a legibilidade do texto.
6. **Monitor:** por padrão mede no monitor virtual (liga e desliga pelo `VirtualDisplay`, igual ao app, resolução do iPhone 2556×1179); opção `--monitor \\.\DISPLAYn` para medir num monitor físico (PC de casa, enquanto o erro 1167 não for resolvido). Nunca mexe em monitor físico além de abrir a janela do marcador.

## 3. Experimentos (Intel QSV, PC do trabalho)

Cada ajuste é medido sozinho contra a **base** (perfil atual `qsv-hwmap`: `hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw…:ch…`, `-low_power 0 -look_ahead 0 -async_depth 1`, `-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M`); depois combinam-se os que ajudaram.

| Ajuste | Esperado | Custo |
|---|---|---|
| `-low_power 1` e/ou `-preset veryfast` | menos tempo por imagem | um pouco de nitidez |
| Taxa constante com folga pequena: `-maxrate` = `-b:v`, `-bufsize` ≈ 1 imagem (`b:v / 60`) | sem picos que atrasam a próxima imagem | nitidez em movimento |
| *Intra-refresh* (QSV `-int_ref_type vertical`, ciclo ~1 s) em vez de IDR grande a cada 2 s | some o soluço periódico | recuperar de erro leva ~1 s |
| `-g 240` (IDR a cada 4 s) | menos picos | idem |
| `ddagrab` com `dup_frames=0` (não repete imagem com a tela parada) | a 1ª imagem depois de parado sai mais cedo | nenhum (o iPhone aceita pausas; o `PING` mantém a conexão) |
| `hwmap` × `download` | escolher o caminho mais rápido | nenhum |
| Comprimir menor pela placa (`vpp_qsv=w=…:h=…`): 1920×886 e 1704×786 (2/3), sempre pares; o monitor do Windows continua 2556×1179 e o iPhone amplia | menos pixels para comprimir/decodificar, imagens menores | texto menos nítido (conferir no `.png`) |

**Regra de escolha:** vence a configuração com menor atraso mediano **sem piorar** o p95 nem aumentar os buracos; entre parecidas, a de maior nitidez (resolução maior, taxa maior). Resolução menor só entra se ganhar de verdade (≥ 5 ms de mediana ou buracos a menos).

**Entrega no app:** a configuração vencedora vira o perfil padrão da Intel (`EncoderProfiles`/`FfmpegArguments`), com testes atualizados; o `CONFIG` continua levando o tamanho **codificado** (se a compressão for menor, o iPhone recebe esse tamanho e amplia com aspecto). **AMD:** só ajustes equivalentes seguros (taxa constante com folga pequena; `-usage ultralowlatency` já existe), sem afinar; os dois perfis AMF continuam passando na sondagem. Os outros perfis (`nvenc`, `mf`, `x264`) não mudam.

## 4. iPhone

O caminho atual (`AVSampleBufferDisplayLayer`, decodificação por hardware, exibir imediatamente) costuma custar 1–2 imagens (~16–35 ms) e só dá para medir com câmera. Estimativa: **foto ponta a ponta − bancada − ~3 ms do cabo**. Se passar de ~35 ms, uma rodada seguinte (fora desta etapa) troca por `VTDecompressionSession` + desenho direto (`CAMetalLayer`).

## 5. Metas e critério de pronto

- **Bancada (captura + compressão), configuração vencedora:** mediana ≤ 35 ms, p95 ≤ 60 ms, nenhum buraco > 50 ms no cenário rolagem, ≥ 58 imagens/s nos cenários rolagem e vídeo.
- **Ponta a ponta (foto do usuário):** cronômetro com milissegundos no monitor do iPhone; uma foto mostrando PC e iPhone juntos; meta ≤ ~80 ms (acima disso o som adiantado incomoda; abaixo de ~45 ms quase não se nota). O número decide se a 5e é necessária e com qual atraso.
- **Uso:** o usuário confirma mouse parado → mexer, rolagem de site e podcast (boca × som).
- Testes do PC e do iPhone verdes, zero avisos; `windows-test`, `ios-test` e `ios-build` verdes no SHA final.

## 7. Monitor do iPhone em 100% com resolução menor (decisão do usuário, 2026-10-21)

**Problema medido:** com o monitor virtual em 2556×1179 e escala 175%, o programa de trabalho do usuário (GO-Global 6.2.2 da GraphOn, cliente de 32 bits sem suporte a DPI por monitor, que mostra o Domínio Web em janelas "seamless") fica preso no monitor pequeno: arrastado para o principal, volta. Reabrir o programa com o monitor do iPhone ligado não resolve. **Causa confirmada pelo usuário:** com o monitor do iPhone em escala 100%, o programa anda por todas as telas — o problema é a **escala misturada** entre monitores.

**Decisão:** o monitor virtual passa a ter a resolução que o usuário enxerga hoje em 175%, mas em **escala 100%**: tamanho = resolução do iPhone ÷ 1,75, arredondado para o par mais próximo. O texto fica do mesmo tamanho, o Windows não mistura escalas e cada imagem tem ~1/3 dos pixels (menos atraso e engasgo — a bancada mostrou que comprimir menor tira os picos).

| iPhone (paisagem) | Monitor virtual (100%) |
|---|---|
| 2556×1179 (15/16, 15/16 Pro) | **1460×674** |
| 2796×1290 (15/16 Plus/Pro Max) | 1598×738 |
| 2622×1206 (16 Pro) | 1498×690 |
| 2868×1320 (16 Pro Max) | 1638×754 |
| 2736×1260 (17) | 1564×720 |

- O `vdd_settings.xml` ganha esses modos (a 60 Hz); o instalador precisa rodar de novo (administrador) em cada PC.
- O TelaDois liga o monitor no modo calculado a partir do `HELLO` (iPhone ÷ 1,75, par). O `CONFIG` leva esse tamanho; o iPhone amplia com aspecto (≈1,75×), como já faz.
- A escala do monitor virtual no Windows tem de ser **100%**. O Windows guarda a escala por monitor (`HKCU\Control Panel\Desktop\PerMonitorSettings`); o usuário já pôs 100% neste PC. O TelaDois registra no log a escala efetiva do monitor ao ligar e, se não for 100%, mostra um balão uma vez explicando como mudar (Configurações → Tela → monitor do iPhone → Escala 100%).
- A bancada mede também nesse modo (rodada 2) para escolher a compressão nele.

## 6. Fora desta etapa

- 5e — atrasar o som só do programa que está no iPhone (desenho já discutido; depende do número final).
- Erro 1167 do PC de casa (tarefa agendada com `pnputil` falha nos dois sentidos) e a Task 9 da 5c.
- Trocar a exibição do iPhone por `VTDecompressionSession` + `CAMetalLayer` (só se a conta do §4 pedir).
