# =====================================================================
#  下载上游源码 / Fetch upstream source
#  版本: Ultimate Doom Builder R4333 (3.0.0.4333)
#  commit: bfb9653951445c7a4e4c46b1600e14358e901e44
#  输出: src\upstream-src\UltimateDoomBuilder-<commit>\
# =====================================================================
param(
    [string]$Destination = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$commit = "bfb9653951445c7a4e4c46b1600e14358e901e44"
$url = "https://codeload.github.com/UltimateDoomBuilder/UltimateDoomBuilder/zip/$commit"

if ([string]::IsNullOrWhiteSpace($Destination)) { $Destination = Join-Path $root "upstream-src" }
$Destination = [IO.Path]::GetFullPath($Destination, $root)
New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$zip = Join-Path $Destination "UltimateDoomBuilder-$commit.zip"

Write-Host "下载上游源码 / Downloading upstream source ..." -ForegroundColor Cyan
Write-Host $url
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing

Write-Host "解压 / Extracting ..." -ForegroundColor Cyan
Expand-Archive -LiteralPath $zip -DestinationPath $Destination -Force
Remove-Item -LiteralPath $zip -Force

$dir = Join-Path $Destination "UltimateDoomBuilder-$commit"
Write-Host "完成 / Done: $dir" -ForegroundColor Green
Write-Host "上游构建需要 Visual Studio（C# + C++ BuilderNative）与 .NET Framework 4.7.2。"
