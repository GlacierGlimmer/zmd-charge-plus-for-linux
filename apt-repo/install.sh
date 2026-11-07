#!/usr/bin/env bash
set -euo pipefail

KEYRING="/etc/apt/keyrings/ecp-archive-keyring.gpg"
SOURCE="/etc/apt/sources.list.d/endfield-charge-plus.list"
REPO_URL="https://apt.x-neko.com"

if [ "$(id -u)" -ne 0 ]; then
  echo "Please run this script as root, for example:"
  echo "  curl -fsSL ${REPO_URL}/install.sh | sudo bash"
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  apt-get update
  apt-get install -y curl
fi

mkdir -p /etc/apt/keyrings

curl -fsSL "${REPO_URL}/ecp-archive-keyring.gpg"   -o "$KEYRING"

chmod 0644 "$KEYRING"

echo "deb [arch=amd64 signed-by=$KEYRING] ${REPO_URL} stable main"   > "$SOURCE"

apt-get update

echo
echo "Endfield Charge Plus APT repository has been added successfully."
echo "Install ECP with:"
echo "  sudo apt-get install endfield-charge-plus-for-linux"
