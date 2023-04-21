using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// 任意の確率で結果を排出
/// </summary>
namespace TestCSharp
{
    public class Probability
    {
        static public string SelectOne(Dictionary<string, float> targetDict)
        {
            double total = 0.0f;
            foreach(var item in targetDict)
            {
                total += item.Value;
            }

            // 累計確率までのランダム数値を生成
            Random rand = new Random();
            double min = 1.0;
            double max = total;
            double range = max - min;
            double randDouble = rand.NextDouble();
            double scaled = (randDouble * range) + min;
            float randomResult = (float)scaled;

            // 生成した数値から個々の確率を引いていく
            foreach (var dic in targetDict)
            {
                randomResult -= dic.Value;
                // 0以下になった時点でのKeyを返す
                if (randomResult < 0.0f)
                {
                    return dic.Key;
                }
            }

            // ここまで来たらエラー
            return "";
        }

        static public bool PercentJudgment(float percent)
        {
            // 少数以下の桁数(現在は第1まで)
            int decimalPoint = 1;

            // 小数点を消すための倍率
            int rate = (int)Math.Pow(10, decimalPoint);

            // 乱数の上限と判定ボーダーを設定
            float randomLimit = 100 * rate;
            float border = rate * percent;

            Random rand = new Random();
            double min = 1.0;
            double max = randomLimit;
            double range = max - min;
            double randDouble = rand.NextDouble();
            double scaled = (randDouble * range) + min;
            float randomResult = (float)scaled;

            return randomResult < border;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, float> select = new Dictionary<string, float>()
            {
                {"hoge.png", 20.0f},
                {"huga.png", 25.0f},
                {"piyo.png", 15.0f},
                {"Biyo.png", 40.0f}
            };

            int maxCount = 10000;
            int firstNum = 0;
            int totalhoge = 0;
            int totalhuga = 0;
            int totalpiyo = 0;
            int totalBiyo = 0;
            int totalTrue = 0;
            int totalFalse = 0;

            for (int i = 0; i < maxCount; i++)
            {
                int hogeNum = 0;
                int hugaNum = 0;
                int piyoNum = 0;
                int BiyoNum = 0;
                int judgeTrue = 0;
                int judgeFalse = 0;
                firstNum += 1;
                int secondNum = 0;
                for (int k = 0; k < 100; k++)
                {
                    secondNum += 1;
                    string selectOne = Probability.SelectOne(select);
                    if (selectOne == "hoge.png")
                    {
                        hogeNum += 1;
                    }
                    else if (selectOne == "huga.png")
                    {
                        hugaNum += 1;
                    }
                    else if (selectOne == "piyo.png")
                    {
                        piyoNum += 1;
                    }
                    else if (selectOne == "Biyo.png")
                    {
                        BiyoNum += 1;
                    }

                    bool judgment = Probability.PercentJudgment(20.0f);
                    if (judgment)
                    {
                        judgeTrue += 1;
                    }
                    else
                    {
                        judgeFalse += 1;
                    }

                    // 少し間を空けないとすべて同じ結果になってしまうため
                    Thread.Sleep(10);
                }
                Console.WriteLine("100連{0}回目：hoge {1}, huga {2}, piyo {3}, Biyo {4}", firstNum, hogeNum, hugaNum, piyoNum, BiyoNum);
                Console.WriteLine("True: {0}, False: {1}\n", judgeTrue, judgeFalse);

                totalhoge += hogeNum;
                totalhuga += hugaNum;
                totalpiyo += piyoNum;
                totalBiyo += BiyoNum;
                totalTrue += judgeTrue;
                totalFalse += judgeFalse;
            }
            float aveHoge = totalhoge / maxCount;
            float aveHuga = totalhuga / maxCount;
            float avePiyo = totalpiyo / maxCount;
            float aveBiyo = totalBiyo / maxCount;
            float aveTrue = totalTrue / maxCount;
            float aveFalse = totalFalse / maxCount;
            Console.WriteLine("平均値：Hoge {0}, Huga {1}, Piyo {2}, Biyo {3}, True {4}, False {5}", aveHoge, aveHuga, avePiyo, aveBiyo, aveTrue, aveFalse);
        }
    }
}
