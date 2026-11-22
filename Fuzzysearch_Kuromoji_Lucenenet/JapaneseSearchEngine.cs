using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Ja;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
// System.IO.Directory と名前が衝突するため、Lucene 側の Directory を明示的に使う
using Directory = Lucene.Net.Store.Directory;

namespace FuzzySearch;

/// <summary>
/// 検索結果 1 件分を表すレコード。
/// </summary>
/// <param name="Id">ドキュメントの一意な ID</param>
/// <param name="Title">タイトル</param>
/// <param name="Body">本文</param>
/// <param name="Score">検索クエリとの関連度スコア（大きいほど関連が高い）</param>
public record SearchHit(string Id, string Title, string Body, float Score);

/// <summary>
/// Kuromoji（JapaneseAnalyzer）を使った日本語全文検索エンジン。
///
/// 【全体の流れ】
///   1. コンストラクタ   : インデックスの保存先・形態素解析器・書き込み器を準備する
///   2. AddDocument      : 文書を形態素解析して転置インデックスに登録する
///   3. Commit           : 登録内容を確定（ディスクに書き込み）する
///   4. Search           : 検索語を同じ形態素解析器で分割し、インデックスから一致する文書を探す
///   5. Dispose          : リソースを解放する
///
/// 【なぜ形態素解析が必要か】
///   英語は空白で単語が区切られているが、日本語は区切りがない。
///   「東京タワーに行く」をそのまま検索するには「東京タワー」「行く」のように
///   単語単位へ分割する必要があり、この分割を Kuromoji が担当する。
/// </summary>
public sealed class JapaneseSearchEngine : IDisposable
{
    /// <summary>
    /// Lucene の互換バージョン。アナライザーや書き込み器に同じ値を渡すことで
    /// インデックスの形式や解析ルールが一致するようにする。
    /// </summary>
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;

    /// <summary>インデックスファイルの保存先（メモリ or ディスク）</summary>
    private readonly Directory _directory;

    /// <summary>
    /// 形態素解析器。文章を単語（トークン）に分割する。
    /// 登録時と検索時で同じ解析器を使うことが重要。
    /// 分割ルールが異なると、同じ単語でも一致しなくなる。
    /// </summary>
    private readonly Analyzer _analyzer;

    /// <summary>
    /// インデックスへの書き込みを担当する。
    /// 文書の追加・更新・削除はすべてこのオブジェクト経由で行う。
    /// </summary>
    private readonly IndexWriter _writer;

    /// <summary>
    /// 検索エンジンを初期化する。
    /// </summary>
    /// <param name="indexPath">
    /// インデックスを保存するフォルダのパス。
    /// null の場合はメモリ上に作成され、プログラム終了時に消える。
    /// パスを指定すると次回起動時にも同じインデックスを再利用できる。
    /// </param>
    public JapaneseSearchEngine(string? indexPath = null)
    {
        // --- 1. インデックスの保存先を決める ---
        // RAMDirectory : メモリ上（高速だが揮発性。テストや小規模データ向け）
        // FSDirectory  : ファイルシステム上（永続化できる。本番向け）
        _directory = indexPath is null
            ? new RAMDirectory()
            : FSDirectory.Open(indexPath);

        // --- 2. 日本語形態素解析器（Kuromoji）を作る ---
        // JapaneseAnalyzer は内部で以下の処理を順番に行うパイプライン:
        //   JapaneseTokenizer      : 辞書（IPADIC）を使って文章を単語に分割
        //   JapaneseBaseFormFilter : 活用形を基本形に戻す（例: 「走った」→「走る」）
        //   JapanesePartOfSpeechStopFilter : 助詞・記号など検索に不要な品詞を除去
        //   CJKWidthFilter         : 全角英数→半角、半角カナ→全角カナに統一
        //   StopFilter             : 「これ」「それ」などのストップワードを除去
        //   JapaneseKatakanaStemFilter : カタカナ語末尾の長音を除去（「サーバー」→「サーバ」）
        //   LowerCaseFilter        : 英字を小文字に統一
        _analyzer = new JapaneseAnalyzer(
            Version,
            // ユーザー辞書。固有名詞や専門用語を追加したい場合にここへ渡す
            userDict: null,
            // 分割モード
            //   NORMAL   : 「関西国際空港」をそのまま 1 語として扱う
            //   SEARCH   : 「関西国際空港」に加えて「関西」「国際」「空港」も出力する
            //              → 「空港」で検索してもヒットするようになる（再現率が上がる）
            //   EXTENDED : SEARCH に加え、未知語を 1 文字ずつ分割する
            mode: JapaneseTokenizerMode.SEARCH,
            // 検索対象から除外する単語の一覧（デフォルトのストップワードを使用）
            stopwords: JapaneseAnalyzer.DefaultStopSet,
            // 検索対象から除外する品詞の一覧（デフォルトでは助詞・助動詞・記号など）
            stoptags: JapaneseAnalyzer.DefaultStopTags);

        // --- 3. インデックス書き込み器を作る ---
        var config = new IndexWriterConfig(Version, _analyzer)
        {
            // CREATE_OR_APPEND : インデックスが無ければ新規作成、あれば追記
            // CREATE           : 毎回作り直す（既存データを消す）
            // APPEND           : 既存インデックスが無ければエラー
            OpenMode = OpenMode.CREATE_OR_APPEND
        };
        _writer = new IndexWriter(_directory, config);
    }

