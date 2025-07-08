# 使用方法  
## 基本的な使用方法（行番号ベースで比較）
.\CSVDiff.ps1 -File1 "sample1.csv" -File2 "sample2.csv"  
  
## 出力ファイルを指定
.\CSVDiff.ps1 -File1 "sample1.csv" -File2 "sample2.csv" -OutputFile "my_diff.csv"  
  
## キー列を指定して比較（推奨）
.\CSVDiff.ps1 -File1 "sample1.csv" -File2 "sample2.csv" -KeyColumn "ID"  
  
## 全パラメータを指定
.\CSVDiff.ps1 -File1 "sample1.csv" -File2 "sample2.csv" -OutputFile "result.csv" -KeyColumn "ID"  
  
# パラメータ
- `File1` (必須): 比較元CSVファイルのパス
- `File2` (必須): 比較先CSVファイルのパス
- `OutputFile` (省略可): 出力ファイル名（デフォルト: CSVDiff_Output.csv）
- `KeyColumn` (省略可): キー列名（指定しない場合は行番号ベースで比較）