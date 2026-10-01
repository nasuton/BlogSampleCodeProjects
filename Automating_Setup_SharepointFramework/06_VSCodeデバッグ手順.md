# 06. VS Code でブレークポイントを使ったデバッグ

Dev Container 内の VS Code から、ホスト (Windows) の Microsoft Edge を起動し、
ワークベンチ上で動く Web パーツの TypeScript ソースにブレークポイントを置いてデバッグする手順です。

## 仕組み

```
┌─ Windows (ホスト) ─────────────────────┐   ┌─ Dev Container ───────────────────────┐
│  Microsoft Edge                         │   │  VS Code Server + js-debug             │
│   ├ https://<tenant>/_layouts/workbench │   │   ├ ブレークポイント / 変数 / コールスタック │
│   │   └ manifests.js を localhost:4321 へ│◄──┤   │   (ポート 4321 をフォワード)            │
│   └ CDP (デバッグプロトコル) ───────────┼───►   └ sourcemap で src/*.tsx に対応付け     │
│  js-debug-companion (VS Code 本体側)    │   │  heft start (dev server)               │
└─────────────────────────────────────────┘   └───────────────────────────────────────┘
```

- コンテナ内にはブラウザが無いため、`launch.json` の **`"browserLaunchLocation": "ui"`** でホスト側の Edge を起動します
  (VS Code 組み込みの **JavaScript Debugger Companion** 拡張がデバッグ接続をコンテナ内へ中継)。
- Web パーツのバンドルは `https://localhost:4321` から配信され、sourcemap (`webpack:///.././src/...`) を
  `sourceMapPathOverrides` でコンテナ内の `/usr/app/spfx/src/...` に対応付けます。

## 手順

### 1. 事前準備 (初回のみ)

