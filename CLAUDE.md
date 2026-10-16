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
