#######################GraphAPI接続 start
$tenantName = "アプリが登録されたテナントID"
$clientAppid = "作成したAzure上のアプリID"
$clientAppSecret = "Azure上のアプリで作成したクライアントシークレットキーの値"

$ReqAppTokenBody = @{
    Grant_Type    = "client_credentials"
    Scope         = "https://graph.microsoft.com/.default"
    client_Id     = $clientAppid
    Client_Secret = $clientAppSecret
}

$TokenAppResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantName/oauth2/v2.0/token" -Method POST -Body $ReqAppTokenBody
#######################GraphAPI接続 end

$TargetRoom = "取得対象となる会議室のメールアドレス"

#######################GraphAPIで情報取得(アプリケーション委任版)
try {
    $apiUrl = 'https://graph.microsoft.com/v1.0/users/' + $TargetRoom + '/calendarView?startDateTime=2023-01-01T00:00:00&endDateTime=2023-05-31T23:59:59&$select=subject,start,end,organizer,attendees'
    while ($true) {
        $App_Data = Invoke-RestMethod -Headers @{Authorization = "Bearer $($TokenAppResponse.access_token)"} -Uri $apiUrl -Method Get
        $App_Calendars = ($App_Data | select-object Value ).Value
        $App_Calendars

        $NextLink = $App_Data.'@odata.nextLink'
        $apiUrl = $NextLink
        if($null -eq $NextLink) {
            break;
        }
    }
    
} catch {
    Write-Host $_.Exception
}
#######################GraphAPIで情報取得 end

