# 使い方

_apiKey、_userName、_hostNameにそれぞれ対応した値を設定してください

# 各関数(メソッド)について

## Task `<HttpResponseMessage>` GetAPIAsync(string url)

引数のURLに対してGetリクエストを送信する

## List<WPTag_Response> GetAllTags()

WordPress上の全Tagを取得する

## List<WPCategory_Response> GetAllCategories()

WordPress上の全Categoryを取得する

## List<WP_PostData> GetAllPosts()

WordPress上の全投稿データを取得する(固定ページは含まれません)

## WP_PostData GetLatestPost()

WordPress上の最新投稿を取得する(固定ページは含まれません)

## void WP_PostDataCSV_Write(List<WP_PostData> postDatas)

投稿データをCSVファイルに書き出します
