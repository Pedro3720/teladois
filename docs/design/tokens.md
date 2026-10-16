# TelaDois — tokens de design (Etapa 1)

Fonte da verdade visual para o app SwiftUI (Etapa 2). Valores tirados do protótipo aprovado
(`prototype/styles.css`, publicado em https://pedro3720.github.io/teladois/).

## Cores

| Token | Hex | Uso |
|---|---|---|
| `black` | `#000000` | Fundo de tudo e tela escurecida (no OLED, pixel apagado) |
| `clock` | `#7FA9F2` | Dígitos do relógio, dia da semana, destaques |
| `ink` | `#F5F5F7` | Texto principal |
| `muted` | `#8E8E93` | Texto secundário (artista no mini player) |
| `battery` | `#34C759` | Anel e número da bateria |
| `track` | `#2C2C2E` | Trilho do anel da bateria |
| `bars` | `#FF8A65` | Barrinhas animadas do mini player |
| página Música | gradiente das cores da capa | Fundo: radial da cor principal da capa sobre linear para `#120A08` |

## Tipografia

| Papel | Protótipo (web) | App (SwiftUI) |
|---|---|---|
| Relógio | Sofia Sans Extra Condensed 800, esticada 1,5× na vertical | Fonte embutida Sofia Sans Extra Condensed (licença OFL) com o mesmo `scaleEffect(y: 1.5)`, ou SF Pro Rounded comprimida — decidir no primeiro print do simulador |
| Interface | `ui-rounded` (SF Pro Rounded) | `.system(design: .rounded)` |
| Números | algarismos tabulares | `.monospacedDigit()` |

Tamanhos relativos à altura da tela em paisagem (`H`):
- Relógio: `min(26,5% da largura, 68% de H)`, entrelinha 0,78.
- Data e temperatura: 9,5% de H, peso semibold.
- Número da bateria: 6,5% de H; anel com 22% de H de diâmetro e traço 4,5/44.
- Mini player: título 6,2% de H (bold), artista 5% de H.
- Música: capa com 78% de H e cantos de 4,5% de H; título 10,5% de H; artista 7% de H; botões com 17% de H.

## Layout

```
┌────────────────────────────────────────────────────────────┐
│                                   QUA., 14          (80)    │ ← folga ≥ 48 pt à direita e ≥ 24 pt no topo
│   10:51                           23°C                      │   (cantos arredondados da tela)
│   (relógio gigante)                                         │
│                                   ▮▮▮ Música — Artista      │ ← só quando está tocando
│                              • •                             │
└────────────────────────────────────────────────────────────┘
```
- Página 1 = relógio; página 2 = música; deslizar na horizontal com paginação.
- Área segura: margem mínima de 16 pt nas laterais e 12 pt em cima e embaixo, somada às áreas seguras do iOS.

## Comportamento

- Escurecer: 60 s sem toque → tela preta com fade de 1,2 s e brilho 0 (no app). O toque acorda instantaneamente e **não** aciona o botão que estiver embaixo.
- Mini player: aparece só com música tocando; um toque leva à página 2.
- Retrato: mensagem "Vire o iPhone na horizontal" (no app, a orientação já é travada em paisagem).
- Movimento reduzido: sem animação das barrinhas.

## Ícone

- Fonte: `docs/design/icone-1024.png` (Canva, 1024×1024, RGB sem transparência). Design editável no Canva: "TelaDois - ícone do app".
- iOS: `ios/TelaDois/Assets.xcassets/AppIcon.appiconset`. Web: `prototype/icon-180.png` (Tela de Início) e `icon-32.png`.
