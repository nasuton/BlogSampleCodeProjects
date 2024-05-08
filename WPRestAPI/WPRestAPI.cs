using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// WordPressの投稿データを格納するクラス
/// </summary>
public class WPPOST_Response
{
    public WPPOST_Response() { }
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    [JsonPropertyName("title")]
    public WP_TitleRendered? rendered { get; set; }
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    [JsonPropertyName("categories")]
    public List<int>? Categories { get; set; }
    [JsonPropertyName("tags")]
    public List<int>? Tags { get; set; }
}
/// <summary>
/// WordPressのタイトルを格納するクラス
/// </summary>
public class WP_TitleRendered
{
    public WP_TitleRendered() { }
    [JsonPropertyName("rendered")]
    public string? Title { get; set; }
}
/// <summary>
/// WordPressのタグ情報を格納するクラス
/// </summary>
public class WPTag_Response
{
    public WPTag_Response() { }
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
/// <summary>
/// WordPressのカテゴリ情報を格納するクラス
/// </summary>
public class WPCategory_Response
{
    public WPCategory_Response() { }
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
// CSVに書き込むデータを格納するクラス
public class WP_PostData
{
    public WP_PostData() { }
    public int? Id { get; set; }
    public string? Date { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? Tags { get; set; }
}
internal class WPRestAPI
{
    private string _apiKey = @"アプリケーションパスワード(文字列の間にある空欄ありのままで)";
    private string _userName = "アプリケーションパスワードを設定したユーザー名";
    private string _hostName = "WordPressのサイトURL";
    /// <summary>
    /// 対象URLに対してGetリクエストを送信する
    /// </summary>
    /// <param name="url">GETメソッドのURL</param>
    /// <returns>レスポンス結果</returns>
    private async Task<HttpResponseMessage> GetAPIAsync(string url)
    {
        HttpResponseMessage response;
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), url))
            {
                // Basic 認証のヘッダを追加
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_userName}:{_apiKey}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                response = await httpClient.SendAsync(request);
            }
        }
        return response;
    }
    /// <summary>
    /// 全てのタグを取得する
    /// </summary>
    /// <returns>取得した全てのタグ情報</returns>
    public List<WPTag_Response> GetAllTags()
    {
        var allTags = new List<WPTag_Response>();
        var url = $"{_hostName}/wp-json/wp/v2/tags";
        var response = GetAPIAsync(url).Result;
        // Headerから総ページ数を取得
        var wp_totalPages = int.Parse(response.Headers.GetValues("X-WP-TotalPages").First());
        allTags.AddRange(JsonSerializer.Deserialize<List<WPTag_Response>>(response.Content.ReadAsStringAsync().Result));
        // 2ページ目以降のデータを取得
        if(wp_totalPages > 1)
        {
            for (int i = 2; i < wp_totalPages + 1; i++)
            {
                url = $"{_hostName}/wp-json/wp/v2/tags" + $"?page={i}";
                response = GetAPIAsync(url).Result;
                allTags.AddRange(JsonSerializer.Deserialize<List<WPTag_Response>>(response.Content.ReadAsStringAsync().Result));
            }
        }
        return allTags;
    }
    /// <summary>
    /// 全てのカテゴリを取得する
    /// </summary>
    /// <returns>取得したすべてのカテゴリ情報</returns>
    public List<WPCategory_Response> GetAllCategories()
    {
        var allCategories = new List<WPCategory_Response>();
        var url = $"{_hostName}/wp-json/wp/v2/categories";
        var response = GetAPIAsync(url).Result;
        // Headerから総ページ数を取得
        var wp_totalPages = int.Parse(response.Headers.GetValues("X-WP-TotalPages").First());
        allCategories.AddRange(JsonSerializer.Deserialize<List<WPCategory_Response>>(response.Content.ReadAsStringAsync().Result));
        if (wp_totalPages > 1)
        {
            // 2ページ目以降のデータを取得
            for (int i = 2; i < wp_totalPages + 1; i++)
            {
                url = $"{_hostName}/wp-json/wp/v2/categories" + $"?page={i}";
                response = GetAPIAsync(url).Result;
                allCategories.AddRange(JsonSerializer.Deserialize<List<WPCategory_Response>>(response.Content.ReadAsStringAsync().Result));
            }
        }
        return allCategories;
    }
    /// <summary>
    /// 全ての投稿を取得する
    /// </summary>
    /// <returns>取得した投稿データリスト</returns>
    public List<WP_PostData> GetAllPosts()
    {
        var postDatas = new List<WP_PostData>();
        var allPosts = new List<WPPOST_Response>();
        var allTags = GetAllTags();
        var allCategories = GetAllCategories();
        var url = $"{_hostName}/wp-json/wp/v2/posts";
        var response = GetAPIAsync(url).Result;
        // Headerから総ページ数を取得
        var wp_totalPages = int.Parse(response.Headers.GetValues("X-WP-TotalPages").First());
        allPosts.AddRange(JsonSerializer.Deserialize<List<WPPOST_Response>>(response.Content.ReadAsStringAsync().Result));
        if(wp_totalPages > 1)
        {
            // 2ページ目以降のデータを取得
            for (int i = 2; i < wp_totalPages + 1; i++)
            {
                url = $"{_hostName}/wp-json/wp/v2/posts" + $"?page={i}";
                response = GetAPIAsync(url).Result;
                allPosts.AddRange(JsonSerializer.Deserialize<List<WPPOST_Response>>(response.Content.ReadAsStringAsync().Result));
            }
        }
        foreach (var post in allPosts)
        {
            var postData = new WP_PostData
            {
                Id = post.Id,
                Date = DateTime.Parse(post.Date).ToString("yyyy/MM/dd HH:mm:ss"),
                Title = WebUtility.HtmlDecode(post.rendered.Title),    // HTMLエンコードされた文字列をデコード
                Link = post.Link,
                Categories = new List<string>(),
                Tags = new List<string>()
            };
            foreach (var categoryId in post.Categories)
            {
                var category = allCategories.Find(c => c.Id == categoryId);
                postData.Categories.Add(category.Name);
            }
            foreach (var tagId in post.Tags)
            {
                var tag = allTags.Find(t => t.Id == tagId);
                postData.Tags.Add(tag.Name);
            }
            postDatas.Add(postData);
        }
        return postDatas;
    }
    /// <summary>
    /// 最新のWordPressの投稿データを取得する
    /// </summary>
    /// <returns>最新のWordPressの投稿データ</returns>
    public WP_PostData GetLatestPost()
    {
        var allTags = GetAllTags();
        var allCategories = GetAllCategories();
        var url = $"{_hostName}/wp-json/wp/v2/posts?per_page=1";
        var response = GetAPIAsync(url).Result;
        var latestPost = JsonSerializer.Deserialize<List<WPPOST_Response>>(response.Content.ReadAsStringAsync().Result).First();
        var postData = new WP_PostData
        {
            Id = latestPost.Id,
            Date = DateTime.Parse(latestPost.Date).ToString("yyyy/MM/dd HH:mm:ss"),
            Title = WebUtility.HtmlDecode(latestPost.rendered.Title),    // HTMLエンコードされた文字列をデコード
            Link = latestPost.Link,
            Categories = new List<string>(),
            Tags = new List<string>()
        };
        foreach (var categoryId in latestPost.Categories)
        {
            var category = allCategories.Find(c => c.Id == categoryId);
            postData.Categories.Add(category.Name);
        }
        foreach (var tagId in latestPost.Tags)
        {
            var tag = allTags.Find(t => t.Id == tagId);
            postData.Tags.Add(tag.Name);
        }
        return postData;
    }
    /// <summary>
    /// CSVファイルにWordPressの投稿データを書き込む
    /// </summary>
    /// <param name="postDatas">投稿データリスト</param>
    public void WP_PostDataCSV_Write(List<WP_PostData> postDatas)
    {
        string csvPath = @"CSVファイルのパス";
        using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
        {
            // ヘッダーを書き出し
            writer.WriteLine("Id,Date,Title,Link,Categories,Tags");
            // データを書き出し
            foreach (var post in postDatas)
            {
                writer.WriteLine($"{post.Id},{post.Date},{post.Title},{post.Link},{string.Join("|", post.Categories)},{string.Join("|", post.Tags)}");
            }
        }
    }
    
}

