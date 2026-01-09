using System.Text;

/// <summary>
/// 日本語対応のあいまい検索クラス
/// N-gram + ひらがな正規化を使用した類似度計算を提供します
/// </summary>
public class JapaneseFuzzySearch
{
    private readonly int _ngramSize;
    private readonly double _threshold;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="ngramSize">N-gramのサイズ（デフォルト: 2）</param>
    /// <param name="threshold">類似度の閾値（0.0～1.0、デフォルト: 0.3）</param>
    public JapaneseFuzzySearch(int ngramSize = 2, double threshold = 0.3)
    {
        _ngramSize = ngramSize;
        _threshold = threshold;
    }

    #region 正規化メソッド

    /// <summary>
    /// 日本語文字列を正規化
    /// - カタカナ → ひらがな変換
    /// - 全角英数字 → 半角変換
    /// - 大文字 → 小文字変換
    /// </summary>
    /// <param name="input">入力文字列</param>
    /// <returns>正規化された文字列</returns>
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            char normalized = c;

            // カタカナ（ァ-ヶ）をひらがな（ぁ-ゖ）に変換
            if (c >= 'ァ' && c <= 'ヶ')
            {
                normalized = (char)(c - 0x60);
            }
            // 全角英大文字 → 半角小文字
            else if (c >= 'Ａ' && c <= 'Ｚ')
            {
                normalized = (char)(c - 'Ａ' + 'a');
            }
            // 全角英小文字 → 半角小文字
            else if (c >= 'ａ' && c <= 'ｚ')
            {
                normalized = (char)(c - 'ａ' + 'a');
            }
            // 全角数字 → 半角数字
            else if (c >= '０' && c <= '９')
            {
                normalized = (char)(c - '０' + '0');
            }
            // 半角英大文字 → 小文字
            else if (c >= 'A' && c <= 'Z')
            {
                normalized = char.ToLower(c);
            }

