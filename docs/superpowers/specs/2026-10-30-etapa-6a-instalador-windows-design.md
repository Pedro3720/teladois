# Etapa 6a — Instalador do Windows (driver + app) — Especificação de Design

- **Data:** 2026-10-30
- **Status:** desenho aprovado em conversa; aguardando revisão do documento
- **Contexto:** "Melhoria B — Kit de instalação" de `melhorias/melhorias-planejadas.html`. App do PC e instalador do driver prontos no branch `etapa-5b-monitor-virtual` (Etapas 5a–5d). Branch desta etapa: `etapa-6a-instalador`, criado a partir dele.
- **Decisões do usuário (2026-10-30):** instalador `.exe` com assistente; FFmpeg **dentro** do instalador; "Abrir com o Windows" **marcado** por padrão; driver USB da Apple, se faltar, instalado pelo **winget**.

## 1. Objetivo

Um PC Windows novo fica pronto para a Segunda Tela com **um único arquivo**: `TelaDois-Setup-<versão>.exe`, baixado da página de Releases do GitHub. Dois cliques, um pedido de administrador, e no fim o TelaDois está na bandeja com o monitor virtual instalado, o FFmpeg junto, atalho na Área de Trabalho e abrindo com o Windows. A desinstalação, em Configurações → Aplicativos, desfaz tudo.

### Fora do escopo
- Assinatura de código (certificado pago). O Windows mostra "editor desconhecido" / SmartScreen na primeira vez; o guia explica "Mais informações → Executar assim mesmo".
- Atualização automática do app (o usuário baixa e roda o instalador novo por cima).
- Instalar o app do iPhone (continua pelo xtool, `docs/INSTALACAO.md`).
- Android.

## 2. Peças

| Peça | Origem | Como entra no instalador |
|---|---|---|
| `TelaDois.exe` | `windows/src/TelaDois.Tray` | `dotnet publish` self-contained, win-x64, arquivo único (não exige .NET instalado) |
| Ícone `TelaDois.ico` | `docs/design/icone-1024.png` | gerado uma vez (16–256 px) e versionado em `windows/src/TelaDois.Tray/TelaDois.ico`; vira ícone do `.exe`, da bandeja, do atalho e do instalador |
| `ffmpeg.exe` + licença | FFmpeg Gyan "essentials" 9.x (AMF, QSV, NVENC, MF, x264; ≥ 6 para o `-stats_mux_pre` da 5d) | baixado pelo CI com **versão e SHA-256 fixos**; vai para `<app>\ffmpeg\` com `LICENSE` e um `LEIA-ME.txt` com o link do código-fonte (GPL) |
| Virtual Display Driver 25.7.23 | `VirtualDisplayDriver-x86.Driver.Only.zip` (o mesmo de hoje) | baixado pelo CI com o SHA-256 que já está em `instalar-vdd.ps1`; vai para `<app>\driver\` |
| `instalar-vdd.ps1`, `desinstalar-vdd.ps1`, `vdd_settings.xml` | `tools/vdd/` | copiados para `<app>\driver\` |
| `verificar-apple.ps1` | `tools/instalador/` (novo) | copiado para `<app>\tools\` |
| Driver USB da Apple | iTunes desktop (winget `Apple.iTunes`, fonte `winget`) | **não** vai dentro (licença da Apple); instalado na hora, se faltar |

Pasta de instalação: `C:\Program Files\TelaDois` (`{autopf}`), porque o instalador já pede administrador por causa do driver.

## 3. Mudanças no código

### 3.1 App do PC (C#)
- **`FfmpegLocator.Find`** passa a procurar primeiro em `<pasta do TelaDois.exe>\ffmpeg\ffmpeg.exe` e só depois no `PATH`. Assinatura testável: `Find(string? path = null, string? appDirectory = null)` (padrão `AppContext.BaseDirectory`). Testes xUnit: acha o da pasta do app antes do PATH; sem ele, cai no PATH; sem nenhum, `null`.
- **Ícone:** `TelaDois.Tray.csproj` ganha `<ApplicationIcon>TelaDois.ico</ApplicationIcon>`; a bandeja usa o ícone do próprio `.exe` (`Icon.ExtractAssociatedIcon(Environment.ProcessPath)`) em vez de `SystemIcons.Application`.
- **Versão:** `Directory.Build.props` ganha `<Version>` (padrão `0.6.0`; o CI passa `-p:Version=` a partir da tag). O registro grava `TelaDois <versão> iniciado` (é por ele que se confere qual versão está rodando); o menu da bandeja não muda.
- **Publicação:** `TelaDois.Tray.csproj` ganha um perfil de publicação (`RuntimeIdentifier=win-x64`, `SelfContained=true`, `PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`, `EnableCompressionInSingleFile=true`) usado pelo script de montagem.

### 3.2 Driver (`tools/vdd/`)
- **`instalar-vdd.ps1`**: parâmetro opcional `-DriverZip <caminho>` — usa o zip local em vez de baixar, **conferindo o mesmo SHA-256 e a mesma assinatura**. Parâmetro opcional `-LogPath <arquivo>` (`Start-Transcript`). Sem parâmetros, o comportamento atual não muda.
- **`desinstalar-vdd.ps1`** (novo, `#Requires -RunAsAdministrator`): remove as tarefas `\TelaDois\Ligar monitor virtual` e `\TelaDois\Desligar monitor virtual` e a pasta `\TelaDois` do Agendador; remove o dispositivo `Root\MttVDD` (`pnputil /remove-device`); remove o pacote do driver (`pnputil /delete-driver <oemNN.inf> /uninstall /force`, achado pelo nome original `MttVDD.inf` em `pnputil /enum-drivers`); apaga `C:\VirtualDisplayDriver`. Cada passo ignora "já não existe"; termina com código 0 se não sobrou nada.

