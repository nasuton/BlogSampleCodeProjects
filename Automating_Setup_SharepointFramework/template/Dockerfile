# SharePoint Framework (SPFx) 開発環境
# SPFx 1.23.x は Node.js 22 LTS が必須 (package.json engines: >=22.14.0 <23)
FROM node:22-bookworm-slim

ARG SPFX_VERSION=latest

ENV DEBIAN_FRONTEND=noninteractive \
    NPM_CONFIG_UPDATE_NOTIFIER=false \
    NODE_OPTIONS="--max-old-space-size=4096 --dns-result-order=ipv4first"

RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates python3 make g++ socat \
    && rm -rf /var/lib/apt/lists/*

# SPFx 1.23 は heft ベース (旧 gulp も互換のため同梱)
RUN npm install -g gulp-cli yo @microsoft/generator-sharepoint@${SPFX_VERSION} \
    && npm cache clean --force

# 作業ディレクトリ (ホストのプロジェクトフォルダをマウント)
WORKDIR /usr/app/spfx

RUN chown -R node:node /usr/app

# entrypoint で volume の所有権を整えたのち node ユーザーへ降格して実行
COPY --chmod=755 docker/entrypoint.sh docker/serve.sh docker/trust-dev-cert.sh docker/load-env.sh /usr/local/bin/

# 対話シェル (VS Code ターミナル等) 起動時に .env を自動で読み込む
RUN printf '\n# SPFx: load project .env\n[ -f /usr/local/bin/load-env.sh ] && . /usr/local/bin/load-env.sh\n' >> /etc/bash.bashrc

# heft start (dev server)
EXPOSE 4321

ENTRYPOINT ["entrypoint.sh"]
CMD ["bash"]