- 開発用証明書 `certs\rushstack-serve.cer` をホストの「信頼されたルート証明機関」へインポート済みであること
  ([05 記録 7](05_構築時に躓いた内容と対処記録.md#記録-7-ワークベンチでデバッグ-マニフェストを読み込み中にエラー-))
- `.env` に `SPFX_SERVE_TENANT_DOMAIN` を設定し、その後にコンテナを作成 (Rebuild) していること
  → `launch.json` の URL に `${env:SPFX_SERVE_TENANT_DOMAIN}` として埋め込まれる

### 2. serve を起動

`Ctrl+Shift+B` (**SPFx: serve**) → ターミナルに `Waiting for changes` が出るまで待つ。

### 3. ブレークポイントを置く

`src/webparts/<name>/components/<Name>.tsx` などを開き、行番号の左をクリック (赤い ●)。
例: `HelloWorld.tsx` の `render()` 内の `return (` 行、`<Name>WebPart.ts` の `onInit()` / `render()` など。

### 4. デバッグ開始

1. 左の「実行とデバッグ」(`Ctrl+Shift+D`) → 構成 **SPFx: Hosted Workbench (Edge)** を選ぶ
2. `F5`
3. ホスト側で Edge が新しいウィンドウで起動し、ワークベンチ URL (デバッグ用クエリ付き) を開く
4. M365 にサインイン (毎回一時プロファイルで起動するため要求される。回避方法は後述)
5. ワークベンチで **+** → ローカル セクションの Web パーツを追加
6. Web パーツが描画されるタイミングで、`render()` に置いたブレークポイントで停止する

停止中は変数ビュー・ウォッチ・コールスタック・デバッグコンソール (`this.props` など評価可) が使えます。
`F10` ステップオーバー / `F11` ステップイン / `F5` 続行。

### 5. コード修正 → 再確認

ソースを保存すると serve が再ビルドします。Edge 側でページを **再読み込み** (`F5` または `Ctrl+R`) すると
新しいバンドルが読み込まれ、ブレークポイントは自動で再バインドされます。デバッグセッションを止める必要はありません。

### 6. 終了

VS Code の停止ボタン (`Shift+F5`) → Edge のデバッグ用ウィンドウも閉じます。

## `launch.json` の構成

| 構成名 | 用途 |
| ---- | ---- |
| **SPFx: Hosted Workbench (Edge)** | serve が起動済みの状態で使う (通常はこれ) |
| **SPFx: serve + Hosted Workbench (Edge)** | `preLaunchTask` で serve を起動してからブラウザを開く。serve が既に動いていると二重起動になるので注意 |

主な設定値:

```jsonc
{
  "type": "msedge",
  "request": "launch",
  "browserLaunchLocation": "ui",          // ← Dev Container では必須。ホスト側で Edge を起動
  "url": "https://${env:SPFX_SERVE_TENANT_DOMAIN}/_layouts/workbench.aspx?debugManifestsFile=https%3A%2F%2Flocalhost%3A4321%2Ftemp%2Fbuild%2Fmanifests.js&debug=true&noredir=true",
  "webRoot": "${workspaceFolder}",        // = /usr/app/spfx
  "sourceMapPathOverrides": {
    "webpack:///.././src/*": "${webRoot}/src/*",   // SPFx 1.23 の sourcemap パス形式
    ...
  }
}
```

### カスタマイズ

**Chrome を使う**: `"type": "msedge"` → `"type": "chrome"` に変更 (ホストに Chrome が必要)。

**サインインを保持する**: 既定では一時プロファイルで起動するため毎回サインインが必要。
ホスト側の固定パスを `userDataDir` に指定すると Cookie が保持されます (パスはホスト側 Windows のパスで書く)。

```jsonc
"userDataDir": "C:\\Users\\<user>\\.spfx-edge-profile"
```

**実ページでデバッグ**: `url` をワークベンチではなく実際のサイトページに変更 (クエリ文字列は同じ)。

```
https://<tenant>.sharepoint.com/sites/<site>/SitePages/<page>.aspx?debugManifestsFile=...&debug=true&noredir=true
```

**テナントが `${env:}` で解決されない**: `url` の `${env:SPFX_SERVE_TENANT_DOMAIN}` を `<tenant>.sharepoint.com` に直接書き換える。

## トラブルシューティング

| 症状 | 原因 / 対処 |
| ---- | ---- |
| F5 で `Unable to find a browser` / `Could not launch browser` | `browserLaunchLocation` が `ui` になっていない (コンテナ内で Edge を探している)。`launch.json` を確認 |
| Edge は開くが URL が `https:///_layouts/...` (テナントが空) | `${env:SPFX_SERVE_TENANT_DOMAIN}` が未解決。`.env` 作成後に **Rebuild Container** するか、URL に直接テナントを書く |
| ブレークポイントが白抜き (未バインド) のまま | (1) Web パーツがまだ読み込まれていない → ワークベンチに追加する。(2) sourcemap の対応付け失敗 → デバッグコンソールで `.scripts` と入力し、`webpack:///` のパスが `sourceMapPathOverrides` のどれかに一致するか確認 |
| ワークベンチが「デバッグ マニフェストを読み込み中にエラー」 | 証明書未インポート。[05 記録 7](05_構築時に躓いた内容と対処記録.md#記録-7-ワークベンチでデバッグ-マニフェストを読み込み中にエラー-) |
| `render()` で止まらない | 既に描画済み。ワークベンチで Web パーツを削除して再追加、またはページ再読み込み |
| 変数が `undefined` と表示される | 本番ビルド (`--production`) のバンドルを見ている。`heft start` (デバッグビルド) で serve しているか確認 |
| Edge で `--remote-debugging-port` 関連の警告 | yo 生成の古い `launch.json` に残っている `runtimeArgs`。現行テンプレートでは不要なので削除済み |

## 補足: デバッグ構成なしで手軽に確認したい場合

VS Code のデバッガを使わず、Edge の DevTools (`F12`) → Sources タブでも `webpack://` 配下に TypeScript ソースが見え、
ブレークポイントを置けます。VS Code 側で編集 → 保存 → ブラウザ再読み込みの流れは同じです。
