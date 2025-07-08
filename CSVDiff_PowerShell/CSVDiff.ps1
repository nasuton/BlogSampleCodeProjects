param(
    [Parameter(Mandatory=$true)]
    [string]$File1,
    
    [Parameter(Mandatory=$true)]
    [string]$File2,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputFile = "CSVDiff_Output.csv",
    
    [Parameter(Mandatory=$false)]
    [string]$KeyColumn = $null
)

function Compare-CSVFiles {
    param(
        [string]$FirstFile,
        [string]$SecondFile,
        [string]$OutputPath,
        [string]$Key
    )
    
    try {
        # CSVファイルの存在確認
        if (-not (Test-Path $FirstFile)) {
            throw "ファイルが見つかりません: $FirstFile"
        }
        if (-not (Test-Path $SecondFile)) {
            throw "ファイルが見つかりません: $SecondFile"
        }
        
        Write-Host "CSVファイルを読み込み中..." -ForegroundColor Green
        
        # CSVファイルを読み込み
        $csv1 = Import-Csv $FirstFile -Encoding UTF8
        $csv2 = Import-Csv $SecondFile -Encoding UTF8
        
        Write-Host "ファイル1: $($csv1.Count) 行" -ForegroundColor Cyan
        Write-Host "ファイル2: $($csv2.Count) 行" -ForegroundColor Cyan
        
        # 差分結果を格納する配列
        $diffResults = @()
        
        if ($Key -or $Key -ne '') {
            # キー列が指定されている場合、キーベースで比較
            Write-Host "キー列 '$Key' を使用して比較中..." -ForegroundColor Yellow
            
            # ハッシュテーブルを作成
            $hash1 = @{}
            $hash2 = @{}
            
            foreach ($row in $csv1) {
                $hash1[$row.$Key] = $row
            }
            
            foreach ($row in $csv2) {
                $hash2[$row.$Key] = $row
            }
            
            # ファイル1にのみ存在するレコード
            foreach ($hash1key1 in $hash1.Keys) {
                if (-not $hash2.ContainsKey($hash1key1)) {
                    $diffRow = $hash1[$hash1key1] | Select-Object *, @{Name='DiffType';Expression={'OnlyInFile1'}}, @{Name='ComparedFile';Expression={$FirstFile}}
                    $diffResults += $diffRow
                }
            }
            
            # ファイル2にのみ存在するレコード
            foreach ($hash2key1 in $hash2.Keys) {
                if (-not $hash1.ContainsKey($hash2key1)) {
                    $diffRow = $hash2[$hash2key1] | Select-Object *, @{Name='DiffType';Expression={'OnlyInFile2'}}, @{Name='ComparedFile';Expression={$SecondFile}}
                    $diffResults += $diffRow
                }
            }
            
            # 両方に存在するが内容が異なるレコード
            foreach ($key in $hash1.Keys) {
                if ($hash2.ContainsKey($key)) {
                    $row1 = $hash1[$key]
                    $row2 = $hash2[$key]
                    
                    $isDifferent = $false
                    $row1.PSObject.Properties | ForEach-Object {
                        if ($_.Name -ne $Key -and $row1.($_.Name) -ne $row2.($_.Name)) {
                            $isDifferent = $true
                        }
                    }
                    
                    if ($isDifferent) {
                        $diffRow1 = $row1 | Select-Object *, @{Name='DiffType';Expression={'Modified_File1'}}, @{Name='ComparedFile';Expression={$FirstFile}}
                        $diffRow2 = $row2 | Select-Object *, @{Name='DiffType';Expression={'Modified_File2'}}, @{Name='ComparedFile';Expression={$SecondFile}}
                        $diffResults += $diffRow1
                        $diffResults += $diffRow2
                    }
                }
            }
        }
        else {
            # キー列が指定されていない場合、行番号ベースで比較
            Write-Host "行番号ベースで比較中..." -ForegroundColor Yellow
            
            $maxRows = [Math]::Max($csv1.Count, $csv2.Count)
            
            for ($i = 0; $i -lt $maxRows; $i++) {
                $row1 = if ($i -lt $csv1.Count) { $csv1[$i] } else { $null }
                $row2 = if ($i -lt $csv2.Count) { $csv2[$i] } else { $null }
                
                if ($row1 -eq $null) {
                    # ファイル2にのみ存在
                    $diffRow = $row2 | Select-Object *, @{Name='RowNumber';Expression={$i+1}}, @{Name='DiffType';Expression={'OnlyInFile2'}}, @{Name='ComparedFile';Expression={$SecondFile}}
                    $diffResults += $diffRow
                }
                elseif ($row2 -eq $null) {
                    # ファイル1にのみ存在
                    $diffRow = $row1 | Select-Object *, @{Name='RowNumber';Expression={$i+1}}, @{Name='DiffType';Expression={'OnlyInFile1'}}, @{Name='ComparedFile';Expression={$FirstFile}}
                    $diffResults += $diffRow
                }
                else {
                    # 両方に存在、内容を比較
                    $isDifferent = $false
                    $row1.PSObject.Properties | ForEach-Object {
                        if ($row1.($_.Name) -ne $row2.($_.Name)) {
                            $isDifferent = $true
                        }
                    }
                    
                    if ($isDifferent) {
                        $diffRow1 = $row1 | Select-Object *, @{Name='RowNumber';Expression={$i+1}}, @{Name='DiffType';Expression={'Modified_File1'}}, @{Name='ComparedFile';Expression={$FirstFile}}
                        $diffRow2 = $row2 | Select-Object *, @{Name='RowNumber';Expression={$i+1}}, @{Name='DiffType';Expression={'Modified_File2'}}, @{Name='ComparedFile';Expression={$SecondFile}}
                        $diffResults += $diffRow1
                        $diffResults += $diffRow2
                    }
                }
            }
        }
        
        # 結果の出力
        if ($diffResults.Count -eq 0) {
            Write-Host "差分は見つかりませんでした。" -ForegroundColor Green
        }
        else {
            Write-Host "$($diffResults.Count) 件の差分が見つかりました。" -ForegroundColor Yellow
            
            # CSVファイルに出力
            $diffResults | Export-Csv -Path $OutputPath -Encoding UTF8 -NoTypeInformation
            Write-Host "差分結果を $OutputPath に出力しました。" -ForegroundColor Green
            
            # コンソールにサマリーを表示
            Write-Host "`n=== 差分サマリー ===" -ForegroundColor Cyan
            $diffResults | Group-Object DiffType | ForEach-Object {
                Write-Host "$($_.Name): $($_.Count) 件" -ForegroundColor White
            }
        }
        
        return $diffResults
    }
    catch {
        Write-Error "エラーが発生しました: $($_.Exception.Message)"
        return $null
    }
}

# メイン処理
Write-Host "=== CSV差分比較ツール ===" -ForegroundColor Cyan
Write-Host "ファイル1: $File1" -ForegroundColor White
Write-Host "ファイル2: $File2" -ForegroundColor White
Write-Host "出力ファイル: $OutputFile" -ForegroundColor White

if ($KeyColumn) {
    Write-Host "キー列: $KeyColumn" -ForegroundColor White
}

$result = Compare-CSVFiles -FirstFile $File1 -SecondFile $File2 -OutputPath $OutputFile -Key $KeyColumn

if ($result) {
    Write-Host "`n処理が完了しました。" -ForegroundColor Green
}
