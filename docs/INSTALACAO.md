# Instalar o TelaDois (PC e iPhone, sem conta paga)

## No PC (Windows)

1. Baixe o `TelaDois-Setup-<versão>.exe` da versão mais nova em https://github.com/Pedro3720/teladois/releases e abra. Enquanto a versão não estiver no `main`, ela aparece marcada como "Pre-release".
2. **"O Windows protegeu o computador"** (SmartScreen): **Mais informações → Executar assim mesmo**. Aparece porque o instalador não tem assinatura paga. Conferir o arquivo (opcional): `Get-FileHash <arquivo>` igual ao `.sha256` da página.
3. Aceite o pedido de administrador (é para o monitor virtual) e clique em **Instalar**. As três opções já vêm marcadas: monitor virtual, abrir com o Windows e atalho na Área de Trabalho.
4. O instalador:
   - coloca o TelaDois em `C:\Program Files\TelaDois`, com o FFmpeg em `ffmpeg\` (não precisa instalar à parte);
   - instala o monitor virtual (Virtual Display Driver 25.7.23, conferido pelo SHA-256 e pela assinatura) e as tarefas `\TelaDois\` do Agendador. Nessa hora o Windows pergunta **"Deseja instalar este software de dispositivo?"** (fornecedor **SignPath Foundation**): clique em **Instalar**;
   - confere o driver USB da Apple; se faltar, baixa o instalador do iTunes pelo winget e instala só o **Apple Mobile Device Support** (o driver, não o iTunes), depois de conferir a assinatura da Apple. Se não der, abre a página da Apple;
   - abre o TelaDois na bandeja, perto do relógio.
5. **Atualizar:** baixe a versão nova e abra por cima; as preferências ficam.
6. **Desinstalar:** Configurações → Aplicativos → TelaDois → Desinstalar (remove também o monitor virtual).

Registros: `C:\Program Files\TelaDois\logs\` (instalação) e `%LOCALAPPDATA%\TelaDois\tela-dois.log` (o app).

**PC com Controle de Aplicativos** (ex.: do trabalho): o Windows pode bloquear o instalador sem assinatura ou o script do monitor virtual. Sem o monitor virtual, o TelaDois mostra no iPhone uma cópia de um monitor.

## No iPhone

Desde 2026-10-18 o jeito principal é o **xtool**, que roda no Ubuntu do WSL e usa o serviço da Apple do Windows. O **Sideloadly** (mais abaixo) fica como alternativa: naquele dia o login dele na Apple falhou com um erro 404.

### Com o xtool (principal)

**Uma vez só** (já feito no PC do `D:\Downloads\Teladois`):
1. iTunes e iCloud versão "web" (veja abaixo), WSL com Ubuntu e, em `%USERPROFILE%\.wslconfig`:
   ```
   [wsl2]
   networkingMode=mirrored
   ```
2. No Ubuntu: xtool 1.22.2 (AppImage de https://github.com/xtool-org/xtool extraído em `/opt/xtool`, link em `/usr/local/bin/xtool`), `sudo apt install zip unzip` e o arquivo `/etc/profile.d/xtool-usbmuxd.sh` com `export USBMUXD_SOCKET_ADDRESS=127.0.0.1:27015`. O script `tools/wsl/instalar-xtool.sh` faz tudo isso e confere o SHA-256: `wsl -d Ubuntu -- bash "/mnt/<pasta do repositório>/tools/wsl/instalar-xtool.sh"` (ele pede a senha do Ubuntu).
3. `xtool auth login` no Ubuntu, com o seu Apple ID. Você digita a senha e o código de 6 dígitos.
4. Na primeira instalação o xtool pode pedir para **revogar o certificado** antigo (o do Sideloadly). Respondendo `yes`, os apps instalados pelo Sideloadly param de abrir até serem reinstalados.

**A cada nova versão** (iPhone no cabo e desbloqueado), no PowerShell:
```
wsl -d Ubuntu -- bash -lc 'xtool install --usb /mnt/d/Downloads/Teladois/builds/<pasta>/TelaDois.ipa'
```
O `bash -lc` é obrigatório: sem ele o xtool não acha o serviço da Apple e fica em "Waiting for device to be connected...". O fim certo é `Successfully installed!`. Se aparecer "Desenvolvedor não confiável", confie de novo em Ajustes → Geral → VPN e Gerenciamento de Dispositivos.

### Com o Sideloadly (alternativa)

Usa o **Sideloadly** no Windows e um **Apple ID grátis**.

#### Uma vez só

1. **iTunes e iCloud: use as versões "web", do site da Apple.**
   O Sideloadly só funciona com o iTunes e o iCloud instalados pelos instaladores do site da Apple, **não** com as versões da Microsoft Store.
   - Se você já tem **iTunes**, **iCloud** ou **Dispositivos Apple** instalados pela Microsoft Store, **desinstale antes** (Configurações → Aplicativos).
   - Os links corretos dos instaladores ficam na seção de requisitos de https://sideloadly.io (não baixe de outro lugar).
2. **Instale o Sideloadly**: https://sideloadly.io (versão Windows).
3. **Conecte o iPhone** ao PC com um cabo que transmita dados (alguns cabos só carregam).
   - Desbloqueie o iPhone, toque em **Confiar neste computador** e digite o código do iPhone.
   - Mantenha o iPhone desbloqueado até ele aparecer no Sideloadly.

#### A cada nova versão

1. **Pegue o `TelaDois.ipa` mais recente.**
   - O Claude baixa para você; ou
   - GitHub → Actions → `ios-build` → última execução verde → artefato `TelaDois-ipa` (é preciso estar logado no GitHub). Descompacte o `.zip`.
2. **No Sideloadly:** arraste o `TelaDois.ipa`, escolha seu iPhone, digite seu **Apple ID** e clique **Start**.
   - O Sideloadly vai pedir a **senha do Apple ID** e, depois, um **código de verificação de 6 dígitos** que a Apple envia para os seus aparelhos Apple. Digite os dois.
   - Dica opcional: use um **Apple ID secundário** só para isso, já que a senha é digitada numa ferramenta de terceiros.
3. **Na primeira instalação, no iPhone:**
   - **Ajustes → Privacidade e Segurança → Modo Desenvolvedor → ativar.** Essa opção **só aparece depois da primeira instalação**. O iPhone reinicia; depois do reinício, confirme **Ativar** e digite o código do iPhone.
   - **Ajustes → Geral → VPN e Gerenciamento de Dispositivos →** toque no seu Apple ID → **Confiar**. Se você abrir o app antes disso, aparece "Desenvolvedor não confiável".
4. **Abra o TelaDois.**

#### Validade

- Com Apple ID grátis, o app **para de abrir após 7 dias**. Para renovar, repita "A cada nova versão" **com o mesmo Apple ID** (os dados do app são mantidos).
- Limites da conta grátis: até **3 apps** instalados assim ao mesmo tempo e **10 App IDs por semana**.
- Alternativa: o Sideloadly tem **renovação automática** (veja as opções do próprio Sideloadly).
