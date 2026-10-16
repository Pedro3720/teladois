#!/usr/bin/env bash
# Imprime o UDID do simulador de iPhone a usar nos testes.
# Estabilidade dos prints: dentro do runtime iOS mais novo, prefere o modelo nomeado
# "iPhone 16 Pro" (tamanho de tela fixo); se não existir, usa o primeiro iPhone do runtime.
# O modelo escolhido vai para o stderr (aparece no log do CI).
set -euo pipefail
PREFERRED_MODEL="iPhone 16 Pro"
export PREFERRED_MODEL
xcrun simctl list devices available -j | python3 -c '
import json, os, re, sys
preferred = os.environ["PREFERRED_MODEL"]
devices = json.load(sys.stdin)["devices"]
def version(key):
    m = re.search(r"iOS-(\d+)-(\d+)", key)
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)
for runtime in sorted(devices, key=version, reverse=True):
    if "iOS" not in runtime:
        continue
    iphones = [d for d in devices[runtime] if d["name"].startswith("iPhone")]
    if not iphones:
        continue
    chosen = next((d for d in iphones if d["name"] == preferred), iphones[0])
    print("simulador: %s (%s)" % (chosen["name"], runtime), file=sys.stderr)
    print(chosen["udid"])
    sys.exit(0)
sys.exit("nenhum simulador de iPhone disponível")
'
