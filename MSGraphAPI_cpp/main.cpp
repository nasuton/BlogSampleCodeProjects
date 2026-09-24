#include "GraphAPIHelper.h"

#include <exception>
#include <iostream>
#include <string>

int main()
{
    // TODO: 後ほど、それぞれの値を記入してください。
    const std::string tenantId = "AzureアプリのテナントID";
    const std::string clientId = "AzureアプリのクライアントID";
    const std::string clientSecret = "Azureアプリのクライアントシークレット";
    const std::string siteId = "tenant名.sharepoint.com:/sites/対象サイトURL";
    const std::string listName = "対象リスト表示名";

    if (tenantId.empty() || clientId.empty() || clientSecret.empty()) {
        std::cerr << "tenantId、clientId、clientSecretを設定してください。\n";
        return 1;
    }

    const CURLcode initResult = curl_global_init(CURL_GLOBAL_DEFAULT);
    if (initResult != CURLE_OK) {
        std::cerr << "libcurlの初期化に失敗: "
                  << curl_easy_strerror(initResult) << '\n';
        return 1;
    }

    int exitCode = 0;
    try {
        GraphAPIHelper helper;
        const std::string token = helper.GetAccessToken(
            tenantId, clientId, clientSecret);

        std::cout << "アクセストークンを取得しました。\n";
        std::cout << "アクセストークン: " << token << '\n';

        auto listItemes = helper.GetAllGameListItems(siteId, listName, "", token);
        for (auto item : listItemes)
        {
            std::cout << item.dump(4) << '\n';
        }
    }
    catch (const std::exception& error) {
        std::cerr << "アクセストークンの取得に失敗: " << error.what() << '\n';
        exitCode = 1;
    }

    curl_global_cleanup();
    return exitCode;
}