            sb.Append(normalized);
        }

        return sb.ToString();
    }

    /// <summary>
    /// カタカナをひらがなに変換
    /// </summary>
    /// <param name="input">入力文字列</param>
    /// <returns>ひらがなに変換された文字列</returns>
    public static string KatakanaToHiragana(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (c >= 'ァ' && c <= 'ヶ')
            {
                sb.Append((char)(c - 0x60));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    #endregion

    #region N-gram メソッド

    /// <summary>
    /// 文字列からN-gramを生成
    /// </summary>
    /// <param name="text">入力文字列</param>
    /// <param name="n">N-gramのサイズ</param>
    /// <returns>N-gramのリスト</returns>
    public static List<string> GetNGrams(string text, int n = 2)
    {
        var ngrams = new List<string>();

        if (string.IsNullOrEmpty(text) || text.Length < n)
        {
            // 文字列がN未満の場合は文字列自体を返す
            if (!string.IsNullOrEmpty(text))
                ngrams.Add(text);
            return ngrams;
        }

        for (int i = 0; i <= text.Length - n; i++)
        {
            ngrams.Add(text.Substring(i, n));
        }

        return ngrams;
    }

    /// <summary>
    /// N-gramベースのJaccard類似度を計算
    /// </summary>
    /// <param name="text1">比較文字列1</param>
    /// <param name="text2">比較文字列2</param>
    /// <param name="n">N-gramのサイズ</param>
    /// <returns>類似度（0.0～1.0）</returns>
    public static double CalculateNGramSimilarity(string text1, string text2, int n = 2)
    {
        // 正規化
        var normalized1 = Normalize(text1);
        var normalized2 = Normalize(text2);

        // N-gram生成
        var ngrams1 = GetNGrams(normalized1, n).ToHashSet();
        var ngrams2 = GetNGrams(normalized2, n).ToHashSet();

        if (ngrams1.Count == 0 && ngrams2.Count == 0)
            return 1.0;

        if (ngrams1.Count == 0 || ngrams2.Count == 0)
            return 0.0;

        // Jaccard係数を計算
        int intersection = ngrams1.Intersect(ngrams2).Count();
        int union = ngrams1.Union(ngrams2).Count();

        return union == 0 ? 0.0 : (double)intersection / union;
    }

    /// <summary>
    /// N-gramベースのDice係数を計算（Jaccardより部分一致に強い）
    /// </summary>
    /// <param name="text1">比較文字列1</param>
    /// <param name="text2">比較文字列2</param>
    /// <param name="n">N-gramのサイズ</param>
    /// <returns>類似度（0.0～1.0）</returns>
    public static double CalculateDiceSimilarity(string text1, string text2, int n = 2)
    {
        var normalized1 = Normalize(text1);
        var normalized2 = Normalize(text2);

        var ngrams1 = GetNGrams(normalized1, n).ToHashSet();
        var ngrams2 = GetNGrams(normalized2, n).ToHashSet();

        if (ngrams1.Count == 0 && ngrams2.Count == 0)
            return 1.0;

        if (ngrams1.Count == 0 || ngrams2.Count == 0)
            return 0.0;

        int intersection = ngrams1.Intersect(ngrams2).Count();

        return (2.0 * intersection) / (ngrams1.Count + ngrams2.Count);
    }

    #endregion


    #region 検索メソッド

    /// <summary>
    /// リストから類似する項目を検索
    /// </summary>
    /// <param name="query">検索クエリ</param>
    /// <param name="items">検索対象のリスト</param>
    /// <returns>類似度でソートされた結果リスト</returns>
    public List<SearchResult> Search(string query, IEnumerable<string> items)
    {
        var results = new List<SearchResult>();

        foreach (var item in items)
        {
            double similarity = CalculateNGramSimilarity(query, item, _ngramSize);

            if (similarity >= _threshold)
            {
                results.Add(new SearchResult
                {
                    Text = item,
                    Similarity = similarity
                });
            }
        }

        return results.OrderByDescending(r => r.Similarity).ToList();
    }

    /// <summary>
    /// リストから類似する項目を検索（セレクター使用版）
    /// </summary>
    /// <typeparam name="T">検索対象の型</typeparam>
    /// <param name="query">検索クエリ</param>
    /// <param name="items">検索対象のリスト</param>
    /// <param name="selector">検索対象のプロパティを取得するセレクター</param>
    /// <returns>類似度でソートされた結果リスト</returns>
    public List<SearchResult<T>> Search<T>(string query, IEnumerable<T> items, Func<T, string> selector)
    {
        var results = new List<SearchResult<T>>();

        foreach (var item in items)
        {
            string text = selector(item);
            double similarity = CalculateNGramSimilarity(query, text, _ngramSize);

            if (similarity >= _threshold)
            {
                results.Add(new SearchResult<T>
                {
                    Item = item,
                    Text = text,
                    Similarity = similarity
                });
            }
        }

        return results.OrderByDescending(r => r.Similarity).ToList();
    }

    /// <summary>
    /// 部分一致を含む検索を行う
    /// </summary>
    /// <param name="query">検索クエリ</param>
    /// <param name="items">検索対象のリスト</param>
    /// <returns>類似度でソートされた結果リスト</returns>
    public List<SearchResult> SearchWithPartialMatch(string query, IEnumerable<string> items)
    {
        var results = new List<SearchResult>();
        var normalizedQuery = Normalize(query);

        foreach (var item in items)
        {
            var normalizedItem = Normalize(item);
            double similarity;

            // 完全一致
            if (normalizedItem == normalizedQuery)
            {
                similarity = 1.0;
            }
            // 部分一致（クエリがアイテムに含まれる）
            else if (normalizedItem.Contains(normalizedQuery))
            {
                similarity = 0.9 * ((double)normalizedQuery.Length / normalizedItem.Length);
            }
            // N-gram類似度
            else
            {
                similarity = CalculateNGramSimilarity(query, item, _ngramSize);
            }

            if (similarity >= _threshold)
            {
                results.Add(new SearchResult
                {
                    Text = item,
                    Similarity = similarity
                });
            }
        }

        return results.OrderByDescending(r => r.Similarity).ToList();
    }

    #endregion
}

/// <summary>
/// 検索結果を表すクラス
/// </summary>
public class SearchResult
{
    /// <summary>
    /// 検索にヒットしたテキスト
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 類似度（0.0～1.0）
    /// </summary>
    public double Similarity { get; set; }

    public override string ToString()
    {
        return $"{Text} (類似度: {Similarity:P1})";
    }
}

/// <summary>
/// 検索結果を表すジェネリッククラス
/// </summary>
/// <typeparam name="T">検索対象の型</typeparam>
public class SearchResult<T>
{
    /// <summary>
    /// 検索にヒットしたアイテム
    /// </summary>
    public T Item { get; set; } = default!;

    /// <summary>
    /// 検索にヒットしたテキスト
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 類似度（0.0～1.0）
    /// </summary>
    public double Similarity { get; set; }

    public override string ToString()
    {
        return $"{Text} (類似度: {Similarity:P1})";
    }
}

try 
{
    // 検索対象のデータ
    var products = new List<string>
    {
        "東京タワー",
        "とうきょうタワー",
        "トウキョウタワー",
        "東京スカイツリー",
        "大阪城",
        "おおさかじょう",
        "名古屋城",
        "富士山",
        "ふじさん",
        "フジサン",
        "Microsoft Office",
        "マイクロソフト オフィス",
        "ＭＩＣＲＯＳＯＦＴ　ＯＦＦＩＣＥ"
    };

    // 検索インスタンスを作成（N-gram=2, 閾値=0.2）
    var fuzzySearch = new JapaneseFuzzySearch(ngramSize: 2, threshold: 0.2);

    // テスト1: カタカナ・ひらがな混在検索
    Console.WriteLine("【検索1】「とうきょう」で検索:");
    var results1 = fuzzySearch.Search("とうきょう", products);
    foreach (var result in results1)
    {
        Console.WriteLine($"  {result}");
    }
    Console.WriteLine();

    // テスト2: 漢字で検索
    Console.WriteLine("【検索2】「東京」で検索:");
    var results2 = fuzzySearch.Search("東京", products);
    foreach (var result in results2)
    {
        Console.WriteLine($"  {result}");
    }
    Console.WriteLine();

    // テスト3: 全角・半角混在の英語検索
    Console.WriteLine("【検索3】「microsoft」で検索:");
    var results3 = fuzzySearch.Search("microsoft", products);
    foreach (var result in results3)
    {
        Console.WriteLine($"  {result}");
    }
    Console.WriteLine();

    // テスト4: 部分一致を含む検索
    Console.WriteLine("【検索4】「ふじ」で部分一致検索:");
    var results4 = fuzzySearch.SearchWithPartialMatch("ふじ", products);
    foreach (var result in results4)
    {
        Console.WriteLine($"  {result}");
    }
    Console.WriteLine();

    // テスト5: 正規化の確認
    Console.WriteLine("【正規化の例】");
    Console.WriteLine($"  「トウキョウタワー」→「{JapaneseFuzzySearch.Normalize("トウキョウタワー")}」");
    Console.WriteLine($"  「ＭＩＣＲＯＳＯＦＴ」→「{JapaneseFuzzySearch.Normalize("ＭＩＣＲＯＳＯＦＴ")}」");
    Console.WriteLine($"  「１２３ABC」→「{JapaneseFuzzySearch.Normalize("１２３ABC")}」");
    Console.WriteLine();

    // テスト6: N-gram生成の確認
    Console.WriteLine("【N-gram生成の例】「東京タワー」のbi-gram:");
    var ngrams = JapaneseFuzzySearch.GetNGrams("東京タワー", 2);
    Console.WriteLine($"  [{string.Join(", ", ngrams)}]");
    Console.WriteLine();

    // テスト7: 類似度計算の比較
    Console.WriteLine("【類似度計算の比較】");
    var pairs = new (string, string)[]
    {
        ("東京タワー", "トウキョウタワー"),
        ("東京タワー", "東京スカイツリー"),
        ("富士山", "ふじさん"),
        ("Microsoft", "ＭＩＣＲＯＳＯＦＴ"),
    };

    foreach (var (s1, s2) in pairs)
    {
        double jaccard = JapaneseFuzzySearch.CalculateNGramSimilarity(s1, s2);
        double dice = JapaneseFuzzySearch.CalculateDiceSimilarity(s1, s2);
        Console.WriteLine($"  「{s1}」vs「{s2}」");
        Console.WriteLine($"    Jaccard: {jaccard:P1}, Dice: {dice:P1}");
    }
    Console.WriteLine();

    // テスト8: オブジェクトリストの検索
    Console.WriteLine("【オブジェクトリストの検索】");
    var books = new List<(int Id, string Title)>
    {
        (1, "吾輩は猫である"),
        (2, "わがはいはねこである"),
        (3, "坊っちゃん"),
        (4, "ぼっちゃん"),
        (5, "人間失格"),
        (6, "にんげんしっかく"),
    };

    Console.WriteLine("「しっかく」で書籍を検索:");
    var bookResults = fuzzySearch.Search("しっかく", books, b => b.Title);
    foreach (var result in bookResults)
    {
        Console.WriteLine($"  ID:{result.Item.Id} - {result.Text} (類似度: {result.Similarity:P1})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"エラーが発生しました: {ex.Message}");
}