#!/bin/bash
# コンテナ内で開発サーバー (heft start) を起動する。
#
# SPFx の dev server は serve.json の hostname (既定 localhost) にバインドし、
# 同じ値がマニフェストの URL にも使われるため 0.0.0.0 にはできない。
# そこで socat でコンテナ NIC の 4321 番をループバックへ転送し、
# ホスト側から https://localhost:4321 でアクセスできるようにする。
set -e

# .env (SPFX_SERVE_TENANT_DOMAIN 等) を読み込む
. load-env.sh

PORT="${SPFX_SERVE_PORT:-4321}"
CONTAINER_IP="$(hostname -i | awk '{print $1}')"

# webpack-dev-server は Node の名前解決で localhost を ::1 または 127.0.0.1 に決める。
# 同じ解決結果へ転送しないと接続できないため Node に問い合わせる。
LOOPBACK="$(node -e "require('dns').lookup('localhost',(e,a)=>console.log(a||'127.0.0.1'))")"
if [[ "${LOOPBACK}" == *:* ]]; then
  TARGET="TCP6:[${LOOPBACK}]:${PORT}"
else
  TARGET="TCP4:${LOOPBACK}:${PORT}"
fi

socat "TCP4-LISTEN:${PORT},bind=${CONTAINER_IP},fork,reuseaddr" "${TARGET}" &
SOCAT_PID=$!
trap 'kill ${SOCAT_PID} 2>/dev/null || true' EXIT

# コンテナ内ではブラウザを開けないため --nobrowser を付与
exec npx heft start --clean --nobrowser "$@"
