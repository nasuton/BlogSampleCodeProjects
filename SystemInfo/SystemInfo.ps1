$ErrorActionPreference = "SilentlyContinue"

# 書き出し先フォルダの作成
$outPutPath = "このフォルダの中身をサポートへ送ってください"
$folderPath = Join-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) $outPutPath
if (-not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Force -Path $folderPath | Out-Null
}
Set-Location -Path (Split-Path $MyInvocation.MyCommand.Path -Parent)

Write-Host "診断情報を収集しております。"
Write-Host "このままお待ちください。"
Write-Host ""

$CPATH = $folderPath
if (-not ($CPATH.EndsWith("\"))) { 
    $CPATH += "\" 
}

$YYYYMMDD = Get-Date -Format "yyyyMMdd"
$HHMMSS = Get-Date -Format "HHmmss"

$LOGFNAME = "${CPATH}infomation.txt"
$DIAGTMP = "${CPATH}diagtmp_${env:computername}_${YYYYMMDD}_${HHMMSS}.txt"
# Shift-JISで出力するためのエンコード(PowerShell6以降でDefaultのエンコードがUTF-8になったため)
$encode_shitjis = ([System.Text.Encoding]::GetEncoding(932))

# DirectX診断ツールのテキスト書き出しオプションで実行
dxdiag /t $DIAGTMP

# 上記の情報が出力されるまで待機
while (-not (Test-Path -Path $DIAGTMP)) {
    Start-Sleep -Seconds 1
}

# ファイルに情報を取得した日付などを書き込む
"${env:computername}_${YYYYMMDD}_${HHMMSS}" | Out-File -FilePath $LOGFNAME -Encoding $encode_shitjis
# Windowsシステムの情報を収集
"Windowsシステムの情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
systeminfo | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# ドライブ情報の取得
"ドライブ情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
get-psdrive -psprovider filesystem | Select-Object Name, Used, Free, Root | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# DirectX診断ツールで書き出した情報を追記
"DirectX診断ツールの情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
Get-Content -Path $DIAGTMP -Encoding $encode_shitjis | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# ネットワーク情報を収集
"ネットワーク情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
ipconfig /all | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# ルーティングテーブルの情報を収集
"ルーティングテーブルの情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
route print | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# 現在動作しているタスクの情報を収集
"現在動作しているタスクの情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
tasklist /V /FO CSV | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
# EventLogのエラーと警告の情報を収集
"EventLogのエラーと警告の情報" | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis
Get-EventLog -LogName system -after (get-date).AddDays(-1) | Where-Object { ($_.EntryType -eq "Error") -or ($_.EntryType -eq "Warning") } | Out-File -Append -FilePath $LOGFNAME -Encoding $encode_shitjis

# 一時ファイルを削除
Remove-Item -Path $DIAGTMP

Clear-Host
Write-Host "診断情報の収集が完了しました。"
Write-Host "下記の1ファイルをサポートへご送付ください。"
Write-Host ""
Write-Host "infomation.txt"
Write-Host ""

Invoke-Item -Path $folderPath

Write-Host "こちらのウインドウは何かキーを押すと閉じます。"
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null