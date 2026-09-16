#Requires -Version 5.1
<#
.SYNOPSIS
Windows PC の運用・保守用情報を収集します（設定変更や修復は行いません）。
.DESCRIPTION
Windows PowerShell 5.1 / PowerShell 7 用。出力は UTF-8 BOM 付きです。
実行ごとにフォルダーを作成し、取得失敗を記録して残りの収集を続けます。
管理者権限がない場合も実行できますが、一部の情報は取得できません。
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'このフォルダの中身をサポートへ送ってください'),
    [ValidateRange(1, 365)][int]$EventLogDays = 1,
    [ValidateRange(1, 100000)][int]$MaxEventsPerLog = 1000,
    [ValidateRange(1, 600)][int]$CommandTimeoutSeconds = 120,
    [ValidateRange(1, 50)][int]$LowDiskSpacePercent = 10,
    [switch]$NoOpenFolder,
    [switch]$NoPause
)

$ErrorActionPreference = 'Stop'
$startedAt = Get-Date
$utf8 = New-Object System.Text.UTF8Encoding($true)
$runName = '{0}_{1}_{2}' -f $env:COMPUTERNAME, $startedAt.ToString('yyyyMMdd_HHmmss_fff'), ([guid]::NewGuid().ToString('N').Substring(0, 8))
$folderPath = (New-Item -ItemType Directory -Path (Join-Path $OutputDirectory $runName) -Force).FullName
$logPath = Join-Path $folderPath 'infomation.txt'
$statusPath = Join-Path $folderPath 'collection-status.csv'
$processPath = Join-Path $folderPath 'infomation.txt_processlist.txt'
$statuses = New-Object 'System.Collections.Generic.List[object]'
$findings = New-Object 'System.Collections.Generic.List[string]'
[System.IO.File]::WriteAllText($logPath, '', $utf8)

function Write-Report {
    param([AllowEmptyString()][string]$Text)
    [System.IO.File]::AppendAllText($logPath, $Text + [Environment]::NewLine, $utf8)
}

function Write-CsvFile {
    param([string]$Path, [object[]]$Rows)
    $csv = @($Rows | ConvertTo-Csv -NoTypeInformation)
    [System.IO.File]::WriteAllLines($Path, [string[]]$csv, $utf8)
}

function Invoke-Section {
    param([string]$Name, [scriptblock]$Action)
    Write-Host ('収集中: {0}' -f $Name)
    Write-Report ("`r`n===== {0} =====" -f $Name)
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $state = 'OK'
    $detail = ''
    try {
        # 途中までの出力を成功と扱わないよう、結果を確定させてから書き出す。
        $result = & $Action
        if ($null -eq $result) { Write-Report '該当するデータはありません。' }
        else { Write-Report ($result | Out-String -Width 4096) }
    }
    catch {
        $state = 'FAILED'
        $detail = $_.Exception.Message
        Write-Report ('取得失敗（権限不足・非対応・タイムアウト等）: {0}' -f $detail)
    }
    finally {
        $timer.Stop()
        $statuses.Add([pscustomobject]@{
            Section = $Name; Status = $state; Seconds = [math]::Round($timer.Elapsed.TotalSeconds, 2); Detail = $detail
        })
        # 後続処理が中断されても、完了済み項目の記録を残す。
        Write-CsvFile -Path $statusPath -Rows $statuses.ToArray()
    }
}

function Get-CimData {
    param([string]$ClassName, [string]$Filter)
    $parameters = @{ ClassName = $ClassName; OperationTimeoutSec = 30; ErrorAction = 'Stop' }
    if ($Filter) { $parameters.Filter = $Filter }
    Get-CimInstance @parameters
}

function Invoke-NativeCommand {
    param([string]$FileName, [string]$Arguments = '')
    $command = Get-Command -Name $FileName -CommandType Application -ErrorAction Stop | Select-Object -First 1
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $command.Source
    $info.Arguments = $Arguments
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    # ウィンドウなしで実行する Windows 標準コマンドは OEM コードページを使う。
    # PowerShell 7 のコンソールは UTF-8 の場合があるため、その値は流用しない。
    $nativeEncoding = [System.Text.Encoding]::GetEncoding([System.Globalization.CultureInfo]::CurrentCulture.TextInfo.OEMCodePage)
    $info.StandardOutputEncoding = $nativeEncoding
    $info.StandardErrorEncoding = $nativeEncoding
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    $didStart = $false
    try {
        $didStart = $process.Start()
        # stdout / stderr を同時に読み、バッファ満杯による停止を防ぐ。
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($CommandTimeoutSeconds * 1000)) {
            throw ('{0}: {1} 秒でタイムアウトしました。' -f $FileName, $CommandTimeoutSeconds)
        }
        $outputText = $stdout.GetAwaiter().GetResult()
        $errorText = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            throw ("{0}: 終了コード {1}`r`n{2}`r`n{3}" -f $FileName, $process.ExitCode, $outputText, $errorText)
        }
        $outputText
        if ($errorText) { $errorText }
    }
    finally {
        if ($didStart -and -not $process.HasExited) { $process.Kill() }
        $process.Dispose()
    }
}