### 3.3 Apple (`tools/instalador/verificar-apple.ps1`, novo)
1. Existe o serviço `Apple Mobile Device Service`? → sai com 0 ("já instalado").
2. Senão, se o `winget` existe: `winget install --id Apple.iTunes -e --source winget --accept-package-agreements --accept-source-agreements`. Serviço apareceu → 0.
3. Senão (sem winget, sem internet ou falhou): abre `https://www.apple.com/itunes/download/win64` no navegador e sai com 2 ("instale à mão").
O instalador mostra uma mensagem conforme o código; nunca falha a instalação inteira por isso.

### 3.4 Instalador (`installer/TelaDois.iss`, Inno Setup 6)
- Idioma: Português (Brasil). `PrivilegesRequired=admin`, `ArchitecturesAllowed=x64compatible`, `MinVersion=10.0.19041` (Windows 10 2004+, exigido pelo driver), `Compression=lzma2/ultra64`, `SolidCompression=yes`, `AppId` fixo (para atualizar por cima), `SetupIconFile` e imagens do assistente geradas do ícone.
- `CloseApplications=force` + passo próprio: antes de copiar, encerra `TelaDois.exe` e os `ffmpeg.exe` da pasta do app.
- **Tarefas (caixas):** "Criar atalho na Área de Trabalho" ✅; "Abrir o TelaDois junto com o Windows" ✅ (`HKLM\Software\Microsoft\Windows\CurrentVersion\Run`, valor `TelaDois`, apagado na desinstalação); "Instalar o monitor virtual (Segunda Tela estendida)" ✅.
- **Depois de copiar** (página de progresso com o texto de cada passo):
  1. Monitor virtual: `powershell -NoProfile -ExecutionPolicy Bypass -File "{app}\driver\instalar-vdd.ps1" -DriverZip "{app}\driver\VirtualDisplayDriver-x86.Driver.Only.zip" -LogPath "{app}\logs\instalar-vdd.log"`. Código ≠ 0 → aviso: "O monitor virtual não pôde ser instalado (o registro está em …). O TelaDois funciona duplicando um monitor; rode o instalador de novo para tentar outra vez." A instalação continua.
  2. Apple: `verificar-apple.ps1`. Código 2 → aviso: "Instale o iTunes da página que abrimos para o PC reconhecer o iPhone."
- **Página final:** "Abrir o TelaDois agora" ✅ (executa como o usuário que abriu o instalador: `runasoriginaluser`).
- **Desinstalação:** encerra `TelaDois.exe`/`ffmpeg.exe`, roda `desinstalar-vdd.ps1`, apaga o valor do `Run`, os arquivos de `{app}` (inclusive `logs`). Não apaga `%LOCALAPPDATA%\TelaDois` (preferências e registro do usuário).

### 3.5 Montagem (`installer/montar.ps1`, novo)
Script único usado pelo CI e localmente:
1. `dotnet publish` do Tray (perfil da §3.1) com a versão recebida → `installer/out/app/`.
2. Baixa (cache em `installer/cache/`, ignorado pelo git) o zip do driver e o do FFmpeg; confere os SHA-256 fixos no próprio script; extrai só `ffmpeg.exe` + `LICENSE`.
3. Chama `ISCC.exe` (procura no `PATH`, em `%ProgramFiles(x86)%\Inno Setup 6` e em `%LOCALAPPDATA%\Programs\Inno Setup 6`) com `/DAppVersion=<versão>` → `installer/out/TelaDois-Setup-<versão>.exe` e o `.sha256` ao lado.

