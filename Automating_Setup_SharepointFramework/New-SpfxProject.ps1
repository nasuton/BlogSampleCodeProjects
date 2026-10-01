<#
.SYNOPSIS
  Dev Containers 対応の SPFx プロジェクトを新規作成する。

.DESCRIPTION
  1. 新しいプロジェクトフォルダーを作成し、template/ 配下の Docker / Dev Containers 設定を配置
  2. Docker イメージをビルド
  3. コンテナ内で yo @microsoft/sharepoint を実行して SPFx ソリューションを生成
  4. 生成物をプロジェクト直下へ展開し、.gitignore / .env を整える

  ホストに Node.js は不要。Docker Desktop が未起動なら自動で起動して待機する。
  実行内容はすべて Doc\logs\New-SpfxProject_<日時>.log に記録される (-LogPath で変更可)。

.EXAMPLE
  # 引数なしで実行すると、種類 / フレームワーク / 名前などを対話形式で選択できる
  .\New-SpfxProject.ps1

.EXAMPLE
  .\New-SpfxProject.ps1 -Path D:\work\SPFx\MyProject -SolutionName my-project -ComponentName MyWebPart -TenantDomain contoso.sharepoint.com

.EXAMPLE
  # 拡張機能 (ApplicationCustomizer) を作る場合
  .\New-SpfxProject.ps1 -Path D:\work\SPFx\MyExt -SolutionName my-ext -ComponentType extension -ExtensionType ApplicationCustomizer -ComponentName MyCustomizer

.EXAMPLE
  # 対話なしで既定値 (webpart / react) を使う
  .\New-SpfxProject.ps1 -Path D:\work\SPFx\MyProject -SolutionName my-project -ComponentName MyWebPart -NonInteractive
#>
[CmdletBinding()]
param(
  [string] $Path,
  [string] $SolutionName,
  [string] $ComponentName,
  [ValidateSet('webpart', 'extension', 'library', 'adaptiveCardExtension')]
  [string] $ComponentType,
  [ValidateSet('react', 'none', 'minimal')]
  [string] $Framework,
  [ValidateSet('ApplicationCustomizer', 'FieldCustomizer', 'ListViewCommandSet', 'FormCustomizer', 'SearchQueryModifier')]
  [string] $ExtensionType,
  [string] $TenantDomain,
  [switch] $SkipInstall,
  [switch] $SkipBuild,
  # 実行ログの出力先。省略時は Doc\logs\New-SpfxProject_yyyyMMdd_HHmmss.log
  [string] $LogPath,
  # 対話プロンプトを出さず、未指定の項目は既定値を使う (CI などバッチ用途)
  [switch] $NonInteractive
)

$ErrorActionPreference = 'Stop'
$templateDir = Join-Path $PSScriptRoot 'template'

# ---- 対話入力ヘルパー ----------------------------------------------------
function Read-Choice {
  param([string] $Title, [hashtable[]] $Options, [string] $Default)
  if ($NonInteractive) { return $Default }
  Write-Host ''
  Write-Host $Title -ForegroundColor Cyan
  for ($i = 0; $i -lt $Options.Count; $i++) {
    $mark = if ($Options[$i].Value -eq $Default) { '*' } else { ' ' }
    Write-Host ("  {0}{1}) {2,-24} {3}" -f $mark, ($i + 1), $Options[$i].Value, $Options[$i].Label)
  }
  while ($true) {
    $in = Read-Host "番号を入力 (Enter で * の既定値)"
    if ([string]::IsNullOrWhiteSpace($in)) { return $Default }
    if ($in -match '^\d+$' -and [int]$in -ge 1 -and [int]$in -le $Options.Count) { return $Options[[int]$in - 1].Value }
    $hit = $Options | Where-Object { $_.Value -eq $in }
    if ($hit) { return $hit.Value }
    Write-Host '  無効な入力です。' -ForegroundColor Yellow
  }
}
function Read-Text {
  param([string] $Prompt, [string] $Default, [string] $Pattern, [string] $Hint)
  if ($NonInteractive) { if ($Default) { return $Default } else { throw "$Prompt が指定されていません (-NonInteractive)。" } }
  while ($true) {
    $label = if ($Default) { "$Prompt [$Default]" } else { $Prompt }
    $in = Read-Host $label
    if ([string]::IsNullOrWhiteSpace($in)) { $in = $Default }
    if ([string]::IsNullOrWhiteSpace($in)) { Write-Host '  必須です。' -ForegroundColor Yellow; continue }
    if ($Pattern -and $in -notmatch $Pattern) { Write-Host "  $Hint" -ForegroundColor Yellow; continue }
    return $in
  }
}

