#wordファイルを読み込む
function ReadWordFile($_filePath){
    if(Test-Path -LiteralPath $_filePath){
        $word = New-Object -ComObject word.Application
        try {
            #wordを非表示で実行
            $word.Visible = $false
            #警告ウィンドウを表示しない
            #$word.DisplayAlerts = "wdAlertsNone"
            #マクロを無効化する
            #$word.AutomationSecurity = "msoAutomationSecurityForceDisable"
            #Wordを開く際にパスワードが必要な場合(必要ない場合は無視される)
            $openPass = ""
            #Wordテンプレートを開く際にパスワードが必要な場合(必要ない場合は無視される)
            $openTemple = ""
            $doc = $word.Documents.Open($_filePath,[type]::Missing,[type]::Missing,[type]::Missing,$openPass,$openTemple)

            #ハイパーリンク取得
            $doc.Hyperlinks | ForEach-Object {
                Write-Host $_.Address
            }

            #行ごとに読み込み
            $doc.Paragraphs | ForEach-Object {
                Write-Host $_.Range.Text
            }
            
            #図形にあるテキストボックスから取得
            $doc.Shapes | ForEach-Object {
                #描画キャンパス(msoCanvasは値が20)
                if($_.Type -eq 20){
                    #内すべてのアイテムに対して実行
                    $_.CanvasItems | ForEach-Object {
                        #グループの場合(msoGroupの値は6)
                        if($_.Type -eq 6){
                            $_.GroupItems | ForEach-Object {
                                if($_.TextFrame.HasText){
                                    Write-Host $_.TextFrame.TextRange.Text
                                }
                            }
                        }else{
                            if($_.TextFrame.HasText){
                                Write-Host  $_.TextFrame.TextRange.Text
                            }
                        }
                    }
                }
                #グループの場合(msoGroupの値は6)
                elseif($_.Type -eq 6){
                    $_.GroupItems | ForEach-Object {
                        if($_.TextFrame.HasText){
                            Write-Host $_.TextFrame.TextRange.Text
                        }
                    }
                }
                #それ以外
                else{
                    if($_.TextFrame.HasText){
                        Write-Host $_.TextFrame.TextRange.Text
                       }
                   }
               }
        } catch {
            Write-Host $_.Exception.ToString()
        } finally {
            #wordファイル操作終了時の決まった処理(はじまり)
            if($doc){
                $doc.Close($false)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($doc) | Out-Null
                $doc = $null
                Remove-Variable -Name doc -ErrorAction SilentlyContinue
            }

            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            [System.GC]::Collect()

            if($word){
                $word.Quit()
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
                $word = $null
                Remove-Variable -Name word -ErrorAction SilentlyContinue
                [System.GC]::Collect()
                [System.GC]::WaitForPendingFinalizers()
                [System.GC]::Collect()
            }
            #wordファイル操作終了時の決まった処理(おわり)
        }
    } else {
        Write-Host $_filePath が見つかりませんでした
    }
}

#今回対象としているwordファイルかどうかを確認する
function GetTargetExtension($_fileExtension){
    $result = $false
    #wordファイルのみを対象とする
    Select-String -InputObject $_fileExtension -Pattern ".(docx?|docm)" | ForEach-Object { $_.Matches } | ForEach-Object { $result = $true }
    return $result
}

#引数として受け取ったパスから拡張子を取得する
function GetFileName($_filePath){
    $fileExtension = [System.IO.Path]::GetExtension($_filePath)
    $result = GetTargetExtension -_fileExtension $fileExtension
    if($result){
        ReadWordFile -_filePath $_filePath
    }else {
        Write-Host 対象となるファイル形式ではありませんでした。
    }
}

#doc, docx, docmは動作確認済み
$filePath = "Wordまでのパス"
GetFileName -_filePath $filePath