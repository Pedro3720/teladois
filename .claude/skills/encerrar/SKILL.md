---
name: encerrar
description: Rotina de fim de sessão do TelaDois — atualiza o CLAUDE.md (visão geral, estado atual, ambiente do Claude Code verificado de verdade, handoff), confere segredos, faz commit e push para o GitHub e diz o que falta instalar à mão no outro PC. Use só quando o usuário digitar /encerrar.
disable-model-invocation: true
---

# /encerrar — deixar o projeto salvo e pronto para o outro computador

O usuário trabalha em dois PCs (casa e trabalho) e quer sair sem perder contexto. Fale com ele em português.
Faça os passos na ordem. Se algum passo encontrar um problema de segurança, **pare antes do commit** e avise.

## 0. Antes de começar

- Se houver agentes, sessões ou comandos em segundo plano mexendo no projeto, espere terminarem ou anote no
  "Estado atual" o último commit de cada um e o que falta.
- `git fetch` e `git status -sb`: anote o branch atual, o que está à frente/atrás do `origin` e os arquivos soltos.
  Se o branch estiver atrás do `origin`, faça `git pull --ff-only` antes de editar (nunca `--force`, nunca rebase de algo já enviado).

## 1. CLAUDE.md (raiz do repositório)

Crie ou atualize, mantendo o que já existe e está certo:

- **Visão geral:** objetivo, stack, estrutura de pastas e como rodar localmente (confira os comandos contra os arquivos reais:
  `ios/project.yml`, `.github/workflows/`, `windows/`, `prototype/package.json`).
- **Regras / convenções** e **Decisões técnicas (e por quê)**: acrescente as decisões tomadas nesta sessão, cada uma com o motivo.
- **Estado atual (atualizado em AAAA-MM-DD):** pronto / em andamento (com o último commit e o que falta) / pendências adiadas
  pelo usuário / bloqueios / próximos passos. Esta seção é a fonte da verdade entre os PCs: escreva para quem chega sem contexto.
- **Variáveis de ambiente:** só os NOMES, nunca os valores.

Atualize também a linha da etapa em `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8) se uma etapa mudou de estado.

## 2. Seção "Ambiente do Claude Code" no CLAUDE.md — verificar de verdade

Não deduza pelo histórico da conversa: leia os arquivos e rode os comandos. Neste PC (Windows):

| O quê | Onde olhar |
|---|---|
| Programas | `winget list`, `Get-Command git,gh,node,py,dotnet,uv,claude,graphify`, `wsl -l -v`, WSL: `xtool --help` |
| Plugins | `claude plugin list`, `~/.claude/settings.json` (`enabledPlugins`, `extraKnownMarketplaces`), `~/.claude/plugins/` |
| Skills | `~/.claude/skills/*` (pessoais), `.claude/skills/*` (projeto), e as que vêm de plugins |
| MCP servers | `~/.claude.json` (`mcpServers`), `.mcp.json` do projeto, `claude mcp list` |
| Hooks/config | `~/.claude/settings.json`, `.claude/settings.json`, `~/.wslconfig` |

Registre:

- **Plugins:** nome, marketplace de origem e os comandos exatos (`/plugin marketplace add …` e `/plugin install …`).
- **Skills:** separe plugins / pessoais (`~/.claude/skills`) / projeto (`.claude/skills`). Para cada pessoal, diga se vale mover
  para `.claude/skills` (vai pelo Git) ou não (por exemplo, depende de algo instalado globalmente).
- **MCP servers:** nome, para que serve e como configurar — **sem tokens, chaves ou senhas**.
- **Programas e passos manuais** (WSL, virtualização na BIOS, xtool + login do Apple ID, Sideloadly, iTunes versão web…).
- Marque o que mudou desde a última versão e o que **ainda falta no outro PC**.

## 3. Handoff entre computadores

Mantenha a seção "Handoff entre computadores" com o checklist (ajuste se algo mudou):

- **Antes de sair:** rodar `/encerrar` (atualiza o "Estado atual", commit e `git push`).
- **Ao chegar no outro PC:** `git pull` (e `git switch <branch da etapa>` + `git pull`), ler o CLAUDE.md inteiro antes de começar,
  `gh auth status`, instalar o que a seção "Ambiente do Claude Code" marcar como faltando.

## 4. Segurança (antes de qualquer commit)

- Confira que `.gitignore` cobre `.env`, `.env.*`, `*.pem`, `*.key`, `*.p12`, `*.cer`, `*.mobileprovision`, `.claude/settings.local.json`.
- Procure segredos no que **vai** e no que **já está** versionado:
  `git ls-files | rg -i "\.env$|\.pem$|\.key$|\.p12$|\.mobileprovision$"` e
  `git grep -nIE "(api[_-]?key|secret|token|password|senha)\s*[:=]\s*['\"][^'\"]{8,}" -- . ":!*.md"`
  e o mesmo `rg` nos arquivos novos/alterados (`git diff --cached`, arquivos não rastreados).
- Arquivos soltos que não devem ir para o Git (logs, `builds/`, prints locais, `*.log`): não adicione; sugira incluí-los no `.gitignore`.
- **Se encontrar algo exposto: pare, mostre ao usuário o arquivo e a linha e espere a decisão antes de fazer commit.**

## 5. Commit e push

- Atualize o grafo do graphify antes do commit (só AST, sem custo de tokens):
  `uv tool run --from graphifyy python -m graphify update .` e inclua `graphify-out/graph.json`,
  `graphify-out/GRAPH_REPORT.md` e `graphify-out/manifest.json` no commit, para o outro PC já chegar com o grafo certo.
  Se documentos (`.md`, workflows) mudaram muito, avise que vale rodar `/graphify . --update` para reextrair o texto.
- Adicione só o que é do projeto (nunca `git add -A` às cegas: revise `git status` e adicione por caminho).
- No Windows, mensagem com acento vai num arquivo em `.git/` e `git commit -F <arquivo>` (o PowerShell estraga UTF-8);
  apague o arquivo depois. A mensagem descreve o que mudou e termina com
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- `git push` do branch atual. Nunca `--force`.
- **Não junte ao `main`** só para encerrar: isso só acontece com `ios-test`, `ios-build` e `windows-test` verdes no SHA final
  (regra do CLAUDE.md). Se o trabalho está num branch `etapa-*`, ele fica salvo no GitHub nesse branch — diga o nome no resumo.

## 6. Resumo para o usuário

Mostre, curto:

1. Commit e branch enviados (e o link do commit no GitHub).
2. O que mudou no CLAUDE.md (seções atualizadas, decisões novas).
3. Resultado da checagem de segurança.
4. **O que precisa de ação manual no outro PC** (lista numerada, com o comando ou o caminho de cada item).
5. Qualquer coisa que ficou de fora (arquivos soltos, CI ainda rodando, branch não juntado ao `main`).
