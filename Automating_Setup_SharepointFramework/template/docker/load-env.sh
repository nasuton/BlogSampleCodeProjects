#!/bin/bash
# プロジェクト直下の .env をシェル環境に読み込む。
# compose の environment はコンテナ作成時にしか反映されないため、
# .env を後から編集しても再作成なしで反映されるようにする。
# Windows で編集された CRLF も考慮して \r を除去する。
SPFX_ENV_FILE="${SPFX_ENV_FILE:-/usr/app/spfx/.env}"
if [ -f "${SPFX_ENV_FILE}" ]; then
  set -a
  # shellcheck disable=SC1090
  . <(sed -e 's/\r$//' -e '/^\s*#/d' -e '/^\s*$/d' "${SPFX_ENV_FILE}")
  set +a
fi
