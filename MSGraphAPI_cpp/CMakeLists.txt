cmake_minimum_required(VERSION 4.3)
project(graphAPI)

set(CMAKE_CXX_STANDARD 26)

include(FetchContent)

# ----- nlohmann/json -----
FetchContent_Declare(
    nlohmann_json
    GIT_REPOSITORY https://github.com/nlohmann/json.git
    GIT_TAG        v3.11.3
)
FetchContent_MakeAvailable(nlohmann_json)

# ----- libcurl -----
FetchContent_Declare(
    curl
    GIT_REPOSITORY https://github.com/curl/curl.git
    GIT_TAG        curl-8_14_1
)
set(BUILD_SHARED_LIBS    OFF CACHE BOOL "" FORCE)
set(BUILD_TESTING        OFF CACHE BOOL "" FORCE)
set(CURL_USE_OPENSSL     OFF CACHE BOOL "" FORCE)
set(CURL_USE_SCHANNEL    ON  CACHE BOOL "" FORCE)   # Windows 標準 TLS
set(CURL_USE_LIBPSL OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(curl)

add_executable(graphAPI main.cpp
        GraphAPIHelper.cpp
        GraphAPIHelper.h)

target_link_libraries(graphAPI PRIVATE
    nlohmann_json::nlohmann_json
    CURL::libcurl
)
