#include <iostream>

#include "NormalLog.h"
#include "SpdLog.h"

using namespace std;

int main()
{
    cout << "Hello World!\n";

    NormalLog::WriteLog("TestLog");
	NormalLog::WriteErrorLog("TestErrorLog");

    SpdLog::WriteLog("SPDLog");
	SpdLog::WriteErrorLog("SPDLogError");
}
