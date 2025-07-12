using CSVDiffTool;

try
{
    string file1 = @".\sample1.csv";
    string file2 = @".\sample2.csv";
    string outputFile = @".\CSVDiff_Output.csv";
    string? keyColumn = null;
    //string? keyColumn = "Id"; // キー列を指定する場合はコメントアウトを外す

    Console.WriteLine($"ファイル1: {file1}");
    Console.WriteLine($"ファイル2: {file2}");
    Console.WriteLine($"出力ファイル: {outputFile}");

    if (!string.IsNullOrEmpty(keyColumn))
    {
        Console.WriteLine($"キー列: {keyColumn}");
    }

    var comparer = new CSVDiffComparer();
    List<DiffResult> results;

    if (!string.IsNullOrEmpty(keyColumn))
    {
        results = comparer.CompareByKey(file1, file2, keyColumn);
    }
    else
    {
        results = comparer.CompareByRowNumber(file1, file2);
    }

    comparer.WriteDiffResults(results, outputFile);
    comparer.DisplaySummary(results);

    Console.WriteLine("\n処理が完了しました。");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
