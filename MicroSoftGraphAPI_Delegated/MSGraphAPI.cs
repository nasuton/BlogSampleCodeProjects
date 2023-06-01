using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestCSharp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var msGraph = new MSGraphAPI();
            msGraph.DelegatedPermission("取得対象となる会議室のメールアドレス").Wait();
        }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
    }

    public class AppData
    {
        [JsonProperty("value")]
        public List<Calendar> Value { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odata_nextLink { get; set; }

        public bool ContainsNextLink()
        {
            return !string.IsNullOrEmpty(odata_nextLink);
        }

        public string GetNextLink()
        {
            return odata_nextLink;
        }
    }

    public class Schedule
    {
        [JsonProperty("dateTime")]
        public DateTime dateTime { get; set; }

        [JsonProperty("timeZone")]
        public string timeZone { get; set; }
    }

    public class Organizer
    {
        [JsonProperty("emailAddress")]
        public EmailAddress emailAddress { get; set; }
    }

    public class Attendees
    {
        [JsonProperty("emailAddress")]
        public EmailAddress emailAddress { get; set; }
    }

    public class EmailAddress
    {
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("address")]
        public string address { get; set; }
    }

    public class Calendar
    {
        
        public string subject { get; set; }
        [JsonProperty("start")]
        public Schedule start { get; set; }
        [JsonProperty("end")]
        public Schedule end { get; set; }
        [JsonProperty("organizer")]
        public Organizer organizer { get; set; }
        [JsonProperty("attendees")]
        public List<Attendees> attendees { get; set; }
    }

    internal class MSGraphAPI
    {
        private readonly string tenantName = "アプリが登録されたテナントID";

        private readonly string clientUserid = "作成したAzure上のアプリID";
        private readonly string clientUserSecret = "Azure上のアプリで作成したクライアントシークレットキーの値";
        private readonly string userName = "認証時に使用するユーザーアドレス(二段階認証の設定がされていないもの)";
        private readonly string userPass = "userNameのパスワード";
        private Dictionary<string, string> reqUserTokenBody = null;

        public MSGraphAPI()
        {
            reqUserTokenBody = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                {"username", userName },
                {"password", userPass },
                { "scope", "Calendars.Read Calendars.Read.Shared" }, // Azureアプリに含まれていないとエラーが発生する
                { "client_id", clientUserid },
                { "client_secret", clientUserSecret }
            };
        }

        /// <summary>
        /// 委任されたアクセス許可バージョン
        /// </summary>
        /// <param name="_accountId">対象メールアドレス</param>
        /// <returns></returns>
        public async Task DelegatedPermission(string _accountId)
        {
            using (HttpClient client = new HttpClient())
            {
                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantName}/oauth2/v2.0/token";
                var tokenResponse = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(reqUserTokenBody));
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenAppResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenContent);

                try
                {
                    string apiUrl = $"https://graph.microsoft.com/v1.0/users/{_accountId}/calendarView?startDateTime=2023-01-01T19:00:00&endDateTime=2023-05-31T00:00:00&$select=subject,start,end,organizer,attendees";
                    while (true)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                        request.Headers.Add("Authorization", $"Bearer {tokenAppResponse.access_token}");

                        var response = await client.SendAsync(request);
                        var responseData = await response.Content.ReadAsStringAsync();
                        var appData = JsonConvert.DeserializeObject<AppData>(responseData);
                        var appCalendars = appData.Value;
                        foreach (var calendar in appCalendars)
                        {
                            Console.WriteLine(calendar.organizer.emailAddress.address);
                        }

                        if (!appData.ContainsNextLink())
                        {
                            break;
                        }

                        apiUrl = appData.GetNextLink();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }
}
