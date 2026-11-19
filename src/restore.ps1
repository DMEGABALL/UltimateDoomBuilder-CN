# =====================================================================
#  还原原始英文版 / Restore the original English build
#  用法: restore.cmd ["UDB 程序目录"]      默认 ..\app
# =====================================================================
param([string]$AppDir = "")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($AppDir)) { $AppDir = Join-Path $root "..\app" }
if (-not (Test-Path -LiteralPath $AppDir)) {
    Write-Host "[错误] 目录不存在: $AppDir" -ForegroundColor Red
    exit 1
}
$AppDir = (Resolve-Path -LiteralPath $AppDir).Path

$builder = Join-Path $AppDir "Builder.exe"
$backup = Join-Path $root "backup"

Write-Host "=== 还原英文原版 / Restoring original English build ===" -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath (Join-Path $backup "Builder.exe"))) {
    Write-Host "[错误] 缺少 backup\Builder.exe 备份。" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path -LiteralPath $builder)) {
    Write-Host "[错误] 未找到 Builder.exe：$builder" -ForegroundColor Red
    exit 1
}
if (Get-Process -Name Builder -ErrorAction SilentlyContinue) {
    Write-Host "[错误] 请先关闭 Ultimate Doom Builder。" -ForegroundColor Red
    exit 1
}

Copy-Item -LiteralPath (Join-Path $backup "Builder.exe") -Destination $builder -Force
$bakPlugins = Join-Path $backup "Plugins"
if (Test-Path -LiteralPath $bakPlugins) {
    $dstPlugins = Join-Path $AppDir "Plugins"
    if (Test-Path -LiteralPath $dstPlugins) {
        Get-ChildItem -Path $bakPlugins -Filter *.dll | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $dstPlugins -Force
        }
    }
}

Write-Host ""
Write-Host "还原游戏配置 ..." -ForegroundColor Yellow
& (Join-Path $root "configtrans.ps1") -AppDir $AppDir -Revert
if ($LASTEXITCODE -ne 0) { throw "configtrans.ps1 还原游戏配置失败" }

$bakHelp = Join-Path $backup "Refmanual.chm"
if (Test-Path -LiteralPath $bakHelp) {
    Write-Host "还原参考手册 ..." -ForegroundColor Yellow
    Copy-Item -LiteralPath $bakHelp -Destination (Join-Path $AppDir "Refmanual.chm") -Force
}

Write-Host "已还原为原始英文版。" -ForegroundColor Green
Write-Host "Restored to the original English build."
exit 0
