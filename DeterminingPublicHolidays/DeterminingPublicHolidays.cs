using System.Text;

internal class DeterminingPublicHolidays
{
    private string url = "https://www8.cao.go.jp/chosei/shukujitsu/syukujitsu.csv";
    private List<string> publicHolidays;

    public DeterminingPublicHolidays(string targetYear)
    {
        // 祝日の一覧を取得
        publicHolidays = GetPublicHolidays(targetYear).Result;
    }

    private async Task<List<string>> GetPublicHolidays(string targetYear)
    {
        List<string> holidays = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            // ステータスコードが200以外の場合は例外をスロー
            response.EnsureSuccessStatusCode();

            // ShiftJISを読み込めるようにする
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            var encoding = provider.GetEncoding("shift-jis");

            // 文字コードをShiftJISを指定してCSVファイルを読み込む
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using(StreamReader reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // CSVの1行目はヘッダーなのでスキップ
                    if (line.StartsWith("国民の祝日"))
                        continue;

                    string[] columns = line.Split(',');
                    // 年が一致する祝日のみ取得
                    if (columns.Length > 0 &&  columns[0].StartsWith(targetYear))
                    {
                        holidays.Add(columns[0]);
                    }
                }
            }
        }

        return holidays;
    }

    public bool IsPublicHoliday(string date)
    {
        // 比較のために日付のフォーマットを変換
        var targetDate = DateTime.Parse(date).ToString("yyyy/M/d");
        return publicHolidays.Contains(targetDate);
    }
}

var publicHolidays = new DeterminingPublicHolidays("2021");
Console.WriteLine(publicHolidays.IsPublicHoliday("2021/01/01")); // True