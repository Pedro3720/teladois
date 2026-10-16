# Instalar o TelaDois no iPhone (sem conta paga)

## Uma vez só
1. Instale o app **Dispositivos Apple** (Microsoft Store) — ou o iTunes. Ele traz o driver USB do iPhone.
2. Instale o **Sideloadly**: https://sideloadly.io (versão Windows).
3. Conecte o iPhone no cabo, desbloqueie e toque em **Confiar neste computador**.

## A cada nova versão
1. Pegue o `TelaDois.ipa` mais recente (o Claude baixa do GitHub Actions para você, ou: GitHub → Actions → `ios-build` → última execução → artefato `TelaDois-ipa`, e descompacte o .zip).
2. Abra o Sideloadly, arraste o `TelaDois.ipa`, escolha seu iPhone, digite seu **Apple ID** e clique **Start**.
3. Na primeira instalação, no iPhone:
   - **Ajustes → Privacidade e Segurança → Modo Desenvolvedor → ativar** (o iPhone reinicia).
   - **Ajustes → Geral → VPN e Gerenciamento de Dispositivos →** toque no seu Apple ID → **Confiar**.
4. Abra o TelaDois.

## Validade
Com Apple ID grátis, o app **para de abrir após 7 dias**. Para renovar, repita o passo 2 (os dados do app são mantidos).
