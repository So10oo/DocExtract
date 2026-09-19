$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "0.1.0"
$pub = Join-Path $root "artifacts\desktop"
$rel = Join-Path $root "artifacts\releases"

$det = Join-Path $root "models\ch_PP-OCRv5_det_mobile.onnx"
if (-not (Test-Path $det)) {
    Write-Host "Models missing, running tools/download-models.ps1"
    & (Join-Path $root "tools\download-models.ps1")
}

dotnet publish (Join-Path $root "src\DocExtract.Desktop\DocExtract.Desktop.csproj") `
    -c Release -r win-x64 --self-contained true -o $pub /p:SelfContained=true

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    dotnet tool install -g vpk
    $tools = Join-Path $env:USERPROFILE ".dotnet\tools"
    $env:Path = "$tools;" + $env:Path
}

New-Item -ItemType Directory -Force -Path $rel | Out-Null
vpk pack --packId DocExtract --packVersion $version --packDir $pub --mainExe DocExtract.Desktop.exe --outputDir $rel
Write-Host "Installer output: $rel"
