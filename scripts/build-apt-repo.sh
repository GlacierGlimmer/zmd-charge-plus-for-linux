#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APT_ROOT="$REPO_ROOT/apt-repo"

mkdir -p "$APT_ROOT/dists/stable/main/binary-amd64"
mkdir -p "$APT_ROOT/pool/main/e/endfield-charge-plus"

cd "$APT_ROOT"

dpkg-scanpackages --arch amd64 pool/ /dev/null > dists/stable/main/binary-amd64/Packages
gzip -9 -c dists/stable/main/binary-amd64/Packages > dists/stable/main/binary-amd64/Packages.gz

apt-ftparchive \
  -o APT::FTPArchive::Release::Origin="Endfield Charge Plus" \
  -o APT::FTPArchive::Release::Label="Endfield Charge Plus" \
  -o APT::FTPArchive::Release::Suite="stable" \
  -o APT::FTPArchive::Release::Codename="stable" \
  -o APT::FTPArchive::Release::Architectures="amd64" \
  -o APT::FTPArchive::Release::Components="main" \
  -o APT::FTPArchive::Release::Description="Official APT repository for Endfield Charge Plus for Linux" \
  release dists/stable > dists/stable/Release
