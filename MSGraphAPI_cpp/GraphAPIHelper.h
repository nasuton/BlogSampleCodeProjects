#ifndef GRAPHAPI_GRAPHAPIHELPER_H
#define GRAPHAPI_GRAPHAPIHELPER_H

#include <curl/curl.h>
#include <nlohmann/json.hpp>

#include <memory>
#include <stdexcept>
#include <string>
#include <vector>

struct M365GameListItemesResponse
{
    std::vector<nlohmann::json> Value;
    std::string ODataNextlink;
};

class GraphAPIHelper
{
public:
    static size_t TokenWriteCallback(
        char* data, size_t size, size_t count, void* userdata) noexcept;

    std::string GetAccessToken(
        const std::string& tenantId,
        const std::string& clientId,
        const std::string& clientSecret);

    M365GameListItemesResponse HttpGameListRequest(
        const std::string& url, const std::string& token);

    M365GameListItemesResponse GetGameListItem(
        const std::string& odNextLink, const std::string& token);

    // gameListField: "Title,Column1,Column2"のような列の内部名。
    // 空文字の場合はfields全体を取得します。
    // siteIdはサイトID、または"contoso.sharepoint.com:/sites/example"形式。
    // listNameにはリストID、またはリスト名を指定します。
    std::vector<nlohmann::json> GetAllGameListItems(
        const std::string& siteId, const std::string& listName,
        const std::string& gameListField, const std::string& token);

private:
    std::string GetSiteId(const std::string& siteId, const std::string& token);
    nlohmann::json HttpGetJson(
        const std::string& url, const std::string& token, bool preferNonIndexed);
};


#endif //GRAPHAPI_GRAPHAPIHELPER_H
