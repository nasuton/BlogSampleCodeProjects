// 任意の確率での排出
#ifndef __PROBABILITY_H__
#define __PROBABILITY_H__

#include <map>
#include <string>

class Probability
{
public:
    // 複数から1つ選ぶ
    static std::string SelectOne(std::map<std::string, float> targetDict);

    // 真偽の判定(trueかfalseを返す)
    static bool PercentJudgment(float perecent);
};

#endif
