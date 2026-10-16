# Instalar o TelaDois no iPhone (sem conta paga)

Este guia usa o **Sideloadly** no Windows e um **Apple ID grátis**.

## Uma vez só

1. **iTunes e iCloud: use as versões "web", do site da Apple.**
   O Sideloadly só funciona com o iTunes e o iCloud instalados pelos instaladores do site da Apple, **não** com as versões da Microsoft Store.
   - Se você já tem **iTunes**, **iCloud** ou **Dispositivos Apple** instalados pela Microsoft Store, **desinstale antes** (Configurações → Aplicativos).
   - Os links corretos dos instaladores ficam na seção de requisitos de https://sideloadly.io (não baixe de outro lugar).
2. **Instale o Sideloadly**: https://sideloadly.io (versão Windows).
3. **Conecte o iPhone** ao PC com um cabo que transmita dados (alguns cabos só carregam).
   - Desbloqueie o iPhone, toque em **Confiar neste computador** e digite o código do iPhone.
   - Mantenha o iPhone desbloqueado até ele aparecer no Sideloadly.

## A cada nova versão

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

## Validade

- Com Apple ID grátis, o app **para de abrir após 7 dias**. Para renovar, repita "A cada nova versão" **com o mesmo Apple ID** (os dados do app são mantidos).
- Limites da conta grátis: até **3 apps** instalados assim ao mesmo tempo e **10 App IDs por semana**.
- Alternativa: o Sideloadly tem **renovação automática** (veja as opções do próprio Sideloadly).
