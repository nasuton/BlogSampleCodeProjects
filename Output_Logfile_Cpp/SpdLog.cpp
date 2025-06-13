#include "SpdLog.h"

std::shared_ptr<spdlog::logger> SpdLog::file_logger_ = nullptr;
std::shared_ptr<spdlog::logger> SpdLog::error_logger_ = nullptr;

void SpdLog::Initialize() {
    static std::once_flag flag;
    std::call_once(flag, []() {
        file_logger_ = spdlog::basic_logger_mt("basic_logger", "spdlog.txt");
        error_logger_ = spdlog::basic_logger_mt("error_logger", "spderror_log.txt");
        });
}

void SpdLog::WriteLog(const std::string& message)
{
    Initialize();
    spdlog::info(message);
    file_logger_->info(message);
    file_logger_->flush();
}

void SpdLog::WriteErrorLog(const std::string& message) {
    Initialize();
    spdlog::error(message);
    error_logger_->error(message);
    error_logger_->flush();
}