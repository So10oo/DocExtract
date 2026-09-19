$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "models"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$files = @(
    @{
        Name = "ch_PP-OCRv5_det_mobile.onnx"
        Url  = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/onnx/PP-OCRv5/det/ch_PP-OCRv5_det_mobile.onnx"
    },
    @{
        Name = "eslav_PP-OCRv5_rec_mobile.onnx"
        Url  = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/onnx/PP-OCRv5/rec/eslav_PP-OCRv5_rec_mobile.onnx"
    },
    @{
        Name = "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx"
        Url  = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.2/onnx/PP-OCRv5/cls/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx"
    }
)

foreach ($file in $files) {
    $dest = Join-Path $outDir $file.Name
    if ((Test-Path $dest) -and ((Get-Item $dest).Length -gt 10000)) {
        Write-Host "Already present $($file.Name)"
        continue
    }
    Write-Host "Downloading $($file.Name) ..."
    Invoke-WebRequest -Uri $file.Url -OutFile $dest -UseBasicParsing
    if ((Get-Item $dest).Length -lt 10000) {
        throw "Downloaded file looks too small: $dest"
    }
}

Write-Host "Models saved to $outDir"
