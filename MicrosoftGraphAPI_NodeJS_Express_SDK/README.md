# 本プロジェクトの使い方   
## まず初めに
ブログ内でも記載していますが、appSettings.js内にある以下の変数の値を自身の環境に合わせて設定してください
1. TENANT_ID      - 使用するAzureアプリ上から取得したテナントID
2. CLIENT_ID      - 使用するAzureアプリ上から取得したクライアントID
3. CLIENT_SECRET  - 使用するAzureアプリ上のクライアントシークレットキーの値

## 使用しているモジュールについて
package.jsonにも記載がありますが以下の3つです
1. Express
2. @azure/identity
3. @microsoft/microsoft-graph-client
4. isomorphic-fetch

## プロジェクトの作成
NodeJSのある環境上で「npm install」を実行し、インストール後に「node index.js」で実行できます