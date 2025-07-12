using System.Text;

namespace CSVDiffTool
{
    /// <summary>
    /// CSVファイルの行を表すクラス
    /// </summary>
    public class CSVRow
    {
        public Dictionary<string, string> Columns { get; set; }
        public int RowNumber { get; set; }
        
        public CSVRow()
        {
            Columns = new Dictionary<string, string>();
        }
        
        public string GetValue(string columnName)
        {
            return Columns.ContainsKey(columnName) ? Columns[columnName] : string.Empty;
        }
        
        public void SetValue(string columnName, string value)
        {
            Columns[columnName] = value ?? string.Empty;
        }
    }
    
    /// <summary>
    /// 差分結果を表すクラス
    /// </summary>
    public class DiffResult
    {
        public CSVRow Row { get; set; }
        public DiffType Type { get; set; }
        public string SourceFile { get; set; }
        public string KeyValue { get; set; }
    }
    
    /// <summary>
    /// 差分の種類
    /// </summary>
    public enum DiffType
    {
        OnlyInFile1,
        OnlyInFile2,
        Modified_File1,
        Modified_File2
    }
    
    /// <summary>
    /// CSVファイル差分比較ツール
    /// </summary>
    public class CSVDiffComparer
    {
        private readonly string _encoding;
        
        public CSVDiffComparer(string encoding = "UTF-8")
        {
            _encoding = encoding;
        }
        
        /// <summary>
        /// CSVファイルを読み込む
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>CSVRowのリスト</returns>
        public List<CSVRow> ReadCSV(string filePath)
        {
            var rows = new List<CSVRow>();
            var encoding = Encoding.GetEncoding(_encoding);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");
            }
            
            using (var reader = new StreamReader(filePath, encoding))
            {
                string headerLine = reader.ReadLine();
                if (string.IsNullOrEmpty(headerLine))
                {
                    return rows;
                }
                
                var headers = ParseCSVLine(headerLine);
                int rowNumber = 1;
                
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    rowNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var values = ParseCSVLine(line);
                    var row = new CSVRow { RowNumber = rowNumber };
                    
                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                    {
                        row.SetValue(headers[i], values[i]);
                    }
                    
                    rows.Add(row);
                }
            }
            