    /// <summary>
    /// 文書をインデックスに登録する。同じ ID の文書が既にあれば上書きする。
    ///
    /// 【内部で起きること】
    ///   1. title と body が _analyzer によって単語に分割される
    ///      例: 「東京の観光スポット」→「東京」「観光」「スポット」
    ///   2. 各単語について「この単語はどの文書に含まれるか」を記録する（転置インデックス）
    ///      例: 観光 → [文書1]、空港 → [文書2]
    ///   3. Field.Store.YES のフィールドは元の文字列もそのまま保存され、
    ///      検索結果として取り出せるようになる
    /// </summary>
    /// <param name="id">文書を一意に識別する ID</param>
    /// <param name="title">タイトル</param>
    /// <param name="body">本文</param>
    public void AddDocument(string id, string title, string body)
    {
        var doc = new Document
        {
            // StringField : 形態素解析せず、値をそのまま 1 つの単語として登録する
            //               ID やコードなど「完全一致」で扱いたい値に使う
            new StringField("id", id, Field.Store.YES),

            // TextField : 形態素解析して単語単位で登録する
            //             全文検索の対象にしたいフィールドに使う
            new TextField("title", title, Field.Store.YES),
            new TextField("body", body, Field.Store.YES)
        };

        // UpdateDocument : 「id フィールドが一致する文書を削除してから追加」を 1 回で行う
        // AddDocument を使うと同じ ID の文書が重複して登録されてしまうため、こちらを使う
        _writer.UpdateDocument(new Term("id", id), doc);
    }

    /// <summary>
    /// これまでの AddDocument の内容を確定させる。
    ///
    /// IndexWriter は性能のために変更をメモリ上にためており、
    /// Commit を呼ぶまではディスクに書き込まれない。
    /// FSDirectory 使用時にプログラムが途中で落ちると、未 Commit の変更は失われる。
    /// なお、本クラスの Search は IndexWriter から直接 Reader を開くため、
    /// Commit 前でも検索結果には反映される（ニアリアルタイム検索）。
    /// </summary>
    public void Commit() => _writer.Commit();

    /// <summary>
    /// title と body を対象に検索する。
    ///
    /// 【内部で起きること】
    ///   1. 検索語を登録時と同じ _analyzer で単語に分割する
    ///      例: 「日本 料理」→「日本」「料理」
    ///   2. 分割した単語をすべて含む文書を転置インデックスから探す（DefaultOperator = AND）
    ///   3. 各文書にスコアを付けて関連度順に並べる
    ///      - 単語の出現回数が多いほど高スコア
    ///      - 珍しい単語ほど高スコア（全文書に出てくる単語はあまり意味がない）
    ///      - title でヒットしたものは body より 2 倍重み付け
    ///
    /// 【使えるクエリ構文の例】
    ///   観光 空港        → 両方を含む（AND）
    ///   観光 OR 空港     → どちらかを含む
    ///   "関西 国際"      → フレーズ検索（隣接して出現）
    ///   title:観光       → 特定フィールドのみ
    ///   学習~            → あいまい検索（表記ゆれを許容）
    /// </summary>
    /// <param name="queryText">検索文字列</param>
    /// <param name="maxHits">返す最大件数</param>
    /// <returns>関連度の高い順に並んだ検索結果</returns>
    public IReadOnlyList<SearchHit> Search(string queryText, int maxHits = 10)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return [];

