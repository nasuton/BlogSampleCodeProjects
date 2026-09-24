#include "GraphAPIHelper.h"

#include <iterator>

namespace
{
std::string EncodeUrlComponent(const std::string& value)
{
    std::unique_ptr<char, decltype(&curl_free)> encoded(
        curl_easy_escape(nullptr, value.c_str(), 0), &curl_free);
    if (!encoded) {
        throw std::runtime_error("URL encoding failed");
    }
    return encoded.get();
}

// サイトをパス指定する場合の区切り文字は残し、各部分をエンコードする。
// 入力はURLエンコード前の文字列を想定する。
std::string EncodeSiteReference(const std::string& value)
{
    std::string result;
    size_t start = 0;
    for (size_t i = 0; i < value.size(); ++i) {
        if (value[i] == ':' || value[i] == '/' || value[i] == ',') {
            result += EncodeUrlComponent(value.substr(start, i - start));
            result += value[i];
            start = i + 1;
        }
    }
    result += EncodeUrlComponent(value.substr(start));
    return result;
}
}

size_t GraphAPIHelper::TokenWriteCallback(
    char* data, size_t size, size_t count, void* userdata) noexcept
{
    const size_t bytes = size * count;

    try {
        static_cast<std::string*>(userdata)->append(data, bytes);
        return bytes;
    }
    catch (...) {
        return 0; // libcurlに書き込み失敗を通知
    }
}

std::string GraphAPIHelper::GetAccessToken(
    const std::string& tenantId,
    const std::string& clientId,
    const std::string& clientSecret)
{
    // 関数終了時や例外発生時にも解放される
    std::unique_ptr<CURL, decltype(&curl_easy_cleanup)> client(
        curl_easy_init(), &curl_easy_cleanup);

    if (!client) {
        throw std::runtime_error("curl_easy_init failed");
    }

    // clientSecretに「+」「&」「=」などが含まれていても送信できるようにする
    const auto encode = [&](const std::string& value) {
        std::unique_ptr<char, decltype(&curl_free)> encoded(
            curl_easy_escape(client.get(), value.c_str(), 0),
            &curl_free);

        if (!encoded) {
            throw std::runtime_error("URL encoding failed");
        }

        return std::string(encoded.get());
    };

    const std::string url =
        "https://login.microsoftonline.com/" + encode(tenantId)
        + "/oauth2/v2.0/token";

    const std::string requestBody =
        "grant_type=client_credentials"
        "&scope=" + encode("https://graph.microsoft.com/.default")
        + "&client_id=" + encode(clientId)
        + "&client_secret=" + encode(clientSecret);

    std::string responseBody;

    // libcurlの設定エラーも例外にする
    const auto check = [](CURLcode result) {
        if (result != CURLE_OK) {
            throw std::runtime_error(curl_easy_strerror(result));
        }
    };

    check(curl_easy_setopt(client.get(), CURLOPT_URL, url.c_str()));

    // POSTFIELDSによりPOSTになり、既定のContent-Typeは
    // application/x-www-form-urlencodedになる
    check(curl_easy_setopt(
        client.get(), CURLOPT_POSTFIELDS, requestBody.c_str()));

    check(curl_easy_setopt(
        client.get(), CURLOPT_WRITEFUNCTION, TokenWriteCallback));
    check(curl_easy_setopt(
        client.get(), CURLOPT_WRITEDATA, &responseBody));
    check(curl_easy_setopt(client.get(), CURLOPT_TIMEOUT, 30L));

    // PostAsync(...).Resultに相当する同期実行
    check(curl_easy_perform(client.get()));

    // EnsureSuccessStatusCode()に相当
    long statusCode = 0;
    check(curl_easy_getinfo(
        client.get(), CURLINFO_RESPONSE_CODE, &statusCode));

    if (statusCode < 200 || statusCode >= 300) {
        throw std::runtime_error(
            "Token request failed: HTTP " + std::to_string(statusCode));
    }

    const auto tokenResponse = nlohmann::json::parse(responseBody);
    return tokenResponse.at("access_token").get<std::string>();
}

