const msal = require('@azure/msal-node');

const TENANT_ID = "Azureのアプリ上から取得できるテナントID";
const CLIENT_ID = "Azureのアプリ上から取得できるクライアントID";
const CLIENT_SECRET = "記事内で作成し取得したクライアントシークレットキーの値";

// 実行環境によっては変更が必要なため以下のMSのサイト記事を参照のこと
const AAD_ENDPOINT = "https://login.microsoftonline.com";
const GRAPH_ENDPOINT = "https://graph.microsoft.com";

const msalConfig = {
    auth: {
        clientId: CLIENT_ID,
        authority: AAD_ENDPOINT + '/' + TENANT_ID,
        clientSecret: CLIENT_SECRET,
    }
};

const tokenRequest = {
    scopes: [GRAPH_ENDPOINT + '/.default'],
};

const apiConfig = {
    uri: GRAPH_ENDPOINT,
};

const cca = new msal.ConfidentialClientApplication(msalConfig);

async function getToken(tokenRequest) {
    return await cca.acquireTokenByClientCredential(tokenRequest);
}

module.exports = {
    apiConfig: apiConfig,
    tokenRequest: tokenRequest,
    getToken: getToken
};