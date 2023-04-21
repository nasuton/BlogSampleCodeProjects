// 任意の確率での排出
#include "Probability.h"
#include <random>
#include <math.h>
#include <assert.h>

std::string Probability::SelectOne(std::map<std::string, float> targetDict)
{
    float total = 0.0f;
    // 累計確率
    for (auto per = targetDict.begin(); per != targetDict.end(); per++)
    {
        total += per->second;
    }

    // 累計確率までのランダム数値を生成
    std::random_device rand_device;
    std::mt19937 mersenne(rand_device());
    std::uniform_real_distribution<float> rand(0.0f, total);
    float randomResult = rand(mersenne);

    // 生成した数値から個々の確率を引いていく
    for (auto per = targetDict.begin(); per != targetDict.end(); per++)
    {
        randomResult -= per->second;
        // 0以下になった時点でのKeyを返す
        if (randomResult < 0.0f)
        {
            return per->first;
        }
    }

    // ここまできたらエラーを返す
    assert("I could not draw.");

    return "";
}

bool Probability::PercentJudgment(float percent)
{
    // 少数以下の桁数(現在は第1まで)
    int decimalPoint = 1;

    // 小数点を消すための倍率
    float rate = powf(10, decimalPoint);

    // 乱数の上限と判定ボーダーを設定
    float randomLimit = 100 * rate;
    float border = rate * percent;

    std::random_device rand_device;
    std::mt19937 mersenne(rand_device());
    std::uniform_real_distribution<float> rand(0.0f, randomLimit);
    float randomResult = rand(mersenne);

    return randomResult < border;
}