#pragma once
#include <string>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/basic_file_sink.h"

using namespace std;
#define SPDLOG_WCHAR_FILENAMES //ファイルパスでWCHARを使用できるようにする
#define SPDLOG_WCHAR_TO_UTF8_SUPPORT //出力内容でWCHARを使用できるようにする

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
