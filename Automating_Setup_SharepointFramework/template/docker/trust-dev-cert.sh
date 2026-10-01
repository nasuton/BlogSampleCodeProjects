#!/bin/bash
# 開発用 HTTPS 証明書を生成し、ホストにインポートできる形で ./certs へコピーする。
# 生成された certs/rushstack-serve.cer を Windows の
# 「信頼されたルート証明機関」にインポートすると https://localhost:4321 の警告が消える。
set -e

npx heft trust-dev-cert

CERT_DIR="${HOME}/.rushstack"
OUT_DIR="/usr/app/spfx/certs"
mkdir -p "${OUT_DIR}"

if [ -f "${CERT_DIR}/rushstack-serve.pem" ]; then
  cp "${CERT_DIR}/rushstack-serve.pem" "${OUT_DIR}/rushstack-serve.cer"
  echo "証明書を ${OUT_DIR}/rushstack-serve.cer に出力しました。"
else
  echo "証明書が見つかりません: ${CERT_DIR}" >&2
  ls -la "${CERT_DIR}" >&2 || true
  exit 1
fi
