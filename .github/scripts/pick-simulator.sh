#!/usr/bin/env bash
# Imprime o UDID do simulador de iPhone a usar nos testes.
# Estabilidade dos prints: usa o runtime iOS igual ao SDK do Xcode fixado (ex.: 18.5) e o modelo
# "iPhone 16 Pro". Sem esse par, cai para o primeiro iPhone do runtime mais novo e avisa no log.
set -euo pipefail
PREFERRED_MODEL="iPhone 16 Pro"
SDK_VERSION="$(xcrun --sdk iphonesimulator --show-sdk-version)"
export PREFERRED_MODEL SDK_VERSION
xcrun simctl list devices available -j | python3 -c '
import json, os, re, sys
preferred = os.environ["PREFERRED_MODEL"]
sdk = tuple(int(p) for p in os.environ["SDK_VERSION"].split(".")[:2])
devices = json.load(sys.stdin)["devices"]
def version(key):
    m = re.search(r"iOS-(\d+)-(\d+)", key)
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)
runtimes = sorted((k for k in devices if "iOS" in k), key=version, reverse=True)
for runtime in runtimes:
    if version(runtime) == sdk:
        for d in devices[runtime]:
            if d["name"] == preferred:
                print("simulador: %s (%s) = SDK %s" % (d["name"], runtime, os.environ["SDK_VERSION"]), file=sys.stderr)
                print(d["udid"])
                sys.exit(0)
for runtime in runtimes:
    iphones = [d for d in devices[runtime] if d["name"].startswith("iPhone")]
    if iphones:
        print("::warning::%s no runtime do SDK %s indisponível; usando %s (%s)"
              % (preferred, os.environ["SDK_VERSION"], iphones[0]["name"], runtime), file=sys.stderr)
        print(iphones[0]["udid"])
        sys.exit(0)
sys.exit("nenhum simulador de iPhone disponível")
'