function Get-DxDiagReport {
    $temporaryPath = Join-Path $folderPath 'dxdiag.tmp.txt'
    $process = $null
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $dxdiag = Get-Command dxdiag.exe -CommandType Application -ErrorAction Stop
        $process = Start-Process -FilePath $dxdiag.Source -ArgumentList ('/whql:off /t "{0}"' -f $temporaryPath) -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit($CommandTimeoutSeconds * 1000)) {
            throw ('dxdiag: {0} 秒でタイムアウトしました。' -f $CommandTimeoutSeconds)
        }
        if ($process.ExitCode -ne 0) { throw ('dxdiag: 終了コード {0}' -f $process.ExitCode) }
        # ファイルの存在だけでなく、プロセスの終了と非空の出力を確認する。
        while (-not (Test-Path -LiteralPath $temporaryPath) -or (Get-Item -LiteralPath $temporaryPath).Length -eq 0) {
            if ($timer.Elapsed.TotalSeconds -ge $CommandTimeoutSeconds) { throw 'dxdiag の出力ファイルを取得できませんでした。' }
            Start-Sleep -Milliseconds 250
        }
        $ansi = [System.Text.Encoding]::GetEncoding([System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ANSICodePage)
        # BOM があれば UTF-16 / UTF-8 を自動判定し、なければ ANSI として読む。
        $reader = New-Object System.IO.StreamReader($temporaryPath, $ansi, $true)
        try { $reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    finally {
        if ($null -ne $process) {
            if (-not $process.HasExited) { $process.Kill() }
            $process.Dispose()
        }
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
}

function Get-RecentEvents {
    param([string]$LogName)
    try {
        $events = @(Get-WinEvent -FilterHashtable @{
            LogName = $LogName; StartTime = $startedAt.AddDays(-$EventLogDays); EndTime = $startedAt; Level = 1, 2, 3
        } -MaxEvents ($MaxEventsPerLog + 1) -ErrorAction Stop)
    }
    catch {
        if ($_.FullyQualifiedErrorId -like 'NoMatchingEventsFound*') { return '指定期間に重大・エラー・警告イベントはありません。' }
        throw
    }
    if ($events.Count -gt $MaxEventsPerLog) {
        $findings.Add(('{0}ログ: 上限を超えたため最新 {1} 件のみ記録しました。' -f $LogName, $MaxEventsPerLog))
    }
    $events | Select-Object -First $MaxEventsPerLog | Select-Object TimeCreated, Id, LevelDisplayName, ProviderName, RecordId, Message | Format-List
}

Write-Host '診断情報を収集しております。このままお待ちください。'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
try {
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
finally { $identity.Dispose() }
Write-Report ('収集開始: {0:o}' -f $startedAt)
Write-Report ('コンピューター: {0} / 実行ユーザー: {1}\{2}' -f $env:COMPUTERNAME, $env:USERDOMAIN, $env:USERNAME)
Write-Report ('PowerShell: {0} / 64bitプロセス: {1} / 管理者: {2}' -f $PSVersionTable.PSVersion, [Environment]::Is64BitProcess, $isAdmin)
Write-Report ('イベント期間: {0:o} ～ {1:o} / ログごとの最大件数: {2}' -f $startedAt.AddDays(-$EventLogDays), $startedAt, $MaxEventsPerLog)
Write-Report '収集値は取得時点の状態です。FAILED は異常の確定ではなく、未確認の項目です。'
if (-not $isAdmin) { $findings.Add('一般ユーザーで実行しています。権限が必要な情報や他ユーザーのプロセス詳細は欠落する場合があります。') }

Invoke-Section 'Windowsシステムの情報' { Invoke-NativeCommand 'systeminfo.exe' }
Invoke-Section 'OS・稼働時間・メモリ使用状況' {
    $os = Get-CimData 'Win32_OperatingSystem'
    $os | Select-Object Caption, Version, BuildNumber, OSArchitecture, InstallDate, LastBootUpTime,
        @{n='UptimeDays'; e={[math]::Round(($startedAt - $_.LastBootUpTime).TotalDays, 2)}},
        @{n='TotalMemoryGB'; e={[math]::Round($_.TotalVisibleMemorySize / 1MB, 2)}},
        @{n='FreeMemoryGB'; e={[math]::Round($_.FreePhysicalMemory / 1MB, 2)}} | Format-List
}
Invoke-Section 'ハードウェア・BIOS' {
    Get-CimData 'Win32_ComputerSystem' | Select-Object Manufacturer, Model, Domain, PartOfDomain, TotalPhysicalMemory | Format-List
    Get-CimData 'Win32_BIOS' | Select-Object Manufacturer, SMBIOSBIOSVersion, ReleaseDate, SerialNumber | Format-List
    Get-CimData 'Win32_Processor' | Select-Object Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed | Format-List
}
Invoke-Section 'ドライブ情報（PowerShell）' {
    Get-PSDrive -PSProvider FileSystem | Select-Object Name, Used, Free, Root, DisplayRoot | Format-List
}
Invoke-Section '固定ディスクの空き容量' {
    $disks = @(Get-CimData 'Win32_LogicalDisk' 'DriveType = 3')
    foreach ($disk in $disks) {
        if ($disk.Size -gt 0 -and ($disk.FreeSpace / $disk.Size * 100) -lt $LowDiskSpacePercent) {
            $findings.Add(('{0} の空き容量が {1}% 未満です（{2:N1}%）。' -f $disk.DeviceID, $LowDiskSpacePercent, ($disk.FreeSpace / $disk.Size * 100)))
        }
    }
    $disks | Select-Object DeviceID, VolumeName, FileSystem,
        @{n='SizeGB'; e={[math]::Round($_.Size / 1GB, 2)}},
        @{n='FreeGB'; e={[math]::Round($_.FreeSpace / 1GB, 2)}},
        @{n='FreePercent'; e={if ($_.Size -gt 0) {[math]::Round($_.FreeSpace / $_.Size * 100, 2)}}}, DirtyBitSet | Format-List
}
Invoke-Section '物理ディスクの状態' {
    $disks = @(Get-PhysicalDisk -ErrorAction Stop)
    foreach ($disk in $disks) {
        if ([string]$disk.HealthStatus -ne 'Healthy') { $findings.Add(('ディスク {0}: HealthStatus={1}。詳細確認が必要です。' -f $disk.FriendlyName, $disk.HealthStatus)) }
    }
    $disks | Select-Object FriendlyName, MediaType, BusType, Size, HealthStatus, OperationalStatus | Format-List
}
Invoke-Section 'DirectX診断ツールの情報' { Get-DxDiagReport }
Invoke-Section 'ネットワーク情報' { Invoke-NativeCommand 'ipconfig.exe' '/all' }
Invoke-Section 'ルーティングテーブルの情報' { Invoke-NativeCommand 'route.exe' 'print' }
Invoke-Section 'ネットワークアダプター・通信エラー' {
    Get-NetAdapter -ErrorAction Stop | Select-Object Name, InterfaceDescription, Status, LinkSpeed, MacAddress, DriverInformation | Format-List
    Get-NetAdapterStatistics -ErrorAction Stop | Select-Object Name, ReceivedBytes, SentBytes, ReceivedPacketErrors, OutboundPacketErrors, ReceivedDiscardedPackets, OutboundDiscardedPackets | Format-List
}
Invoke-Section 'TCP/UDP接続・待受ポート（PID付き）' { Invoke-NativeCommand 'netstat.exe' '-ano' }
Invoke-Section 'WinHTTPプロキシ' { Invoke-NativeCommand 'netsh.exe' 'winhttp show proxy' }
Invoke-Section '実行ユーザーのプロキシ設定' {
    Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' | Select-Object ProxyEnable, ProxyServer, ProxyOverride, AutoConfigURL | Format-List
}
Invoke-Section '時刻同期の状態' { Invoke-NativeCommand 'w32tm.exe' '/query /status' }
Invoke-Section '現在動作しているタスクの情報' { Invoke-NativeCommand 'tasklist.exe' '/V /FO CSV' }
Invoke-Section 'プロセス詳細（001.bat の WMIC PROCESS GET 相当）' {
    $processes = @(Get-CimData 'Win32_Process')
    if ($processes.Count -eq 0) { throw 'プロセス一覧が空です。' }
    # 同じ WMI クラスの全データプロパティを採用（CIM の管理用プロパティは除外）。
    $properties = @($processes[0].CimInstanceProperties | ForEach-Object { $_.Name })
    $rows = @($processes | Select-Object -Property $properties)
    Write-CsvFile -Path $processPath -Rows $rows
    '保存先: infomation.txt_processlist.txt（CSV形式） / 件数: {0}' -f $rows.Count
    'CommandLine / ExecutablePath が空の場合は、保護プロセス・権限不足・取得中の終了などが考えられます。'
}
Invoke-Section 'サービスの状態・起動方式' {
    Get-CimData 'Win32_Service' | Sort-Object Name | Select-Object Name, DisplayName, State, StartMode, StartName, ExitCode, PathName | Format-List
    '自動起動サービスの停止はトリガー起動などでも発生するため、用途と照合して判断してください。'
}
Invoke-Section '自動起動プログラム' {
    Get-CimData 'Win32_StartupCommand' | Select-Object Name, Command, Location, User | Format-List
}
Invoke-Section 'スケジュールタスクの最終実行結果' {
    Get-ScheduledTask -ErrorAction Stop | ForEach-Object {
        $task = $_
        $taskInfo = $task | Get-ScheduledTaskInfo -ErrorAction Stop
        [pscustomobject]@{
            TaskPath = $task.TaskPath; TaskName = $task.TaskName; State = $task.State
            LastRunTime = $taskInfo.LastRunTime; LastTaskResult = $taskInfo.LastTaskResult
            NextRunTime = $taskInfo.NextRunTime; NumberOfMissedRuns = $taskInfo.NumberOfMissedRuns
        }
    } | Format-List
}
Invoke-Section 'インストール済みアプリ（マシン全体・実行ユーザー）' {
    # Win32_Product は MSI 修復を誘発するため使用しない。32/64bit レジストリを明示して読む。
    $apps = @(foreach ($hive in @([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryHive]::CurrentUser)) {
        $views = @([Microsoft.Win32.RegistryView]::Registry32)
        if ([Environment]::Is64BitOperatingSystem) { $views += [Microsoft.Win32.RegistryView]::Registry64 }
        foreach ($view in $views) {
            $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($hive, $view)
            try {
                $uninstall = $base.OpenSubKey('SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')
                if ($null -eq $uninstall) { continue }
                try {
                    foreach ($keyName in $uninstall.GetSubKeyNames()) {
                        $key = $uninstall.OpenSubKey($keyName)
                        if ($null -eq $key) { continue }
                        try {
                            if ($key.GetValue('DisplayName')) {
                                [pscustomobject]@{
                                    Name = $key.GetValue('DisplayName'); Version = $key.GetValue('DisplayVersion')
                                    Publisher = $key.GetValue('Publisher'); InstallDate = $key.GetValue('InstallDate')
                                    Scope = [string]$hive; RegistryView = [string]$view
                                }
                            }
                        } finally { $key.Dispose() }
                    }
                } finally { $uninstall.Dispose() }
            } finally { $base.Dispose() }
        }
    })
    $apps | Sort-Object Name, Version, Scope, RegistryView -Unique | Format-List
    '一覧はアンインストール用レジストリ登録情報です。他ユーザー専用・Store・ポータブルアプリ等は網羅しません。'
}
Invoke-Section 'インストール済みWindows更新（QFE）' {
    Get-CimData 'Win32_QuickFixEngineering' | Select-Object HotFixID, Description, InstalledOn, InstalledBy | Format-List
    'QFEで確認できる適用済み更新です。すべての更新履歴や未適用更新の有無・最新性を示すものではありません。'
}
Invoke-Section '再起動待ちの確認' {
    $session = Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager'
    $activeName = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName').ComputerName
    $configuredName = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName').ComputerName
    $pending = [ordered]@{
        ComponentBasedServicing = Test-Path -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'
        WindowsUpdate = Test-Path -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'
        PendingFileRenameOperations = [bool]$session.PendingFileRenameOperations
        ComputerRename = $activeName -ne $configuredName
    }
    if ($pending.Values -contains $true) { $findings.Add('再起動待ちを示す情報があります。再起動待ちの確認セクションを参照してください。') }
    [pscustomobject]$pending | Format-List
    '代表的なレジストリ情報による参考判定です。すべての再起動要求を網羅するものではありません。'
}
Invoke-Section 'デバイス異常' {
    $devices = @(Get-CimData 'Win32_PnPEntity' 'ConfigManagerErrorCode <> 0')
    if ($devices.Count -gt 0) { $findings.Add(('エラーコードがあるデバイス: {0} 件。無効化されたデバイスも含みます。' -f $devices.Count)) }
    $devices | Select-Object Name, PNPDeviceID, Status, ConfigManagerErrorCode | Format-List
}
Invoke-Section 'ドライバーのバージョン・署名' {
    Get-CimData 'Win32_PnPSignedDriver' | Sort-Object DeviceName | Select-Object DeviceName, DriverVersion, DriverDate, DriverProviderName, IsSigned, InfName | Format-List
}
Invoke-Section 'Microsoft Defenderの状態' {
    Get-MpComputerStatus -ErrorAction Stop | Select-Object AMServiceEnabled, AntivirusEnabled, AntispywareEnabled,
        RealTimeProtectionEnabled, AntivirusSignatureVersion, AntivirusSignatureLastUpdated, AntivirusSignatureAge,
        QuickScanEndTime, FullScanEndTime, RebootRequired | Format-List
    '別製品のウイルス対策を使用している場合、Defenderが無効でも異常とは限りません。'
}
Invoke-Section '登録済みウイルス対策製品' {
    Get-CimInstance -Namespace 'root/SecurityCenter2' -ClassName AntiVirusProduct -OperationTimeoutSec 30 -ErrorAction Stop |
        Select-Object displayName, productState, timestamp | Format-List
}
Invoke-Section 'Windowsファイアウォールの状態' {
    Get-NetFirewallProfile -PolicyStore ActiveStore -ErrorAction Stop | Select-Object Name, Enabled, DefaultInboundAction, DefaultOutboundAction | Format-List
}
Invoke-Section 'BitLockerの状態' {
    Get-BitLockerVolume -ErrorAction Stop | Select-Object MountPoint, VolumeType, VolumeStatus, ProtectionStatus, EncryptionPercentage, EncryptionMethod, LockStatus | Format-List
}
Invoke-Section 'Systemログの重大・エラー・警告' { Get-RecentEvents 'System' }
Invoke-Section 'Applicationログの重大・エラー・警告' { Get-RecentEvents 'Application' }

$failed = @($statuses | Where-Object { $_.Status -eq 'FAILED' })
Write-Report "`r`n===== 運用・保守の確認ポイント ====="
if ($findings.Count -gt 0) { $findings | ForEach-Object { Write-Report ('- ' + $_) } }
else { Write-Report '自動判定項目では注意点は検出されませんでした。収集結果全体の正常性を保証するものではありません。' }
Write-Report ('取得成功: {0} / 取得失敗・未確認: {1}' -f ($statuses.Count - $failed.Count), $failed.Count)
foreach ($failure in $failed) { Write-Report ('- 未確認: {0} / {1}' -f $failure.Section, $failure.Detail) }
Write-Report ('収集終了: {0:o} / 所要時間: {1:N1} 秒' -f (Get-Date), ((Get-Date) - $startedAt).TotalSeconds)
Write-Report '設定変更、更新のインストール、ディスク修復、サービス操作、再起動は行っていません。'

Write-Host ''
Write-Host ('診断情報の収集が完了しました。取得失敗・未確認: {0} 件' -f $failed.Count)
Write-Host ('出力先: {0}' -f $folderPath)
Write-Host 'このフォルダーをサポートへ送付してください。collection-status.csv で取得結果を確認できます。'
Write-Host 'ユーザー名、IPアドレス、プロセスのコマンドライン等を含みます。送付前に内容を確認してください。'
if (-not $NoOpenFolder) {
    try { Invoke-Item -LiteralPath $folderPath } catch { Write-Warning ('出力フォルダーを開けませんでした: {0}' -f $_.Exception.Message) }
}
if (-not $NoPause -and [Environment]::UserInteractive -and -not [Console]::IsInputRedirected) {
    try {
        Write-Host '何かキーを押すと終了します。'
        $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') | Out-Null
    } catch { Write-Verbose '対話入力が利用できないため、そのまま終了します。' }
}
