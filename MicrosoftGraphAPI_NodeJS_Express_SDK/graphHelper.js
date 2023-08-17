require('isomorphic-fetch');
const azure = require('@azure/identity');
const graph = require('@microsoft/microsoft-graph-client');
const authProviders = require('@microsoft/microsoft-graph-client/authProviders/azureTokenCredentials');

let _settings = undefined;
let _clientSecretCredential = undefined;
let _appClient = undefined;

function initializeGraphForAppOnlyAuth(settings) {
  // 各設定項目がNull判定
  if (!settings) {
    throw new Error('Settings cannot be undefined');
  }

  _settings = settings;

  if (!_settings) {
    throw new Error('Settings cannot be undefined');
  }

  if (!_clientSecretCredential) {
    _clientSecretCredential = new azure.ClientSecretCredential(
      _settings.tenantId,
      _settings.clientId,
      _settings.clientSecret
    );
  }

  if (!_appClient) {
    const authProvider = new authProviders.TokenCredentialAuthenticationProvider(
      _clientSecretCredential, {
      scopes: ['https://graph.microsoft.com/.default']
    });

    _appClient = graph.Client.initWithMiddleware({
      authProvider: authProvider
    });
  }

  console.log('initializeGraph Sucess!!');
}

// ユーザー一覧取得処理
async function getUsersAsync() {
  if (!_appClient) {
    throw new Error('Graph has not been initialized for app-only auth');
  }

  return _appClient?.api('/users')
    .select(['displayName', 'mail'])                                  // 取得項目名：表示名、id、メールアドレス
    .top(25)                                                          // 先頭25件取得
    .orderby('displayName')                                           // 表示名を基準にソート
    .get();
}

// 対象のカレンダー情報一覧を取得
async function getCalendarViewAsync(targetId, startDay, endDay) {
  if (!_appClient) {
    throw new Error('Graph has not been initialized for app-only auth');
  }

  const start = `${startDay}T00:00:00`;                               // 取得開始日付と時間をくっつける
  const end = `${endDay}T23:59:59`;                                   // 取得終了日付と時間をくっつける

  return _appClient?.api(`/users/${targetId}/calendarView`)
    .header('Prefer', 'outlook.timezone="Tokyo Standard Time"')       // 時刻を東京に指定(何も指定しないとUTCとなる)
    .query({
      startDateTime: start,                                           // 取得開始期間
      endDateTime: end                                                // 取得終了期間
    })
    .select(['subject', 'start', 'end', 'organizer', 'attendees'])    // 取得項目名：会議名、開始時間、終了時間、主催者、出席者
    .top(25)                                                          // 先頭25件取得
    .get();
}

module.exports = {
  initializeGraphForAppOnlyAuth: initializeGraphForAppOnlyAuth,
  getUsersAsync: getUsersAsync,
  getCalendarViewAsync: getCalendarViewAsync
};
