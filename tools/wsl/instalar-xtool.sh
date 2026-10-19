#!/usr/bin/env bash
# Instala o xtool 1.22.2 no Ubuntu do WSL. O xtool instala o .ipa no iPhone usando o serviço
# da Apple do Windows (usbmuxd em 127.0.0.1:27015), visível no Ubuntu por causa do
# `networkingMode=mirrored` no %USERPROFILE%\.wslconfig.
#
# Uso, no Windows (pede a senha do sudo do Ubuntu):
#   wsl -d Ubuntu -- bash "/mnt/c/<caminho do repo>/tools/wsl/instalar-xtool.sh" ["/mnt/c/<caminho>/xtool-x86_64.AppImage"]
# Sem o argumento (ou se o arquivo não existir), o script baixa o AppImage do GitHub.
# Depois: wsl -d Ubuntu -- bash -lc 'xtool auth login'   (você digita o Apple ID e a senha)
set -euo pipefail

VERSION="1.22.2"
SHA256="e3176cdf861f58b7f135c755c7bfaec92d0568ef826027b4e5e3d82b3219ddfe"
URL="https://github.com/xtool-org/xtool/releases/download/${VERSION}/xtool-x86_64.AppImage"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

APPIMAGE="${1:-}"
if [ -z "$APPIMAGE" ] || [ ! -f "$APPIMAGE" ]; then
  echo "Baixando o xtool ${VERSION}..."
  APPIMAGE="$WORK/download.AppImage"
  curl -fL --retry 3 -o "$APPIMAGE" "$URL"
fi

echo "Conferindo o SHA-256..."
echo "${SHA256}  ${APPIMAGE}" | sha256sum -c -

echo "Instalando zip e unzip (o xtool usa para montar o .ipa)..."
sudo apt-get update -qq
sudo apt-get install -y -qq zip unzip

echo "Extraindo o AppImage em /opt/xtool..."
cp "$APPIMAGE" "$WORK/xtool.AppImage"
chmod +x "$WORK/xtool.AppImage"
(cd "$WORK" && ./xtool.AppImage --appimage-extract >/dev/null)
sudo rm -rf /opt/xtool
sudo mv "$WORK/squashfs-root" /opt/xtool
sudo ln -sf /opt/xtool/AppRun /usr/local/bin/xtool

echo "Apontando o xtool para o serviço da Apple do Windows..."
echo 'export USBMUXD_SOCKET_ADDRESS=127.0.0.1:27015' | sudo tee /etc/profile.d/xtool-usbmuxd.sh >/dev/null

echo
echo "xtool instalado em /opt/xtool (comando: xtool)."
echo "Próximo passo: wsl -d Ubuntu -- bash -lc 'xtool auth login'"
