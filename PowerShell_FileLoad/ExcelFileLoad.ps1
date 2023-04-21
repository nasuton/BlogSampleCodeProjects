#excelファイルを読み込む
function ReadExcelFile($_filePath){
    #ファイルの存在を確認する
    if(Test-Path -LiteralPath $_filePath){
        $excel = New-Object -ComObject Excel.Application
        #最大行数
        $maxRow = 100
        try{
            #excelを非表示で実行
            $excel.Visible = $false
            #警告ウィンドウを表示しない
            $excel.DisplayAlerts = $false
            #マクロを実行させない
            #$excel.EnableEvents = $false
            #$excel.AutomationSecurity = "msoAutomationSecurityForceDisable"
            #Excelを開く際にパスワードが必要な場合(必要ない場合は無視される)
            $openPass = ""
            #Excelを書き込む際にパスワードが必要な場合(必要ない場合は無視される)
            $writePass = ""
            #エクセルを開く
            $book =  $excel.Workbooks.Open($_filePath,[type]::Missing,[type]::Missing,[type]::Missing,$openPass,$writePass)
            #シートを読み込み
            $book.Worksheets | ForEach-Object {
                #ハイパーリンク取得
                $_.Hyperlinks | ForEach-Object {
                    Write-Host $_.Address
                }

                #図形にあるテキストボックスから取得
                $_.Shapes | ForEach-Object {
                    #グループの場合(msoGroupの値は6)
                    if($_.Type -eq 6){
                        $_.GroupItems | ForEach-Object{
                            if($_.TextFrame2.HasText){
                                Write-Host $_.TextFrame2.TextRange.Text
                            }
                        }
                    }else{
                        if($_.TextFrame2.HasText){
                            Write-Host $_.TextFrame2.TextRange.Text
                        }
                    }
                }
                
                $rowCnt = 0
                #行読み込み
                $_.UsedRange.Rows | ForEach-Object {
                    $rowCnt += 1
                    #列を読み込み
                    $_.Columns | ForEach-Object {
                        #文字が記載されている個所のみ取得
                        if($_.Text -ne ""){
                            Write-Host $_.Text
                            $rowCnt = 0
                        }
                    }
                    #指定された行数以上1文字も記載がなければ
                    #現在確認しているシートをスキップする
                    if($rowCnt -ge $maxRow){
                        Write-Host これ以上記載がないと判断してスキップします
                        break
                    }
                }
            }
        } catch {
            Write-Host $_.Exception.ToString()
        } finally {
            #excelファイル操作終了時の決まった処理(はじまり)
            if($book){
                #Excelを保存をせずに閉じる
                $book.Close($false)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($book) | Out-Null
                $book = $null
                Remove-Variable -Name book -ErrorAction SilentlyContinue
            }

            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            [System.GC]::Collect()

            if($excel){
                $excel.Quit()
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
                $excel = $null
                Remove-Variable -Name excel -ErrorAction SilentlyContinue
                [System.GC]::Collect()
                [System.GC]::WaitForPendingFinalizers()
                [System.GC]::Collect()
            }
            #excelファイル操作終了時の決まった処理(おわり)
        }
    }else{
        Write-Host $_filePath + " が見つかりませんでした。"
    }
}

#今回対象としているExcelファイルかどうかを確認する
function GetTargetExtension($_fileExtension){
    $result = $false
    #excelファイルのみを対象とする
    Select-String -InputObject $_fileExtension -Pattern ".(xlsx?|xlsm)" | ForEach-Object { $_.Matches } | ForEach-Object { $result = $true }
    return $result
}

#引数として受け取ったパスから拡張子を取得する
function GetFileName($_filePath){
    $fileExtension = [System.IO.Path]::GetExtension($_filePath)
    $result = GetTargetExtension -_fileExtension $fileExtension
    if($result){
        ReadExcelFile -_filePath $_filePath
    }else {
        Write-Host 対象となるファイル形式ではありませんでした。
    }
}

#xls, xlsx, xlsmは動作確認済み
$filePath = "Excelまでのフルパス"
GetFileName -_filePath $filePath