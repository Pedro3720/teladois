## Instalar no PC (Windows 10 2004 ou mais novo, 64 bits)

1. Baixe o **`TelaDois-Setup-….exe`** abaixo e abra.
2. Se aparecer **"O Windows protegeu o computador"**: clique em **Mais informações → Executar assim mesmo** (o instalador não tem assinatura paga).
3. Aceite o pedido de administrador e clique em **Instalar** — as opções já vêm marcadas.
4. Se o Windows perguntar **"Deseja instalar este software de dispositivo?"** (fornecedor **SignPath Foundation**, é o monitor virtual), clique em **Instalar**.

O instalador põe o TelaDois, o FFmpeg e o monitor virtual; se faltar o driver USB da Apple, instala só o driver (não o iTunes). Para tirar: Configurações → Aplicativos → TelaDois → Desinstalar.

Conferir o arquivo (opcional): `Get-FileHash .\TelaDois-Setup-….exe` deve dar o mesmo valor do `.sha256`.

## iPhone

O app do iPhone continua sendo instalado pelo xtool: veja https://github.com/Pedro3720/teladois/blob/{{TAG}}/docs/INSTALACAO.md#no-iphone.

O app do iPhone tem que ser da **mesma versão** do instalador do PC: use o artefato `TelaDois-ipa` da execução do `ios-build` da tag `{{TAG}}` (aba Actions; é preciso estar logado no GitHub, e o artefato expira em 90 dias).
