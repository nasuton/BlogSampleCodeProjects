$currentPath = Split-Path -Parent $PSCommandPath

#モジュールの読み込み
$itexDLL = Join-Path $currentPath "itextsharp.dll"
[Reflection.Assembly]::LoadFrom($itexDLL) | Out-Null

function ReadPdfFile($_filePath){
    if(Test-Path -LiteralPath $_filePath){
        $reader = New-Object iTextSharp.text.pdf.PdfReader($_filePath)
        #Pdfの最大ページを取得
        $pages = $reader.NumberOfPages
        for($page = 1; $page -le $pages; $page++){
            #1ページずつ読み込む
            $text = [iTextSharp.text.pdf.parser.PdfTextExtractor]::GetTextFromPage($reader, $page)
            #1行ずつに分ける
            $lines = $text -split "\n"
            foreach($line in $lines){
                Write-Host $line
            }
        }

        $reader.Close()
        $reader.Dispose()
    }else{
        Write-Host $_filePath が見つかりませんでした。
    }
}

#今回対象としているPdfファイルかどうかを確認する
function GetTargetExtension($_fileExtension){
    $result = $false
    #Pdfファイルのみを対象とする
    Select-String -InputObject $_fileExtension -Pattern ".pdf" | ForEach-Object { $_.Matches } | ForEach-Object { $result = $true }
    return $result
}

#引数として受け取ったパスから拡張子を取得する
function GetFileName($_filePath){
    $fileExtension = [System.IO.Path]::GetExtension($_filePath)
    $result = GetTargetExtension -_fileExtension $fileExtension
    if($result){
        ReadPdfFile -_filePath $_filePath
    }else {
        Write-Host 対象となるファイル形式ではありませんでした。
    }
}

$filePath = "Pdfファイルまでのパス"
GetFileName -_filePath $filePath