# ---- 未指定のパラメーターを対話で補完 ------------------------------------
if (-not $ComponentType) {
  $ComponentType = Read-Choice -Title 'コンポーネントの種類を選択してください' -Default 'webpart' -Options @(
    @{ Value = 'webpart';               Label = 'Web パーツ (ページに配置する部品)' },
    @{ Value = 'extension';             Label = '拡張機能 (ヘッダー/フッター・列・コマンド・フォーム等)' },
    @{ Value = 'library';               Label = 'ライブラリ (他コンポーネントから参照する共通コード)' },
    @{ Value = 'adaptiveCardExtension'; Label = 'Adaptive Card 拡張機能 (Viva Connections ダッシュボード)' }
  )
}
if ($ComponentType -eq 'extension' -and -not $ExtensionType) {
  $ExtensionType = Read-Choice -Title '拡張機能の種類を選択してください' -Default 'ApplicationCustomizer' -Options @(
    @{ Value = 'ApplicationCustomizer'; Label = 'ページ全体にスクリプト/ヘッダー/フッターを追加' },
    @{ Value = 'FieldCustomizer';       Label = 'リスト列の表示をカスタマイズ' },
    @{ Value = 'ListViewCommandSet';    Label = 'リストのコマンドバーにボタンを追加' },
    @{ Value = 'FormCustomizer';        Label = 'リストフォームを置き換え' },
    @{ Value = 'SearchQueryModifier';   Label = '検索クエリを書き換え' }
  )
}
# フレームワークを選べるのは webpart と一部の extension のみ
$frameworkApplicable = ($ComponentType -eq 'webpart') -or
  ($ComponentType -eq 'extension' -and $ExtensionType -in 'ApplicationCustomizer', 'FieldCustomizer', 'FormCustomizer')
if ($frameworkApplicable -and -not $Framework) {
  $fwOptions = @(
    @{ Value = 'react';   Label = 'React (推奨。Fluent UI 利用可)' },
    @{ Value = 'none';    Label = 'フレームワークなし (素の TypeScript / DOM 操作)' }
  )
  if ($ComponentType -eq 'webpart') { $fwOptions += @{ Value = 'minimal'; Label = '最小構成 (サンプルコードなし)' } }
  $Framework = Read-Choice -Title 'フレームワークを選択してください' -Default 'react' -Options $fwOptions
}
$useFramework = if ($frameworkApplicable) { if ($Framework) { $Framework } else { 'react' } } else { $null }

if (-not $Path)          { $Path          = Read-Text -Prompt '作成先フォルダー (例: D:\work\SPFx\MyProject)' }
if (-not $SolutionName)  {
  $suggest = (Split-Path $Path -Leaf).ToLower() -replace '[^a-z0-9]+', '-' -replace '^-|-$', ''
  $SolutionName = Read-Text -Prompt 'ソリューション名 (小文字ケバブケース)' -Default $suggest -Pattern '^[a-z0-9][a-z0-9-]*$' -Hint '小文字英数字とハイフンのみ使用できます。'
}
if (-not $ComponentName) {
  $suggest = ((($SolutionName -split '-') | ForEach-Object { if ($_) { $_.Substring(0,1).ToUpper() + $_.Substring(1) } }) -join '')
  $ComponentName = Read-Text -Prompt 'コンポーネント名 (PascalCase)' -Default $suggest -Pattern '^[A-Za-z][A-Za-z0-9]*$' -Hint '英字で始まる英数字のみ使用できます。'
}
if (-not $PSBoundParameters.ContainsKey('TenantDomain') -and -not $NonInteractive) {
  $TenantDomain = Read-Host 'テナントドメイン (例: contoso.sharepoint.com / 空欄で省略)'
}

