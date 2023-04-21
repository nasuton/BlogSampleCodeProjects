class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        DeepLAPI deepLAPI = new DeepLAPI();
        string fromText = "Hello,world!";
        var transText = deepLAPI.TranslationEnToJa(fromText);
        Console.WriteLine($"変換前:{fromText}\n変換後:{transText}");
        string targetFile = @"翻訳対象ファイルパス";
        string outPutFile = @"翻訳後ダウンロード先ファイルパス";
        var uploadResult = deepLAPI.TranslationUploadFile(targetFile, outPutFile);
        Console.WriteLine($"ファイル翻訳結果:{uploadResult}");
    }
}

public class DeepLResponseData
{
    public DeepLResponseData() { }

    [JsonProperty("translations")]
    public List<ResponseDataDetail> translations { get; set; }
}

public class ResponseDataDetail
{
    public ResponseDataDetail() { }

    [JsonProperty("detected_source_language")]
    public string detected_source_language { get; set; }

    [JsonProperty("text")]
    public string text { get;  set; }
}

public class DeepLDocumentUploadResponse
{
    public DeepLDocumentUploadResponse() { }

    [JsonProperty("document_id")]
    public string documentId { get; set; }

    [JsonProperty("document_key")]
    public string documentKey { get; set; }
}

public class DeepLDocumentGetStatusResponse
{
    public DeepLDocumentGetStatusResponse() { }

    [JsonProperty("document_id")]
    public string documentId { get; set ; }

    [JsonProperty("status")]
    public string documentStatus { get; set; }

    [JsonProperty("seconds_remaining")]
    public int secondsRemaining { get; set; }

    [JsonProperty("billed_characters")]
    public int billedCharacters { get; set; }

    [JsonProperty("error_message")]
    public string errorMessage { get; set; }
}

internal class DeepLAPI
{
    /// <summary>
    /// deepAPIで使用する認証キー
    /// </summary>
    private readonly string deepAPIAuthCode = "DeepL認証コード";

    /// <summary>
    /// 日本語を意味する値(DeepLで使用)
    /// </summary>
    private readonly string Lang_JA = "JA";

    public DeepLAPI()
    {

    }

    /// <summary>
    /// 引数で受け取った英文を日本語に翻訳した文字列を変換する
    /// </summary>
    /// <param name="_enText">英文</param>
    /// <returns>日本語に翻訳した文字列</returns>
    public string TranslationEnToJa(string _enText)
    {
        string translationText = string.Empty;
        string postURL = "https://api-free.deepl.com/v2/translate";

        // POSTするデータを作成
        WebRequest req = WebRequest.Create(postURL);
        req.Method = "POST";
        req.Headers.Add("Authorization", $"DeepL-Auth-Key {deepAPIAuthCode}");
        req.ContentType = "application/x-www-form-urlencoded";
        var postData = "text=" + Uri.EscapeDataString(_enText);
        postData += "&target_lang=" + Uri.EscapeDataString(Lang_JA);
        var data = Encoding.ASCII.GetBytes(postData);
        req.ContentLength = data.Length;
        using (var stream = req.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        // レスポンスデータを処理
        using(var response = req.GetResponse())
        using(var stream = response.GetResponseStream())
        {
            StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            var result = reader.ReadToEnd();
            var responseData = JsonConvert.DeserializeObject<DeepLResponseData>(result);
            translationText = responseData.translations[0].text;
        }

        return translationText;
    }

    /// <summary>
    /// 翻訳したファイルをアップロードし、翻訳が完了したファイルをダウンロードする
    /// </summary>
    /// <param name="_translationPath">翻訳したいファイルパス(フルパス)</param>
    /// <param name="_outPutFilePath">翻訳されたファイルパス(フルパス)</param>
    /// <returns>翻訳結果(OK or NG)</returns>
    public string TranslationUploadFile(string _translationPath, string _outPutFilePath)
    {
        // アップロード先URL
        string uploadURL = "https://api-free.deepl.com/v2/document";

        // アップロードするファイル名を取得
        string translationFileName = Path.GetFileName(_translationPath);

        // アップロードしたファイルの参照用情報
        string documentKey = string.Empty;
        string documentId = string.Empty;

        // アップロードしたファイルのステータス
        string uploadStatus = string.Empty;

        string resultStatus = string.Empty;

        try
        {
            #region 翻訳対象のファイルをアップロードする
            //  POSTするデータを作成(ドキュメントのアップロード用)
            var uploadWebRequest = WebRequest.Create(uploadURL) as HttpWebRequest;
            uploadWebRequest.Method = "POST";
            uploadWebRequest.Headers.Add("Authorization", $"DeepL-Auth-Key {deepAPIAuthCode}");
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(Lang_JA), "target_lang");
            multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(_translationPath)), "file", translationFileName);
            uploadWebRequest.ContentType = multipartContent.Headers.ContentType.ToString();
            uploadWebRequest.ContentLength = multipartContent.Headers.ContentLength.Value;
            using (var stream = uploadWebRequest.GetRequestStream())
            {
                multipartContent.CopyToAsync(stream).Wait();
            }