        // --- 1. インデックスの読み取り器を開く ---
        // IndexWriter から直接開くことで、Commit 前の最新状態も読める（NRT: Near Real Time）
        // Reader は開いた時点のスナップショットなので、検索のたびに開き直す
        using var reader = DirectoryReader.Open(_writer, applyAllDeletes: true);
        var searcher = new IndexSearcher(reader);

        // --- 2. 検索文字列を Query オブジェクトに変換するパーサーを作る ---
        // MultiFieldQueryParser : 複数フィールドを横断して検索するためのパーサー
        // 「観光」と入力すると内部的に (title:観光 OR body:観光) のようなクエリが作られる
        var parser = new MultiFieldQueryParser(
            Version,
            ["title", "body"],   // 検索対象フィールド
            _analyzer,           // 登録時と同じ解析器で検索語を分割する（重要）
            // フィールドごとの重み。title でヒットしたほうがスコアが高くなる
            new Dictionary<string, float> { ["title"] = 2.0f, ["body"] = 1.0f })
        {
            // 複数の単語をスペース区切りで入力したときの結合方法
            //   AND : すべての単語を含む文書だけヒット（絞り込み型）
            //   OR  : いずれかの単語を含む文書がヒット（デフォルト）
            DefaultOperator = Operator.AND
        };

        // --- 3. 検索文字列を解析して Query を作る ---
        Query query;
        try
        {
            query = parser.Parse(queryText);
        }
        catch (ParseException)
        {
            // ユーザーが「(」や「"」などの特殊文字を閉じ忘れると構文エラーになる。
            // その場合は特殊文字をすべてエスケープして、ただの文字列として検索し直す
            query = parser.Parse(QueryParserBase.Escape(queryText));
        }

        // --- 4. 検索を実行する ---
        // TopDocs にはスコア上位 maxHits 件の「文書番号とスコア」だけが入っている
        var topDocs = searcher.Search(query, maxHits);

        // --- 5. 文書番号から実際の文書内容を取り出して結果を組み立てる ---
        var hits = new List<SearchHit>(topDocs.ScoreDocs.Length);
        foreach (var sd in topDocs.ScoreDocs)
        {
            // searcher.Doc で Field.Store.YES にしたフィールドの元の値を取得できる
            var doc = searcher.Doc(sd.Doc);
            hits.Add(new SearchHit(
                doc.Get("id"),
                doc.Get("title"),
                doc.Get("body"),
                sd.Score));
        }
        return hits;
    }

    /// <summary>
    /// デバッグ用: テキストが形態素解析でどう分割されるかを確認する。
    ///
    /// 「なぜこの検索語でヒットしないのか」を調べるときに、
    /// 登録文書と検索語の両方をこのメソッドに通して分割結果を比較すると原因が分かる。
    /// 例: 「関西国際空港へのアクセス」→ 関西 / 関西国際空港 / 国際 / 空港 / アクセス
    ///     （「へ」「の」は助詞なので除去されている）
    /// </summary>
    /// <param name="text">解析したいテキスト</param>
    /// <returns>分割された単語の一覧</returns>
    public IReadOnlyList<string> Tokenize(string text)
    {
        var tokens = new List<string>();

        // GetTokenStream : 指定フィールド用の解析パイプラインを取得する
        // フィールド名によって解析方法を変える Analyzer もあるため、名前を渡す必要がある
        using var stream = _analyzer.GetTokenStream("body", text);

        // TokenStream は「現在のトークン」の情報を Attribute 経由で公開する
        // ICharTermAttribute : トークンの文字列そのもの
        // （他に位置や品詞などを取る Attribute もある）
        var termAttr = stream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();

        // Reset → IncrementToken を繰り返す → End という呼び出し順序が Lucene の決まり
        stream.Reset();
        while (stream.IncrementToken())   // 次のトークンへ進む。無くなったら false
        {
            tokens.Add(termAttr.ToString());
        }
        stream.End();
        return tokens;
    }

    /// <summary>
    /// リソースを解放する。
    /// IndexWriter の Dispose は内部で Commit も行うため、
    /// 未 Commit の変更があってもここで確定される。
    /// 解放の順序は「書き込み器 → 解析器 → 保存先」と、生成の逆順にする。
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
        _analyzer.Dispose();
        _directory.Dispose();
    }
}
