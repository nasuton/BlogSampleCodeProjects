# 使用する際の注意点
- 変数.client_id、変数.tenant_id、変数.clientSecret、変数.siteId、変数.listDisplayNameのそれぞれは適宜使用環境に合わせて変更してください
- 今回はclientSecretでの認証にしています

## 本コードのテスト
`src/test/kotlin/MainTest.kt` では次を確認しています。

- Site / List / Items がすべて取れたときに、期待どおりの出力になる
- Site が取得できないときに、`Failed to retrieve Site ID.` を出して終了する
- List が取得できないときに、`Failed to retrieve List ID.` を出して終了する
