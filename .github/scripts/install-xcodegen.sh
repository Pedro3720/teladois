#!/usr/bin/env bash
# Instala o XcodeGen em versão fixa, com verificação de integridade (SHA-256).
# Usado por ios-test.yml e ios-build.yml. Para atualizar: troque VERSION e SHA256
# (o hash do release aparece em `gh api repos/yonaskolb/XcodeGen/releases/tags/<versão>`, campo digest).
set -euo pipefail

VERSION="2.46.0"
SHA256="4d9e34b62172d645eed6457cac13fc222569974098ef4ee9c3368bedf0196806"
URL="https://github.com/yonaskolb/XcodeGen/releases/download/${VERSION}/xcodegen.zip"

dest="${RUNNER_TEMP:-$(mktemp -d)}/xcodegen-${VERSION}"
rm -rf "$dest"
mkdir -p "$dest"

curl -fsSL --retry 3 -o "$dest/xcodegen.zip" "$URL"

actual="$(shasum -a 256 "$dest/xcodegen.zip" | awk '{print $1}')"
if [ "$actual" != "$SHA256" ]; then
  echo "SHA-256 do XcodeGen não confere: esperado $SHA256, obtido $actual" >&2
  exit 1
fi

unzip -q "$dest/xcodegen.zip" -d "$dest"
# O zip traz xcodegen/bin/xcodegen e xcodegen/share/ (presets); mantenha a estrutura.
bin_dir="$dest/xcodegen/bin"
"$bin_dir/xcodegen" --version

if [ -n "${GITHUB_PATH:-}" ]; then
  echo "$bin_dir" >> "$GITHUB_PATH"
fi
