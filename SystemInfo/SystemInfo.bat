@echo off

REM batファイルのパスまで移動
cd /d %~dp0

powershell "./SystemInfo.ps1"