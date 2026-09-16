# PC環境情報の収集

`SystemInfo.ps1` は、Windows PowerShell 5.1 / PowerShell 7 でPCの診断情報を収集します。Windowsの設定変更、修復、更新のインストール、再起動は実行しません。64bit版PowerShellでの実行を推奨します。

## 実行方法

このフォルダーでPowerShellを開いて実行します。

```powershell
.\SystemInfo.ps1
```

一般ユーザーでも収集を続けます。BitLockerなど権限が必要な項目まで確認する場合は、管理者として開いたPowerShellで実行してください。実行ポリシーで禁止されている環境では、組織の運用ルールに従ってください。

自動実行や、フォルダー表示・キー入力待ちが不要な場合:

```powershell
.\SystemInfo.ps1 -NoOpenFolder -NoPause
```

保存先、イベント期間、取得上限、待機時間を指定する場合:

```powershell
.\SystemInfo.ps1 -OutputDirectory 'D:\Support Logs' -EventLogDays 7 -MaxEventsPerLog 2000 -CommandTimeoutSeconds 180 -NoOpenFolder -NoPause
```

| パラメーター | 既定値 | 用途 |
| --- | --- | --- |
| OutputDirectory | スクリプトと同じ場所の「このフォルダの中身をサポートへ送ってください」 | 収集フォルダーの親ディレクトリ |
| EventLogDays | 1 | 過去何日分のイベントを収集するか（1～365） |
| MaxEventsPerLog | 1000 | System / Applicationそれぞれの最大件数（最新順） |
| CommandTimeoutSeconds | 120 | 外部コマンドごとの待機上限（秒）。dxdiagにも適用 |
| LowDiskSpacePercent | 10 | 固定ディスクの空き容量がこの割合未満なら注意点に記録 |
| NoOpenFolder | 指定なし | 指定すると収集後にフォルダーを開かない |
| NoPause | 指定なし | 指定すると終了時にキー入力を待たない |

保存先には `PC名_日時_識別子` のサブフォルダーを実行ごとに作成します。既存の収集結果を上書きしません。出力はすべてUTF-8 BOM付きで、日本語や空白を含むパスにも対応します。

| 出力ファイル | 内容 |
| --- | --- |
| infomation.txt | 診断情報、取得失敗の理由、運用・保守の確認ポイント |
| infomation.txt_processlist.txt | プロセス詳細のCSV。001.batと同じファイル名を維持。取得成功時に作成 |
| collection-status.csv | 項目別のOK / FAILED、所要時間、失敗理由 |

`FAILED` は権限不足、コマンド・機能の非搭載、タイムアウト等で**未確認**という意味です。PCの故障を確定するものではありません。取得対象が0件の場合は、その旨を記録します。出力先への書き込み自体に失敗した場合は実行を終了します。

外部コマンドとDirectX診断はタイムアウト時に今回起動したプロセスを終了します。CIM照会は1操作あたり30秒を指定しています。その他のWindowsモジュールもあるため、スクリプト全体の実行時間を保証する上限ではありません。