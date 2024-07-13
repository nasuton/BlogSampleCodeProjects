internal class PostResponse
{
    public PostResponse() { }
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    [JsonPropertyName("title")]
    public TitleRendered? Title { get; set; }
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    [JsonPropertyName("content")]
    public ContentRendered Content { get; set; }
}

internal class TitleRendered
{
    public TitleRendered() { }
    [JsonPropertyName("rendered")]
    public string? Rendered { get; set; }
}

internal class ContentRendered
{
        public ContentRendered() { }

        [JsonPropertyName("rendered")]
        public string? Rendered { get; set; }
}

internal class WPRestAPI
{
    private string _appPass;
    private string _userName;
    private string _hostName;
    public WPRestAPI(string hostUrl, string username, string appPass) 
    {
        _appPass = appPass;
        _userName = username;
        _hostName = hostUrl;
    }

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
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_userName}:{_appPass}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                response = await httpClient.SendAsync(request);
            }
        }
        return response;
    }

    /// <summary>
    /// 全ての投稿を取得する
    /// </summary>
    /// <returns>取得した投稿データリスト</returns>
    public List<PostResponse> GetAllPosts()
    {
        var allPosts = new List<PostResponse>();
        var url = $"{_hostName}/wp-json/wp/v2/posts?per_page=20";
        var response = GetAPIAsync(url).Result;
        // Headerから総ページ数を取得
        var wp_totalPages = int.Parse(response.Headers.GetValues("X-WP-TotalPages").First());
        allPosts.AddRange(JsonSerializer.Deserialize<List<PostResponse>>(response.Content.ReadAsStringAsync().Result));
        if (wp_totalPages > 1)
        {
            // 2ページ目以降のデータを取得
            for (int i = 2; i < wp_totalPages + 1; i++)
            {
                url = $"{_hostName}/wp-json/wp/v2/posts" + $"?page={i}&per_page=20";
                response = GetAPIAsync(url).Result;
                allPosts.AddRange(JsonSerializer.Deserialize<List<PostResponse>>(response.Content.ReadAsStringAsync().Result));
            }
        }
        return allPosts;
    }
}

public List<string> CheckLink(string content)
{

    string pattern = @"href=\""(.*?)\""";
    var regex = new Regex(pattern);
    var result = new List<string>();
    var matches = regex.Matches(content);
    result.AddRange(matches.Select(m => m.Value));

    return result;
}

public bool UrlRequest(string url)
{
    bool result = false;
    var request = (HttpWebRequest)WebRequest.Create(url);
    request.Method = "HEAD";
    request.Timeout = 10000;
    using (var response = (HttpWebResponse)request.GetResponse())
    {
        // HTTPステータスコードを取得
        HttpStatusCode statusCode = response.StatusCode;
        // ステータスコードが200番台であれば成功とみなす
        result = (int)statusCode >= 200 && (int)statusCode < 300;
    }
    return result;
}

void HtmlDocumentVer(string url, string userName, string secretKey)
{
    var wPRestAPI = new WPRestAPI(url, userName, secretKey);
    var postResponses = wPRestAPI.GetAllPosts();
    var linkCheck = new LinkCheck();
    
    foreach (var post in postResponses)
    {
        // HtmlDocumentオブジェクトを作成
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(post.Content.Rendered);

        // href属性を持つ全ての<a>タグを取得
        var hrefList = new List<string>();
        var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
        if (links == null)
        {
            continue;
        }
        
        Console.WriteLine(post.Title.Rendered);
        foreach (var link in links)
        {
            var hrefValue = link.GetAttributeValue("href", string.Empty);
            hrefList.Add(hrefValue);
        }

        // 取得したhrefを出力
        foreach (var href in hrefList)
        {
            try
            {
                if (linkCheck.UrlRequest(href))
                {
                    Console.WriteLine($"{href} is OK");
                }
                else
                {
                    Console.WriteLine($"{href} is NG");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(href);
                Console.WriteLine(ex.Message);
            }
        }
    }
}

void RegexVer(string url, string userName, string secretKey)
{
    var wPRestAPI = new WPRestAPI(url, userName, secretKey);
    var postResponses = wPRestAPI.GetAllPosts();
    var linkCheck = new LinkCheck();

    foreach (var post in postResponses)
    {
        var matches = new RegexCheck().CheckLink(post.Content.Rendered);
        if(matches.Count == 0)
        {
            continue;
        }

        Console.WriteLine(post.Title.Rendered);
        foreach(var match in matches)
        {
            // 取得したhrefを出力(href="を削除および末尾の"を削除)
            var replaceMatch = match.Replace("href=\"", "");
            replaceMatch = replaceMatch.Replace("\"", "");
            try
            {
                if (linkCheck.UrlRequest(replaceMatch))
                {
                    Console.WriteLine($"{replaceMatch} is OK");
                }
                else
                {
                    Console.WriteLine($"{replaceMatch} is NG");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(replaceMatch);
                Console.WriteLine(ex.Message);
            }
        }
    }
}

try 
{
    string wordpress_host_url = "https://example.com"; // WordPressのURL
    string wordpress_user_name = "admin";
    string wordpress_app_pass = "password"

    HtmlDocumentVer(wordpress_host_url, wordpress_user_name, wordpress_app_pass);
    RegexVer(wordpress_host_url, wordpress_user_name, wordpress_app_pass);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}