#pragma once
#include <string>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/basic_file_sink.h"

using namespace std;
#define SPDLOG_WCHAR_FILENAMES //�t�@�C���p�X��WCHAR���g�p�ł���悤�ɂ���
#define SPDLOG_WCHAR_TO_UTF8_SUPPORT //�o�͓��e��WCHAR���g�p�ł���悤�ɂ���

class SpdLog
{
private:
	static std::shared_ptr<spdlog::logger> file_logger_;
	static std::shared_ptr<spdlog::logger> error_logger_;
	static void Initialize();

public:
	static void WriteLog(const string& message);
	static void WriteErrorLog(const string& message);
};
