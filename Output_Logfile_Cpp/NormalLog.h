#pragma once
#include <fstream>
#include <iostream>
#include <string>
#include <ctime>


using namespace std;

class NormalLog
{
private:
	static string GetCurrentTime();

public:
	static void WriteLog(const string& message);
	static void WriteErrorLog(const string& message);
};

