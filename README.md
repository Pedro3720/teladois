# TelaDois

Transforma o iPhone em **segunda tela do PC (via cabo USB)** e em um **Modo Descanso** estilo StandBy — relógio, clima, bateria e Spotify — sem precisar do carregador.

## Como instalar

São duas partes: o **app do PC** (Windows) e o **app do iPhone**. O PC precisa de Windows 10 (versão 2004) ou mais novo, 64 bits; o iPhone liga no PC por um cabo que transmite dados.

### 1. No PC (Windows) — um instalador só

1. Abra a [página de versões](https://github.com/Pedro3720/teladois/releases), pegue a versão mais nova e baixe o **`TelaDois-Setup-<versão>.exe`**.
2. Abra o arquivo. Se aparecer **"O Windows protegeu o computador"**, clique em **Mais informações → Executar assim mesmo** (o instalador não tem assinatura paga).
3. Aceite o pedido de administrador, deixe as opções marcadas e clique em **Instalar**:
   - **Instalar o monitor virtual** — o iPhone vira uma tela a mais, não uma cópia;
   - **Abrir o TelaDois junto com o Windows**;
   - **Criar atalho na Área de Trabalho**.
4. Quando o Windows perguntar **"Deseja instalar este software de dispositivo?"** (fornecedor *SignPath Foundation*), clique em **Instalar** — é o driver do monitor virtual.
5. Espere alguns minutos e clique em **Concluir**. O TelaDois fica na bandeja, perto do relógio.

O instalador já traz tudo o que o PC precisa:

| Peça | O que acontece |
|---|---|
| TelaDois | vai para `C:\Program Files\TelaDois` (não precisa do .NET instalado) |
| FFmpeg | vem junto, em `C:\Program Files\TelaDois\ffmpeg` |
| Monitor virtual (Virtual Display Driver 25.7.23) | vem dentro do instalador; assinatura e SHA-256 conferidos |
| Driver USB da Apple | se faltar, é baixado pelo winget e só o *Apple Mobile Device Support* é instalado, depois de conferir a assinatura da Apple |

- **Atualizar:** baixe a versão nova e abra por cima; as preferências ficam.
- **Desinstalar:** Configurações → Aplicativos → TelaDois → Desinstalar (remove também o monitor virtual).
- **Conferir o arquivo (opcional):** `Get-FileHash .\TelaDois-Setup-<versão>.exe` deve dar o valor do `.sha256` da mesma página.
- **Deu algo errado?** Os registros ficam em `C:\Program Files\TelaDois\logs\` (instalação) e `%LOCALAPPDATA%\TelaDois\tela-dois.log` (o app).

### 2. No iPhone

O app do iPhone não está na App Store: ele é compilado pelo GitHub Actions sem assinatura e instalado com um **Apple ID grátis** pelo **xtool** (ou pelo Sideloadly). Com o Apple ID grátis o app precisa ser reinstalado a cada 7 dias. Passo a passo em [docs/INSTALACAO.md](docs/INSTALACAO.md#no-iphone).

### 3. Usar

Conecte o iPhone pelo cabo, abra o app **TelaDois** nele e use o atalho **Win+Shift+]** (ou o menu da bandeja) para ligar o modo monitor. **Win+Shift+[** apaga a tela do iPhone; um toque acende de novo.

## Para desenvolver

- Guia completo de instalação: [docs/INSTALACAO.md](docs/INSTALACAO.md)
- Especificação: [docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md](docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md)
- Montar o instalador do Windows localmente: `powershell -ExecutionPolicy Bypass -File installer\montar.ps1 -Version 0.6.0` (precisa do .NET 10 SDK e do Inno Setup 6). O GitHub Actions faz o mesmo a cada envio e publica a versão numa tag `v*`.
