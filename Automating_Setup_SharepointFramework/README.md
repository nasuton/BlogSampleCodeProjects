# SPFx × Dev Containers プロジェクト作成キット

VS Code の **Dev Containers** 拡張機能を前提に、SharePoint Framework (SPFx) プロジェクトを
Docker コンテナ内で開発するための **テンプレート・手順書・自動生成スクリプト** をまとめたフォルダーです。

ホストに必要なのは **Docker Desktop と VS Code** のみ。Node.js / gulp / yo のインストールは不要です。

## フォルダー構成

```
Doc/
├── README.md                          # このファイル (索引)
├── New-SpfxProject.ps1                # 新規プロジェクトを一発で作成するスクリプト
├── New-SpfxProject.bat                # 上記を bat / コマンド プロンプトから呼ぶラッパー (Shift_JIS 保存)
├── logs/                              # スクリプト実行ログ (自動生成・Git 管理外推奨)
├── 01_新規プロジェクト作成手順.md      # スクリプト利用 / 手動の両手順
├── 02_ファイル一覧と変更ポイント.md    # 各テンプレートファイルの役割と、プロジェクトごとに変える箇所
├── 03_日常の開発フロー.md              # serve / ワークベンチ / ビルド / パッケージ
├── 04_トラブルシューティング.md        # 症状別の索引と対処
├── 05_構築時に躓いた内容と対処記録.md  # 実際に発生した問題を時系列で記録 (対処手順・再発防止策・チェックリスト)
├── 06_VSCodeデバッグ手順.md            # ブレークポイントを使ったデバッグ (ホスト側 Edge を起動してアタッチ)
└── template/                          # 新規プロジェクトへコピーするファイル一式 (動作確認済み)
    ├── Dockerfile
    ├── docker-compose.yml
    ├── .dockerignore
    ├── .gitattributes
    ├── .env.example
    ├── docker/
    │   ├── entrypoint.sh
    │   ├── load-env.sh
    │   ├── serve.sh
    │   └── trust-dev-cert.sh
    ├── .devcontainer/
    │   ├── devcontainer.json
    │   └── docker-compose.devcontainer.yml
    └── .vscode/
        ├── tasks.json
        ├── settings.json
        └── launch.json                    # Dev Container 用デバッグ構成 (browserLaunchLocation: ui)
```

## クイックスタート

```powershell
# Docker Desktop を起動しておく
cd D:\work\SPFx\SampleSPFx\Doc
.\New-SpfxProject.ps1 `
  -Path D:\work\SPFx\MyProject `
  -SolutionName my-project `
  -ComponentName MyWebPart `
  -ComponentType webpart `         # webpart / extension / library / adaptiveCardExtension
  -Framework react `               # react / none / minimal
  -TenantDomain <テナント名>.sharepoint.com `
  -SkipInstall
# ※ 引数を省略した項目 (種類・フレームワーク・名前など) は対話形式で選択できる。
#    New-SpfxProject.bat をダブルクリックすれば全項目を対話で決められる。

code D:\work\SPFx\MyProject
# F1 → Dev Containers: Reopen in Container
# 初回のみ certs\rushstack-serve.cer を「信頼されたルート証明機関」へインポート
# Ctrl+Shift+B (SPFx: serve) → 表示された Workbench URL をブラウザで開く
```

詳細は [01_新規プロジェクト作成手順.md](01_新規プロジェクト作成手順.md) を参照してください。
初回起動で **`{tenantDomain}` の警告** や **ワークベンチのマニフェスト読み込みエラー** が出た場合は
[05_構築時に躓いた内容と対処記録.md](05_構築時に躓いた内容と対処記録.md) の記録 6・7 に対処手順があります。

## 環境バージョン (このキットで検証済み)

| 項目 | バージョン |
| ---- | ---- |
| SPFx (`@microsoft/generator-sharepoint`) | 1.23.2 (heft ベース) |
| Node.js (ベースイメージ) | 22 LTS (`node:22-bookworm-slim`) |
| Docker Desktop | 29.x / Compose v5 |
| VS Code 拡張 | Dev Containers (`ms-vscode-remote.remote-containers`) |
