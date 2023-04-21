#include "Probability.h"

int main()
{
	std::map<std::string, float> select =
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
    for (int i = 0; i < maxCount; i++) {
        int hogeNum = 0;
        int hugaNum = 0;
        int piyoNum = 0;
        int BiyoNum = 0;
        int judgeTrue = 0;
        int judgeFalse = 0;
        firstNum += 1;
        int secondNum = 0;
        for (int k = 0; k < 100; k++) {
            secondNum += 1;
            std::string selectOne = Probability::SelectOne(select);
            if (selectOne == "hoge.png") {
                hogeNum += 1;
            }
            else if (selectOne == "huga.png") {
                hugaNum += 1;
            }
            else if (selectOne == "piyo.png") {
                piyoNum += 1;
            }
            else if (selectOne == "Biyo.png") {
                BiyoNum += 1;
            }

            bool judgment = Probability::PercentJudgment(20.0f);
            if (judgment) {
                judgeTrue += 1;
            }
            else {
                judgeFalse += 1;
            }
        }
        printf("100連%d回目：hoge %d, huga %d, piyo %d, Biyo %d\n", firstNum, hogeNum, hugaNum, piyoNum, BiyoNum);
        printf("True: %d, False: %d\n\n", judgeTrue, judgeFalse);

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
    printf("平均値：Hoge %f, Huga %f, Piyo %f, Biyo %f, True %f, False %f\n", aveHoge, aveHuga, avePiyo, aveBiyo, aveTrue, aveFalse);
}