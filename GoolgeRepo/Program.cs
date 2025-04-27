using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;

void ScreenPageViewsReport(BetaAnalyticsDataClient client, string propertyId)
{
    // リクエストを構築
    RunReportRequest request = new RunReportRequest
    {
        Property = $"properties/{propertyId}", // プロパティIDを指定 (GA4のプロパティID)
        Dimensions = { new Dimension { Name = "date" } },
        Metrics = { new Metric { Name = "screenPageViews" } },
        DateRanges = { new DateRange { StartDate = "2024-04-01", EndDate = "2025-03-31" } },
    };

    // レポートを実行
    RunReportResponse response = client.RunReport(request);

    Console.WriteLine("ScreenPageViewsのレポート実行結果:");
    // 結果を出力
    foreach (var row in response.Rows)
    {
        Console.WriteLine($"{row.DimensionValues[0].Value}: {row.MetricValues[0].Value}");
    }
    Console.WriteLine("ScreenPageViewsのレポート実行完了");
}

void ActiveUsersCountryReport(BetaAnalyticsDataClient client, string propertyId)
{
    // リクエストを構築
    RunReportRequest request = new RunReportRequest
    {
        Property = $"properties/{propertyId}", // プロパティIDを指定 (GA4のプロパティID)
        Dimensions =
        {
            new Dimension { Name = "country" },
            new Dimension { Name = "region" },
            new Dimension { Name = "city" }
        },
        Metrics = { new Metric { Name = "activeUsers" } },
        DateRanges = { new DateRange { StartDate = "2024-04-01", EndDate = "2025-03-31" } },
    };

    // レポートを実行
    RunReportResponse response = client.RunReport(request);

    Console.WriteLine("ActiveUsersCountryのレポート実行結果:");
    // 結果を出力
    foreach (var row in response.Rows)
    {
        Console.WriteLine($"{row.DimensionValues[0].Value}, {row.DimensionValues[1].Value},  {row.DimensionValues[2].Value}: {row.MetricValues[0].Value}");
    }
    Console.WriteLine("ActiveUsersCountryのレポート実行完了");
}

void SessionsReport(BetaAnalyticsDataClient client, string propertyId)
{
    // リクエストを構築
    RunReportRequest request = new RunReportRequest
    {
        Property = $"properties/{propertyId}", // プロパティIDを指定 (GA4のプロパティID)
        Dimensions =
        {
            //new Dimension { Name = "deviceCategory"},
            new Dimension { Name = "sessionDefaultChannelGroup" } 
        },
        Metrics =
        {
            new Metric { Name = "sessions" } // セッション数を取得
        },
        DateRanges = { new DateRange { StartDate = "2024-04-01", EndDate = "2025-03-31" } },    // 日付範囲を指定
    };

    // レポートを実行
    RunReportResponse response = client.RunReport(request);

    Console.WriteLine("Sessionsのレポート実行結果:");
    // 結果を出力
    foreach (var row in response.Rows)
    {
        Console.WriteLine($"{row.DimensionValues[0].Value}: {row.MetricValues[0].Value}");
    }
    Console.WriteLine("Sessionsのレポート実行完了");
}

void PageViewsWithDetailsReport(BetaAnalyticsDataClient client, string propertyId)
{
    // リクエストを構築
    RunReportRequest request = new RunReportRequest
    {
        Property = $"properties/{propertyId}", // プロパティIDを指定 (GA4のプロパティID)
        Dimensions =
        {
            new Dimension { Name = "pageTitle" }, // ページタイトル
            new Dimension { Name = "unifiedScreenClass" }, // スクリーンクラス
            new Dimension { Name = "pagePath" } // ページパス
        },
        Metrics =
        {
            new Metric { Name = "screenPageViews" } // 表示回数
        },
        DateRanges =
        {
            new DateRange { StartDate = "2024-04-01", EndDate = "2025-03-31" } // 日付範囲
        },
        OrderBys =
        {
            new OrderBy
            {
                Metric = new OrderBy.Types.MetricOrderBy { MetricName = "screenPageViews" },
                Desc = true // 表示回数で降順ソート
            }
        },
        Limit = 10  // 上位10件を取得
    };

    // レポートを実行
    RunReportResponse response = client.RunReport(request);

    Console.WriteLine("PageViewsWithDetailsのレポート実行結果:");
    // 結果を出力
    foreach (var row in response.Rows)
    {
        Console.WriteLine($"Page Title: {row.DimensionValues[0].Value}, Screen Class: {row.DimensionValues[1].Value}, Page URL: {row.DimensionValues[2].Value}, Page Views: {row.MetricValues[0].Value}");
    }
    Console.WriteLine("PageViewsWithDetailsのレポート実行完了");
}

try
{
    string propertyId = "GoogleAnalyticsのプロパティIDを指定";
    string jsonPath = "service-account.json"; // サービスアカウントのJSONファイルのパスを指定

    // サービスアカウントの認証情報を読み込む
    GoogleCredential credential = GoogleCredential.FromFile(jsonPath);

    // クライアントを初期化
    BetaAnalyticsDataClient client = new BetaAnalyticsDataClientBuilder
    {
        Credential = credential
    }.Build();

    ScreenPageViewsReport(client, propertyId);
    ActiveUsersCountryReport(client, propertyId);
    SessionsReport(client, propertyId);
    PageViewsWithDetailsReport(client, propertyId);
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}