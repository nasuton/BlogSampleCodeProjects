#######################GraphAPI接続 start
$tenantName = "アプリが登録されたテナントID"
$clientUserid = "作成したAzure上のアプリID"
$clientUserSecret = "Azure上のアプリで作成したクライアントシークレットキーの値"
$userName = "認証時に使用するユーザーアドレス(二段階認証の設定がされていないもの)"
$userPass = "userNameのパスワード"

$ReqUserTokenBody = @{
    Grant_Type    = "password"
    username      = $userName
    password      = $userPass
    client_Id     = $clientUserid
    Client_Secret = $clientUserSecret
    Scope         = "Calendars.Read Calendars.Read.Shared" # Azureアプリに含まれていないとエラーが発生する
}

$TokenUserResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantName/oauth2/v2.0/token" -Method POST -Body $ReqUserTokenBody
#######################GraphAPI接続 end

$TargetRoom = "取得対象となる会議室のメールアドレス"

#######################GraphAPIで情報取得(アプリケーション委任版)
try {
    $apiUrl = 'https://graph.microsoft.com/v1.0/users/' + $TargetRoom + '/calendarView?startDateTime=2023-01-01T00:00:00&endDateTime=2023-05-31T23:59:59&$select=subject,start,end,organizer,attendees'
    while ($true) {
        $User_Data = Invoke-RestMethod -Headers @{Authorization = "Bearer $($TokenUserResponse.access_token)"} -Uri $apiUrl -Method Get
        $User_Calendars = ($User_Data | select-object Value ).Value
        $User_Calendars

        $NextLink = $User_Data.'@odata.nextLink'
        $apiUrl = $NextLink
        if ($null -eq $NextLink) {
            break;
        }
    }
    
} catch {
    Write-Host $_.Exception
}
#######################GraphAPIで情報取得 end

