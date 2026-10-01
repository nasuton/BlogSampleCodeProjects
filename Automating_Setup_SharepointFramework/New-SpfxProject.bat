@echo off
rem ※ このファイルは Shift_JIS (CP932) で保存すること。UTF-8 にすると cmd が日本語を誤読して壊れる。
rem New-SpfxProject.ps1 を bat から呼び出すラッパー。
rem 使い方: New-SpfxProject.bat -Path D:\work\SPFx\MyProject -SolutionName my-project -ComponentName MyWebPart -TenantDomain contoso.sharepoint.com -SkipInstall
rem
rem - ExecutionPolicy はこのプロセス内でのみ Bypass
rem - 引数はそのまま PowerShell スクリプトへ渡す
rem - 実行ログは logs\New-SpfxProject_<日時>.log に保存される (PS1 側の Start-Transcript)
rem - 終了時に pause するのでダブルクリック起動でもウィンドウが閉じない

setlocal
set "SCRIPT=%~dp0New-SpfxProject.ps1"

where pwsh >nul 2>nul
if %ERRORLEVEL%==0 (
  pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
)
set "RC=%ERRORLEVEL%"

echo.
if "%RC%"=="0" (
  echo [OK] 正常終了しました。
) else (
  echo [ERROR] New-SpfxProject.ps1 が終了コード %RC% で失敗しました。
  echo         詳細ログ: %~dp0logs\
)
echo.
pause
endlocal & exit /b %RC%