            return rows;
        }
        
        /// <summary>
        /// CSV行をパースする（簡易版）
        /// </summary>
        /// <param name="line">CSV行</param>
        /// <returns>値の配列</returns>
        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString());
            return result.ToArray();
        }
        
        /// <summary>
        /// キーベースでCSVファイルを比較
        /// </summary>
        /// <param name="file1">ファイル1のパス</param>
        /// <param name="file2">ファイル2のパス</param>
        /// <param name="keyColumn">キー列名</param>
        /// <returns>差分結果のリスト</returns>
        public List<DiffResult> CompareByKey(string file1, string file2, string keyColumn)
        {
            var rows1 = ReadCSV(file1);
            var rows2 = ReadCSV(file2);
            var results = new List<DiffResult>();
            
            Console.WriteLine($"ファイル1: {rows1.Count} 行");
            Console.WriteLine($"ファイル2: {rows2.Count} 行");
            Console.WriteLine($"キー列 '{keyColumn}' を使用して比較中...");
            
            // ハッシュテーブルを作成
            var hash1 = rows1.ToDictionary(r => r.GetValue(keyColumn), r => r);
            var hash2 = rows2.ToDictionary(r => r.GetValue(keyColumn), r => r);
            
            // ファイル1にのみ存在するレコード
            foreach (var kvp in hash1)
            {
                if (!hash2.ContainsKey(kvp.Key))
                {
                    results.Add(new DiffResult
                    {
                        Row = kvp.Value,
                        Type = DiffType.OnlyInFile1,
                        SourceFile = file1,
                        KeyValue = kvp.Key
                    });
                }
            }
            
            // ファイル2にのみ存在するレコード
            foreach (var kvp in hash2)
            {
                if (!hash1.ContainsKey(kvp.Key))
                {
                    results.Add(new DiffResult
                    {
                        Row = kvp.Value,
                        Type = DiffType.OnlyInFile2,
                        SourceFile = file2,
                        KeyValue = kvp.Key
                    });
                }
            }
            
            // 両方に存在するが内容が異なるレコード
            foreach (var kvp in hash1)
            {
                if (hash2.ContainsKey(kvp.Key))
                {
                    var row1 = kvp.Value;
                    var row2 = hash2[kvp.Key];
                    
                    if (!AreRowsEqual(row1, row2, keyColumn))
                    {
                        results.Add(new DiffResult
                        {
                            Row = row1,
                            Type = DiffType.Modified_File1,
                            SourceFile = file1,
                            KeyValue = kvp.Key
                        });
                        
                        results.Add(new DiffResult
                        {
                            Row = row2,
                            Type = DiffType.Modified_File2,
                            SourceFile = file2,
                            KeyValue = kvp.Key
                        });
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 行番号ベースでCSVファイルを比較
        /// </summary>
        /// <param name="file1">ファイル1のパス</param>
        /// <param name="file2">ファイル2のパス</param>
        /// <returns>差分結果のリスト</returns>
        public List<DiffResult> CompareByRowNumber(string file1, string file2)
        {
            var rows1 = ReadCSV(file1);
            var rows2 = ReadCSV(file2);
            var results = new List<DiffResult>();
            
            Console.WriteLine($"ファイル1: {rows1.Count} 行");
            Console.WriteLine($"ファイル2: {rows2.Count} 行");
            Console.WriteLine("行番号ベースで比較中...");
            
            int maxRows = Math.Max(rows1.Count, rows2.Count);
            
            for (int i = 0; i < maxRows; i++)
            {
                var row1 = i < rows1.Count ? rows1[i] : null;
                var row2 = i < rows2.Count ? rows2[i] : null;
                
                if (row1 == null)
                {
                    // ファイル2にのみ存在
                    results.Add(new DiffResult
                    {
                        Row = row2,
                        Type = DiffType.OnlyInFile2,
                        SourceFile = file2,
                        KeyValue = (i + 1).ToString()
                    });
                }
                else if (row2 == null)
                {
                    // ファイル1にのみ存在
                    results.Add(new DiffResult
                    {
                        Row = row1,
                        Type = DiffType.OnlyInFile1,
                        SourceFile = file1,
                        KeyValue = (i + 1).ToString()
                    });
                }
                else
                {
                    // 両方に存在、内容を比較
                    if (!AreRowsEqual(row1, row2))
                    {
                        results.Add(new DiffResult
                        {
                            Row = row1,
                            Type = DiffType.Modified_File1,
                            SourceFile = file1,
                            KeyValue = (i + 1).ToString()
                        });
                        
                        results.Add(new DiffResult
                        {
                            Row = row2,
                            Type = DiffType.Modified_File2,
                            SourceFile = file2,
                            KeyValue = (i + 1).ToString()
                        });
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 2つの行が等しいかどうかを判定
        /// </summary>
        /// <param name="row1">行1</param>
        /// <param name="row2">行2</param>
        /// <param name="excludeColumn">比較から除外する列名</param>
        /// <returns>等しい場合はtrue</returns>
        private bool AreRowsEqual(CSVRow row1, CSVRow row2, string excludeColumn = null)
        {
            var allColumns = row1.Columns.Keys.Union(row2.Columns.Keys);
            
            foreach (var column in allColumns)
            {
                if (column == excludeColumn) continue;
                
                if (row1.GetValue(column) != row2.GetValue(column))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 差分結果をCSVファイルに出力
        /// </summary>
        /// <param name="results">差分結果</param>
        /// <param name="outputPath">出力パス</param>
        public void WriteDiffResults(List<DiffResult> results, string outputPath)
        {
            if (results.Count == 0)
            {
                Console.WriteLine("差分は見つかりませんでした。");
                return;
            }
            
            var encoding = Encoding.GetEncoding(_encoding);
            
            using (var writer = new StreamWriter(outputPath, false, encoding))
            {
                // ヘッダーを取得（最初の行から）
                var firstRow = results.First().Row;
                var headers = firstRow.Columns.Keys.ToList();
                headers.AddRange(new[] { "DiffType", "SourceFile", "KeyValue" });
                
                // ヘッダーを書き込み
                writer.WriteLine(string.Join(",", headers.Select(EscapeCSVValue)));
                
                // データを書き込み
                foreach (var result in results)
                {
                    var values = new List<string>();
                    
                    foreach (var header in headers)
                    {
                        if (header == "DiffType")
                        {
                            values.Add(EscapeCSVValue(result.Type.ToString()));
                        }
                        else if (header == "SourceFile")
                        {
                            values.Add(EscapeCSVValue(result.SourceFile));
                        }
                        else if (header == "KeyValue")
                        {
                            values.Add(EscapeCSVValue(result.KeyValue));
                        }
                        else
                        {
                            values.Add(EscapeCSVValue(result.Row.GetValue(header)));
                        }
                    }
                    
                    writer.WriteLine(string.Join(",", values));
                }
            }
            
            Console.WriteLine($"差分結果を {outputPath} に出力しました。");
        }
        
        /// <summary>
        /// CSV値をエスケープ
        /// </summary>
        /// <param name="value">値</param>
        /// <returns>エスケープされた値</returns>
        private string EscapeCSVValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            
            return value;
        }
        
        /// <summary>
        /// 差分サマリーを表示
        /// </summary>
        /// <param name="results">差分結果</param>
        public void DisplaySummary(List<DiffResult> results)
        {
            if (results.Count == 0)
            {
                Console.WriteLine("差分は見つかりませんでした。");
                return;
            }
            
            Console.WriteLine($"\n{results.Count} 件の差分が見つかりました。");
            Console.WriteLine("\n=== 差分サマリー ===");
            
            var summary = results.GroupBy(r => r.Type)
                                 .Select(g => new { Type = g.Key, Count = g.Count() })
                                 .OrderBy(x => x.Type);
            
            foreach (var item in summary)
            {
                Console.WriteLine($"{item.Type}: {item.Count} 件");
            }
        }
    }
}
