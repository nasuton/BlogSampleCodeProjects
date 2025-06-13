#include "NormalLog.h"


string NormalLog::GetCurrentTime() {
    time_t now = time(0);
    struct tm tstruct;
    char buf[80];
    localtime_s(&tstruct, &now);
    strftime(buf, sizeof(buf), "%Y-%m-%d %X", &tstruct);
    return buf;
}

void NormalLog::WriteLog(const string& message)
{
    ofstream logFile("log.txt", ios::app);
    if (!logFile.is_open()) {
        cerr << "���O�t�@�C�����J���܂���ł����B" << endl;
        return;
    }
    logFile << GetCurrentTime() << " - " << message << endl;
    logFile.close();
}

void NormalLog::WriteErrorLog(const string& message) {
    ofstream logFile("error_log.txt", ios::app);
    if (!logFile.is_open()) {
        cerr << "�G���[���O�t�@�C�����J���܂���ł����B" << endl;
        return;
    }
    logFile << GetCurrentTime() << "�G���[: " << message << endl;
    logFile.close();
}