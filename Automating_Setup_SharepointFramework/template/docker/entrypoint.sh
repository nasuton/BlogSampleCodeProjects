#!/bin/bash
# 名前付き volume (node_modules, ~/.rushstack) は初回作成時に root 所有になるため、
# 起動時に node ユーザーへ所有権を移してから権限を落として実行する。
set -e

if [ "$(id -u)" = "0" ]; then
  NODE_UID="$(id -u node)"
  for dir in /usr/app/spfx/node_modules /home/node/.rushstack; do
    if [ -d "${dir}" ] && [ "$(stat -c %u "${dir}")" != "${NODE_UID}" ]; then
      chown node:node "${dir}"
    fi
  done
  export HOME=/home/node
  exec setpriv --reuid=node --regid=node --init-groups "$@"
fi

exec "$@"
