using Microsoft.VisualBasic.FileIO;
using System.Text;

internal class CSVHelper
{
    public static void WriteTsv(string path, List<string[]> datas, string[] columnNames = null)
    {
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            if (columnNames != null && columnNames.Length > 0)
            {
                // タブ区切りで書き出す
                writer.WriteLine(string.Join("\t", columnNames));
            }

            foreach (var data in datas)
            {
                // タブ区切りで書き出す
                writer.WriteLine(string.Join("\t", data));
            }
        }
    }

    public static List<string[]> ReadTsv(string path, bool headerSkip = false)
    {
        var datas = new List<string[]>();
        using (var reader = new StreamReader(path, Encoding.UTF8))
        {

            if (headerSkip)
            {
                // ヘッダー行を読み飛ばす
                string[] headers = reader.ReadLine().Split('\t');
            }
            while (!reader.EndOfStream)
            {
                // タブ区切りで読み込む
                datas.Add(reader.ReadLine().Split('\t'));
            }
        }

        return datas;
    }

    public static void WriteCsv(string path, List<string[]> datas, string[] columnNames = null)
    {
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            if (columnNames != null && columnNames.Length > 0)
            {
                // ダブルクォーテーションで囲んでカンマ区切りで書き出す
                writer.WriteLine(string.Join(",", columnNames.Select(x => $"\"{x}\"")));
            }

            foreach (var data in datas)
            {
                // ダブルクォーテーションで囲んでカンマ区切りで書き出す
                writer.WriteLine(string.Join(",", data.Select(x => $"\"{x}\"")));
            }
        }
    }

    public static List<string[]> ReadCsv(string path, bool headerSkip = false)
    {
        var datas = new List<string[]>();
        using (TextFieldParser parser = new TextFieldParser(path))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            if(headerSkip)
            {
                string[] headers = parser.ReadFields();
            }

            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                datas.Add(fields);
            }
        }

        return datas;
    }
}

try
{
    var columnNames = new string[] { "Id", "Title", "Content" };
    var datas = new List<string[]>();
    for (int i = 0; i < 10; i++)
    {
        datas.Add(new string[] { i.ToString(), $"タイトル{i},", $"内容,{i}" });
    }
    string csvPath = "test.csv";
    CSVHelper.WriteCsv(csvPath, datas, columnNames);
    var readDatas = CSVHelper.ReadCsv(csvPath, true);
    // コンソールに読み込んだデータを表示
    foreach (var data in readDatas)
    {
        Console.WriteLine(string.Join("\t", data));
    }

    string tsvPath = "test.tsv";
    CSVHelper.WriteTsv(tsvPath, datas, columnNames);
    var readTsvDatas = CSVHelper.ReadTsv(tsvPath, true);
    // コンソールに読み込んだデータを表示
    foreach (var data in readTsvDatas)
    {
        Console.WriteLine(string.Join("\t", data));
    }
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}