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
- Antes de entregar um `.ipa` ao usuário, inspecione-o com Python isolado (`python -I`, `plistlib` no `Info.plist`; cabeçalho Mach-O arm64 e ausência de `LC_CODE_SIGNATURE`), extraindo numa pasta vazia do scratchpad. O REA não inspeciona artefatos Apple (`inspect_plist`/`inspect_macho`) no Windows; continua útil para binários Windows (ex.: driver de monitor virtual). Atenção: `python3` no PATH pode ser o stub da Microsoft Store — use o interpretador real (`py -3`).
- O `.ipa` entregue ao usuário deve vir de um commit cujas execuções `ios-test` **e** `ios-build` estão verdes.
- Armadilha dos prints: `XCUIScreen.main.screenshot()` vem em retrato com `imageOrientation`; o export do xcresult ignora isso, por isso o teste redesenha a imagem com `UIGraphicsImageRenderer` antes de anexar (ver `ScreenshotHelpers.swift`).
- A partir da Etapa 1, trabalhar em branch e só juntar ao `main` com CI verde.
- Quando houver código suficiente, use o `graphify` para navegar no repositório com menos tokens.

## Estrutura
- `ios/` — app Swift/SwiftUI (XcodeGen)
- `windows/` — app de bandeja C#/.NET (a partir da Etapa 4)
- `prototype/` — protótipo HTML do Descanso + Playwright (Etapa 1)
- `.github/workflows/` — CI