# ---- ログ (Start-Transcript で画面出力をすべてファイルにも記録) -------------
if (-not $LogPath) {
  $LogPath = Join-Path $PSScriptRoot ('logs\New-SpfxProject_{0:yyyyMMdd_HHmmss}.log' -f (Get-Date))
}
New-Item -ItemType Directory -Force -Path (Split-Path $LogPath) | Out-Null
Start-Transcript -Path $LogPath -Force | Out-Null
Write-Host "ログ: $LogPath" -ForegroundColor DarkGray
Write-Host ("引数: " + ($PSBoundParameters.GetEnumerator() | ForEach-Object { "-$($_.Key) $($_.Value)" }) -join ' ') -ForegroundColor DarkGray
Write-Host ''
Write-Host '作成内容:' -ForegroundColor Cyan
Write-Host "  作成先         : $Path"
Write-Host "  ソリューション名 : $SolutionName"
Write-Host "  コンポーネント名 : $ComponentName"
Write-Host "  種類           : $ComponentType$(if ($ComponentType -eq 'extension') { " / $ExtensionType" })"
Write-Host "  フレームワーク   : $(if ($useFramework) { $useFramework } else { '(該当なし)' })"
Write-Host "  テナント        : $(if ($TenantDomain) { $TenantDomain } else { '(未設定)' })"
if (-not $NonInteractive) {
  $ok = Read-Host 'この内容で作成しますか? [Y/n]'
  if ($ok -and $ok -notmatch '^[Yy]') { Write-Host '中止しました。' -ForegroundColor Yellow; Stop-Transcript | Out-Null; exit 2 }
}

# ネイティブコマンド (docker 等) を stderr 込みで実行し、出力を画面とログの両方へ流す
function Invoke-Native {
  param([Parameter(Mandatory)] [string[]] $CommandLine)
  $ErrorActionPreference = 'Continue'   # stderr 出力を例外に昇格させない (Windows PowerShell 5.1 対策)
  $quoted = $CommandLine | ForEach-Object { if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } }
  Write-Host ('> ' + ($quoted -join ' ')) -ForegroundColor DarkGray
  & cmd /c (($quoted -join ' ') + ' 2>&1') | Out-Host
  return $LASTEXITCODE
}

