# 本プロジェクトの使い方   
## まず初めに
ブログ内でも記載していますが、auth.js内にある以下の変数の値を自身の環境に合わせて設定してください
AAD_ENDPOINT、GRAPH_ENDPOINTについては[こちらのサイト](https://learn.microsoft.com/ja-jp/azure/active-directory/develop/tutorial-v2-nodejs-console#add-app-registration-details)を参考に設定してください
1. TENANT_ID      - 使用するAzureアプリ上から取得したテナントID
2. CLIENT_ID      - 使用するAzureアプリ上から取得したクライアントID
3. CLIENT_SECRET  - 使用するAzureアプリ上のクライアントシークレットキーの値
4. AAD_ENDPOINT   - 使用環境によって変わるので適宜変更してください
5. GRAPH_ENDPOINT - 使用環境によって変わるので適宜変更してください

## 使用しているモジュールについて
package.jsonにも記載がありますが以下の3つです
1. Express
2. axios
3. @azure/msal-node

## プロジェクトの作成
NodeJSのある環境上で「npm install」を実行し、インストール後に「node index.js」で実行できます