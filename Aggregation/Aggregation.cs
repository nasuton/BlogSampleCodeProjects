using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// アイテム作成日時集計
/// </summary>
namespace TestCSharp
{
    public class Aggregation
    {
        private DateTime currentTime;

        public Aggregation()
        {
            currentTime = DateTime.Now;
        }

        /// <summary>
        /// 時間を30分ごとに区切る
        /// </summary>
        /// <param name="itemCreateTimes">作成時間等のカウント対象の配列</param>
        /// <param name="descending">降順フラグ</param>
        public void AggregationCount(List<DateTime> itemCreateTimes, bool descending = false)
        {
            Console.WriteLine($"対象データ数：{itemCreateTimes.Count}");
            // 集計データ確認用
            int dicCount1 = 0;
            int dicCount2 = 0;

            // アイテムを30分ごとのスロットに分類する用変数
            Dictionary<DateTime, int> slotCounts1 = new Dictionary<DateTime, int>();
            Dictionary<DateTime, int> slotCounts2 = new Dictionary<DateTime, int>();
            var dateTime = currentTime;

            // 現時刻から8時間前を取得
            DateTime eightHoursAgo = dateTime.AddHours(-8);
            Console.WriteLine($"現時刻から8時間前：{eightHoursAgo}");

            // 振り分け用の配列作成
            DateTime currentTimeSlot = eightHoursAgo;
            List<DateTime> timeSlots = new List<DateTime>();
            while (currentTimeSlot < dateTime)
            {
                timeSlots.Add(currentTimeSlot);
                currentTimeSlot = currentTimeSlot.AddMinutes(30);
            }

            foreach (DateTime itemCreateTime in itemCreateTimes)
            {
                foreach (DateTime timeSlot in timeSlots)
                {
                    if (itemCreateTime >= timeSlot && itemCreateTime < timeSlot.AddMinutes(30))
                    {
                        if (!slotCounts1.ContainsKey(timeSlot))
                        {
                            slotCounts1[timeSlot] = 1;
                            dicCount1++;
                        }
                        else
                        {
                            slotCounts1[timeSlot]++;
                            dicCount1++;
                        }
                        break; // アイテムは1つの時間スロットにのみカウントされる
                    }
                }

                DateTime slotStartTime = new DateTime(itemCreateTime.Year, itemCreateTime.Month, itemCreateTime.Day, itemCreateTime.Hour, 0, 0);
                int minutes = itemCreateTime.Minute;
                // 30分より前か後か判定する
                int slotIndex = minutes / 30;
                DateTime slotKey = slotStartTime.AddMinutes(slotIndex * 30);

                if (!slotCounts2.ContainsKey(slotKey))
                {
                    slotCounts2[slotKey] = 1;
                    dicCount2++;
                }
                else
                {
                    slotCounts2[slotKey]++;
                    dicCount2++;
                }
            }

            var sortedDictionary1 = new Dictionary<DateTime, int>();
            var sortedDictionary2 = new Dictionary<DateTime, int>();
            if (descending)
            {
                // キーを昇順にソート
                sortedDictionary1 = slotCounts1
                    .OrderBy(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                sortedDictionary2 = slotCounts2
                    .OrderBy(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                // キーを降順にソート
                sortedDictionary1 = slotCounts1
                    .OrderByDescending(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                sortedDictionary2 = slotCounts2
                    .OrderByDescending(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            Console.WriteLine($"集計合計数(実行時間からの30分区切り版)：{dicCount1}");
            Console.WriteLine($"集計合計数(1時間を30分ずつに区切った版)：{dicCount2}");
            Console.WriteLine();
            Console.WriteLine("集計結果 : 実行時間からの30分区切り版");
            // 集計結果の出力
            foreach (var kvp in sortedDictionary1)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value} アイテム");
            }
            Console.WriteLine();
            Console.WriteLine("集計結果 : 1時間を30分ずつに区切った版");
            foreach (var kvp in sortedDictionary2)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value} アイテム");
            }
        }

        /// <summary>
        /// テスト用データCSV作成関数
        /// </summary>
        /// <param name="csvPath">作成先CSVパス</param>
        public void TargetCSVDateCreate(string csvPath)
        {
            if (csvPath != null && csvPath != string.Empty)
            {
                List<string[]> csvData = new List<string[]>();

                // 8時間前の時刻を計算
                var dateTime = currentTime;
                DateTime targetTime = dateTime.AddHours(-8);

                // ランダムな分を引いていく
                Random random = new Random();
                while (dateTime > targetTime)
                {
                    int minutesToSubtract = random.Next(1, 6); // 1分から5分までランダムに引く
                    dateTime = dateTime.AddMinutes(-minutesToSubtract);
                    string[] row = new string[] { dateTime.ToString("yyyy/MM/dd HH:mm:ss") };
                    csvData.Add(row);
                    Console.WriteLine($"引いた時間: {minutesToSubtract}分, 残り時間: {(dateTime - targetTime).TotalMinutes}分");
                }

                try
                {
                    using (StreamWriter writer = new StreamWriter(csvPath))
                    {
                        foreach (string[] row in csvData)
                        {
                            string line = string.Join(",", row);
                            writer.WriteLine(line);
                        }
                        writer.Close();
                    }

                    Console.WriteLine($"CSVファイル '{csvPath}' にデータを書き込みました。");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CSVファイルの書き込みエラー: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("引数が設定されていません");
            }
        }

        /// <summary>
        /// CSVファイルの読み込み
        /// </summary>
        /// <param name="filePath">対象CSVファイルパス</param>
        /// <returns>CSVデータ配列</returns>
        public  List<string[]> ReadCsvFile(string filePath)
        {
            List<string[]> csvData = new List<string[]>();

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(','); // CSVの各列をカンマで分割

                        csvData.Add(values);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSVファイルの読み込みエラー: {ex.Message}");
            }

            return csvData;
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var agger = new Aggregation();
            var datetimeList = new List<DateTime>();
            string csvPath = @".\CreateTime.csv";
            // テストデータ作成
            agger.TargetCSVDateCreate(csvPath);
            // データ集計
            var result = agger.ReadCsvFile(csvPath);
            foreach (var row in result)
            {
                Console.WriteLine($"読み込んだCSVデータ : {row[0]}");
                datetimeList.Add(DateTime.Parse(row[0]));
            }
            Console.WriteLine();
            // 昇順にソートして実施
            Console.WriteLine("昇順");
            agger.AggregationCount(datetimeList);
            Console.WriteLine();
            // 降順にソートして実施
            Console.WriteLine("降順");
            agger.AggregationCount(datetimeList, true);
        }
    }
}