            // レスポンスデータを処理(ドキュメントのアップロード用)
            using (var response = uploadWebRequest.GetResponse() as HttpWebResponse)
            using (var stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                var result = reader.ReadToEnd();
                var responseData = JsonConvert.DeserializeObject<DeepLDocumentUploadResponse>(result);
                // レスポンスに含まれるIDとKeyを取得(後続の処理で使用)
                documentId = responseData.documentId;
                documentKey = responseData.documentKey;
            }
            #endregion

            #region アップロードしたファイルのステータスを取得
            // https://api-free.deepl.com/v2/document/ + ドキュメントID
            string uploadFileStatusURL = $"{uploadURL}/{documentId}";
            //  POSTするデータを作成(アップロードしたファイルのステータス取得用)
            WebRequest uploadFileStatusRequest = WebRequest.Create(uploadFileStatusURL);
            uploadFileStatusRequest.Method = "POST";
            uploadFileStatusRequest.Headers.Add("Authorization", $"DeepL-Auth-Key {deepAPIAuthCode}");
            uploadFileStatusRequest.ContentType = "application/x-www-form-urlencoded";
            var statusPostData = "document_key=" + Uri.EscapeDataString(documentKey);
            var statusData = Encoding.ASCII.GetBytes(statusPostData);
            uploadFileStatusRequest.ContentLength = statusData.Length;
            using (var stream = uploadFileStatusRequest.GetRequestStream())
            {
                stream.Write(statusData, 0, statusData.Length);
            }

            // レスポンスデータを処理
            using (var response = uploadFileStatusRequest.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                var result = reader.ReadToEnd();
                var responseData = JsonConvert.DeserializeObject<DeepLDocumentGetStatusResponse>(result);
                uploadStatus = responseData.documentStatus;
            }
            #endregion

            #region 翻訳が完了したファイルをダウンロードする
            if (uploadStatus == "done")
            {
                // https://api-free.deepl.com/v2/document/ + ドキュメントID/ + result
                var translationFileDownloadURL = $"{uploadFileStatusURL}/result";
                //  POSTするデータを作成(翻訳されたファイル取得用)
                WebRequest translationFileDownloadRequst = WebRequest.Create(translationFileDownloadURL);
                translationFileDownloadRequst.Method = "POST";
                translationFileDownloadRequst.Headers.Add("Authorization", $"DeepL-Auth-Key {deepAPIAuthCode}");
                translationFileDownloadRequst.ContentType = "application/x-www-form-urlencoded";
                var translationPostData = "document_key=" + Uri.EscapeDataString(documentKey);
                var translationData = Encoding.ASCII.GetBytes(translationPostData);
                translationFileDownloadRequst.ContentLength = translationData.Length;
                using (var stream = translationFileDownloadRequst.GetRequestStream())
                {
                    stream.Write(translationData, 0, translationData.Length);
                }

                // レスポンスデータを処理(ファイルのダウンロード)
                using (var response = translationFileDownloadRequst.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    using (var fileStream = new FileStream(_outPutFilePath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            #endregion

            resultStatus = "OK";
        }
        catch (Exception ex)
        {
            resultStatus = "NG";
        }
        
        return resultStatus;
    }
}