nlohmann::json GraphAPIHelper::HttpGetJson(
    const std::string& url, const std::string& token, bool preferNonIndexed)
{
    // nextLinkも含め、BearerトークンはMicrosoft Graphにのみ送信する。
    if (!url.starts_with("https://graph.microsoft.com/")) {
        throw std::invalid_argument("Expected a Microsoft Graph HTTPS URL");
    }
    if (token.empty() || token.find_first_of("\r\n") != std::string::npos) {
        throw std::invalid_argument("Invalid access token");
    }

    std::unique_ptr<curl_slist, decltype(&curl_slist_free_all)> headers(
        nullptr, &curl_slist_free_all);
    const auto addHeader = [&](const std::string& value) {
        curl_slist* updated = curl_slist_append(headers.get(), value.c_str());
        if (!updated) {
            throw std::runtime_error("Could not allocate HTTP headers");
        }
        headers.release();
        headers.reset(updated);
    };
    addHeader("Authorization: Bearer " + token);
    if (preferNonIndexed) {
        addHeader("Prefer: HonorNonIndexedQueriesWarningMayFailRandomly");
    }

    std::unique_ptr<CURL, decltype(&curl_easy_cleanup)> client(
        curl_easy_init(), &curl_easy_cleanup);
    if (!client) {
        throw std::runtime_error("curl_easy_init failed");
    }

    const auto check = [](CURLcode result) {
        if (result != CURLE_OK) {
            throw std::runtime_error(curl_easy_strerror(result));
        }
    };
    std::string responseBody;
    check(curl_easy_setopt(client.get(), CURLOPT_URL, url.c_str()));
    check(curl_easy_setopt(client.get(), CURLOPT_HTTPHEADER, headers.get()));
    check(curl_easy_setopt(client.get(), CURLOPT_WRITEFUNCTION, TokenWriteCallback));
    check(curl_easy_setopt(client.get(), CURLOPT_WRITEDATA, &responseBody));
    check(curl_easy_setopt(client.get(), CURLOPT_TIMEOUT, 30L));
    check(curl_easy_perform(client.get()));

    long statusCode = 0;
    check(curl_easy_getinfo(client.get(), CURLINFO_RESPONSE_CODE, &statusCode));
    if (statusCode < 200 || statusCode >= 300) {
        throw std::runtime_error(
            "Graph request failed: HTTP " + std::to_string(statusCode));
    }
    return nlohmann::json::parse(responseBody);
}

std::string GraphAPIHelper::GetSiteId(
    const std::string& siteId, const std::string& token)
{
    if (siteId.empty()) {
        throw std::invalid_argument("siteId must not be empty");
    }
    const auto response = HttpGetJson(
        "https://graph.microsoft.com/v1.0/sites/" + EncodeSiteReference(siteId),
        token, false);
    return response.at("id").get<std::string>();
}

M365GameListItemesResponse GraphAPIHelper::HttpGameListRequest(
    const std::string& url, const std::string& token)
{
    const auto response = HttpGetJson(url, token, true);
    M365GameListItemesResponse result;
    result.Value = response.at("value").get<std::vector<nlohmann::json>>();
    const auto nextLink = response.find("@odata.nextLink");
    if (nextLink != response.end() && !nextLink->is_null()) {
        result.ODataNextlink = nextLink->get<std::string>();
    }
    return result;
}

M365GameListItemesResponse GraphAPIHelper::GetGameListItem(
    const std::string& odNextLink, const std::string& token)
{
    return HttpGameListRequest(odNextLink, token);
}

std::vector<nlohmann::json> GraphAPIHelper::GetAllGameListItems(
    const std::string& siteId, const std::string& listName,
    const std::string& gameListField, const std::string& token)
{
    if (listName.empty()) {
        throw std::invalid_argument("listName must not be empty");
    }
    const std::string id = GetSiteId(siteId, token);
    const std::string expand = gameListField.empty()
        ? "fields" : "fields($select=" + gameListField + ")";
    const std::string requestUrl =
        "https://graph.microsoft.com/v1.0/sites/" + EncodeUrlComponent(id)
        + "/lists/" + EncodeUrlComponent(listName)
        + "/items?$expand=" + EncodeUrlComponent(expand)
        + "&$orderby=" + EncodeUrlComponent("fields/Title");

    auto response = HttpGameListRequest(requestUrl, token);
    std::vector<nlohmann::json> listItems = std::move(response.Value);
    while (!response.ODataNextlink.empty()) {
        // nextLinkは再構築・再エンコードせず、そのまま使う。
        response = GetGameListItem(response.ODataNextlink, token);
        listItems.insert(listItems.end(),
            std::make_move_iterator(response.Value.begin()),
            std::make_move_iterator(response.Value.end()));
    }
    return listItems;
}