### 3.6 GitHub
- **Workflow `windows-installer.yml`** (`windows-2025`): em push de tag `v*`, em push nos branches `etapa-*` que mexem em `windows/**`, `installer/**`, `tools/vdd/**`, `tools/instalador/**` e manual. Passos: `setup-dotnet` (pelo `global.json`) → `dotnet test` → instala o Inno Setup (`choco install innosetup` se o `ISCC` não estiver no runner) → `installer/montar.ps1` → artefato `TelaDois-Setup`. Em tag: cria a **Release** com o `.exe` e o `.sha256` (`gh release create`, marcada como pré-lançamento enquanto a tag não estiver no `main`).
- **README:** seção "Instalar no PC" com o link `https://github.com/Pedro3720/teladois/releases/latest`.
- **`docs/INSTALACAO.md`:** seção nova "No PC (Windows)" — baixar, aviso do SmartScreen, o que o instalador faz, como desinstalar, onde ficam os registros.

## 4. Experiência (revisão apple-design)
- Textos curtos, no idioma do usuário, dizendo o resultado e não o mecanismo ("Instalando o monitor virtual…", não "Executando pnputil").
- Uma decisão por tela; padrões já certos (as três caixas marcadas) para quem só clica "Avançar".
- Imagens do assistente com o ícone do TelaDois sobre fundo escuro (tokens de `docs/design/tokens.md`), sem texto embutido na imagem.
- Avisos só quando o usuário precisa agir; nenhum erro técnico na tela (vai para o registro).
- Bandeja com o ícone do app (reconhecível entre os outros ícones).

## 5. Tratamento de erros

| Situação | Comportamento |
|---|---|
| Windows anterior ao 10 2004 ou 32 bits | Inno recusa antes de começar, com mensagem |
| TelaDois ou FFmpeg abertos | Encerrados antes de copiar |
| Controle de Aplicativos bloqueia o PowerShell (modo restrito) | `instalar-vdd.ps1` sai ≠ 0 com a mensagem atual; instalador avisa e segue (app duplica um monitor) |
| SHA-256 ou assinatura do driver não conferem | Nada do driver é instalado; aviso; app instalado mesmo assim |
| Sem internet / sem winget para o iTunes | Abre a página da Apple; aviso |
| Windows pede reinício depois do driver | `instalar-vdd.ps1` sai com 3010; o Inno oferece "Reiniciar agora / depois" na página final (nunca reinicia sozinho) |
| Instalar por cima de versão anterior | Mesmo `AppId`: substitui os arquivos, roda o `instalar-vdd.ps1` de novo (idempotente), mantém as preferências |

## 6. Testes e verificação
- **xUnit:** `FfmpegLocator` (pasta do app antes do PATH). `windows-test` verde.
- **CI:** `windows-installer` verde e artefato com o `.exe` e o `.sha256`.
- **Neste PC (`D:\Projetos\TelaDois`, AMD, sem driver nem iTunes conferidos):** instalar com o `.exe` do CI — conferir: bandeja com o ícone novo, `C:\Program Files\TelaDois\ffmpeg\ffmpeg.exe` usado (registro), dispositivo `Root\MttVDD` desativado e as 2 tarefas criadas, valor `Run`, atalho. Desinstalar — conferir que nada sobra (tarefas, dispositivo, `C:\VirtualDisplayDriver`, `Run`, pasta). Instalar de novo por cima.
- **Com o iPhone (quando o usuário puder):** Segunda Tela estendida funcionando com o app instalado (não o da pasta `builds/`).
- **Revisão apple-design** das telas do assistente (prints de cada página).

## 7. Riscos
- **SmartScreen** bloqueia `.exe` sem assinatura baixado da internet; mitigação: guia com os cliques, SHA-256 publicado.
- **Licença do FFmpeg (GPL):** redistribuir o binário exige oferecer o código; mitigação: `LICENSE` + link da fonte exata da versão da Gyan junto do `ffmpeg.exe`.
- **winget `Apple.iTunes`** pode mudar de fonte/ID; mitigação: confirmar o ID no plano e cair na página da Apple.
- **Inno Setup no runner** pode não vir instalado; mitigação: `choco install innosetup` com versão fixa.
- **PC do trabalho (Controle de Aplicativos):** o próprio `.exe` sem assinatura pode ser bloqueado; não há como contornar sem assinatura — anotar no guia.
