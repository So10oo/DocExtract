$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\nuget"
New-Item -ItemType Directory -Force -Path $out | Out-Null

dotnet pack (Join-Path $root "src\DocExtract\DocExtract.csproj") -c Release -o $out
dotnet pack (Join-Path $root "src\DocExtract.Engines.Onnx\DocExtract.Engines.Onnx.csproj") -c Release -o $out
dotnet pack (Join-Path $root "src\DocExtract.Models\DocExtract.Models.csproj") -c Release -o $out

Write-Host "NuGet packages: $out"
