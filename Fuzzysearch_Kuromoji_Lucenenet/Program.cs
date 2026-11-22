using FuzzySearch;

// 検索エンジンを生成する。
// 引数なし → メモリ上にインデックスを作る（終了時に消える）
// 永続化したい場合は new JapaneseSearchEngine(@"D:\index") のようにパスを渡す
// using により、スコープを抜けるときに自動で Dispose される
using var engine = new JapaneseSearchEngine();

// --- 文書の登録 ---
// 各文書は (ID, タイトル, 本文) の 3 つで構成される。
// 登録時に Kuromoji が本文を単語に分割し、転置インデックスを作る
engine.AddDocument("1", "東京の観光スポット", "東京タワーやスカイツリーなど、東京には多くの観光名所があります。");
engine.AddDocument("2", "関西国際空港へのアクセス", "大阪市内から関西国際空港へは電車やバスで行くことができます。");
engine.AddDocument("3", "日本料理の魅力", "寿司や天ぷらなどの日本料理は世界中で人気です。");
engine.AddDocument("4", "機械学習入門", "機械学習はデータからパターンを学習する技術です。");

// 登録内容を確定する（ディスク保存時はここで書き込まれる）
engine.Commit();

// --- 形態素解析の確認 ---
// 検索がうまくいかないときは、まずここで単語の分割のされ方を確認するとよい
Console.WriteLine("=== 形態素解析の例 ===");
Console.WriteLine(string.Join(" / ", engine.Tokenize("関西国際空港へのアクセス")));
Console.WriteLine();

// --- 検索の実行 ---
// 「空港」      : SEARCH モードのおかげで「関西国際空港」の文書もヒットする
// 「日本 料理」 : スペース区切りは AND 条件になる
foreach (var q in new[] { "観光", "空港", "関西国際空港", "日本 料理", "学習", "TestHoge" })
{
    Console.WriteLine($"=== 検索: \"{q}\" ===");
    var hits = engine.Search(q);
    if (hits.Count == 0) Console.WriteLine("  (該当なし)");
    foreach (var hit in hits)
    {
        // Score は関連度。同じ単語が多く含まれるほど、またタイトルに含まれるほど高くなる
        Console.WriteLine($"  [{hit.Score:F3}] {hit.Id}: {hit.Title}");
    }
    Console.WriteLine();
}