$script:failed = $false
try {

if (-not (Test-Path $templateDir)) { throw "template フォルダーが見つかりません: $templateDir" }

# ---- 0. Docker Desktop の起動確認 (未起動なら起動して待つ) ------------------
function Test-DockerReady { $null -ne (& cmd /c 'docker info 2>nul' | Select-String 'Server Version') }
if (-not (Test-DockerReady)) {
  $dd = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'
  if (-not (Test-Path $dd)) { throw 'Docker Desktop が起動しておらず、実行ファイルも見つかりません。手動で起動してください。' }
  Write-Host '[0/5] Docker Desktop を起動しています...' -ForegroundColor Cyan
  Start-Process $dd
  $deadline = (Get-Date).AddMinutes(3)
  while (-not (Test-DockerReady)) {
    if ((Get-Date) -gt $deadline) { throw 'Docker Desktop の起動を 3 分待ちましたが応答がありません。' }
    Start-Sleep -Seconds 5
  }
  Write-Host '      Docker Desktop が起動しました。' -ForegroundColor Green
}

# ---- 1. フォルダー作成とテンプレート配置 -------------------------------
$Path = [IO.Path]::GetFullPath($Path)
if ((Test-Path $Path) -and (Get-ChildItem $Path -Force | Select-Object -First 1)) {
  throw "出力先が空ではありません: $Path"
}
New-Item -ItemType Directory -Force -Path $Path | Out-Null
Write-Host "[1/5] テンプレートを配置: $Path" -ForegroundColor Cyan
Copy-Item -Path (Join-Path $templateDir '*') -Destination $Path -Recurse -Force

# シェルスクリプトは LF 必須 (CRLF だと exec に失敗する)
Get-ChildItem (Join-Path $Path 'docker\*.sh') | ForEach-Object {
  $c = (Get-Content $_.FullName -Raw) -replace "`r`n", "`n"
  [IO.File]::WriteAllText($_.FullName, $c, (New-Object Text.UTF8Encoding $false))
}

if ($TenantDomain) {
  "SPFX_SERVE_TENANT_DOMAIN=$TenantDomain`n" | Set-Content -NoNewline (Join-Path $Path '.env')
}

Push-Location $Path
try {
  # ---- 2. イメージビルド ----------------------------------------------
  if (-not $SkipBuild) {
    Write-Host '[2/5] Docker イメージをビルド' -ForegroundColor Cyan
    if ((Invoke-Native 'docker','compose','build') -ne 0) { throw 'docker compose build に失敗しました。' }
  }

  # ---- 3. yo でソリューション生成 -------------------------------------
  Write-Host '[3/5] SPFx ソリューションを生成 (yo @microsoft/sharepoint)' -ForegroundColor Cyan
  $yoArgs = @(
    '--solution-name', $SolutionName,
    '--component-type', $ComponentType,
    '--component-name', $ComponentName,
    '--skip-install', '--no-insight'
  )
  switch ($ComponentType) {
    'webpart'   { $yoArgs += @('--framework', $useFramework) }
    'extension' { $yoArgs += @('--extension-type', $ExtensionType); if ($useFramework) { $yoArgs += @('--framework', $useFramework) } }
  }
  if ((Invoke-Native (@('docker','compose','run','--rm','spfx','yo','@microsoft/sharepoint') + $yoArgs)) -ne 0) { throw 'yo @microsoft/sharepoint に失敗しました。' }

  # yo は <SolutionName>/ サブフォルダーに生成するのでルートへ展開
  $sub = Join-Path $Path $SolutionName
  if (Test-Path $sub) {
    Write-Host '[4/5] 生成物をプロジェクト直下へ展開' -ForegroundColor Cyan
    Get-ChildItem $sub -Force | ForEach-Object {
      $dest = Join-Path $Path $_.Name
      if ($_.Name -eq '.vscode') {
        # テンプレートの tasks.json / settings.json / launch.json を優先し、yo 生成物は不足分だけ取り込む
        Get-ChildItem $_.FullName | Where-Object { -not (Test-Path (Join-Path $dest $_.Name)) } | Move-Item -Destination $dest
        Remove-Item $_.FullName -Recurse -Force
      } elseif ($_.Name -eq 'README.md') {
        Move-Item $_.FullName (Join-Path $Path 'README.spfx-template.md') -Force
      } else {
        Move-Item $_.FullName $dest -Force
      }
    }
    Remove-Item $sub -Recurse -Force
  }

  # コンテナ内 root で作成された空の node_modules は不要
  $nm = Join-Path $Path 'node_modules'
  if ((Test-Path $nm) -and -not (Get-ChildItem $nm -Force | Select-Object -First 1)) { Remove-Item $nm -Force }

  # .gitignore に Docker / ローカル用の除外を追加
  $gi = Join-Path $Path '.gitignore'
  if (-not (Test-Path $gi) -or -not (Select-String -Path $gi -Pattern '^certs/' -Quiet)) {
    Add-Content $gi "`n# Docker / local`n.env`ncerts/`n"
  }

  # ---- 5. npm install (任意) ------------------------------------------
  if (-not $SkipInstall) {
    Write-Host '[5/5] npm install (コンテナ内)' -ForegroundColor Cyan
    if ((Invoke-Native 'docker','compose','run','--rm','spfx','npm','install') -ne 0) { throw 'npm install に失敗しました。' }
  } else {
    Write-Host '[5/5] npm install をスキップ (Dev Container 起動時の postCreateCommand で実行されます)' -ForegroundColor Yellow
  }
}
finally {
  Pop-Location
}

Write-Host ''
Write-Host '完了しました。次の手順:' -ForegroundColor Green
Write-Host "  1. code `"$Path`""
Write-Host '  2. F1 -> Dev Containers: Reopen in Container'
Write-Host '  3. 初回のみ certs\rushstack-serve.cer をダブルクリックして「信頼されたルート証明機関」へインポート'
Write-Host '  4. Ctrl+Shift+B (SPFx: serve) -> 表示された Workbench URL をブラウザで開く'

}
catch {
  $script:failed = $true
  Write-Host ''
  Write-Host '==================== エラー ====================' -ForegroundColor Red
  Write-Host $_.Exception.Message -ForegroundColor Red
  if ($_.InvocationInfo) { Write-Host ("場所: " + $_.InvocationInfo.PositionMessage) -ForegroundColor DarkGray }
  Write-Host ($_.ScriptStackTrace) -ForegroundColor DarkGray
  if ($_.Exception.InnerException) { Write-Host ("内部例外: " + $_.Exception.InnerException.Message) -ForegroundColor DarkGray }
  Write-Host '================================================' -ForegroundColor Red
  Write-Host "詳細はログを確認してください: $LogPath" -ForegroundColor Yellow
}
finally {
  Stop-Transcript | Out-Null
}

if ($script:failed) { exit 1 }
exit 0
