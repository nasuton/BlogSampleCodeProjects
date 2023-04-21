#テキストファイル内を実際に読み込む
function ReadFile($_filePath){
    if(Test-Path -LiteralPath $_filePath){
        try{
            #容量の大きいファイルを読み込んだ際のメモリを消費を抑えるため ForEach-Object を使用
            Get-Content -LiteralPath $_filePath | ForEach-Object {
                Write-Host $_
            }        
        } catch {
            Write-Host $_.Exception.ToString()
        }
    }else{
        Write-Host 対処となるファイルが存在しませんでした。
    }
}

#今回対象としているテキストファイルかどうかを確認する
function GetTargetExtension($_fileName){
    $result = $false
    #テキストファイルのみを対象とする
    Select-String -InputObject $_fileName -Pattern ".+\.(txt)" | ForEach-Object { $_.Matches } | ForEach-Object { $result = $true }
    return $result
}

#引数として受け取ったパスからファイル名(拡張子含む)を取得する
function GetFileName($_filePath){
    $fileName = [System.IO.Path]::GetFileName($_filePath)
    $result = GetTargetExtension -_fileName $fileName
    if($result){
        ReadFile -_filePath $_filePath
    }else {
        Write-Host 対象となるファイル形式ではありませんでした。
    }
}

#対象となるファイルのフルパス
$filePath = ""
GetFileName -_filePath $filePath