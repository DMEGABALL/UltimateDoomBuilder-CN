# =====================================================================
#  从程序集提取全部字符串（含 IL 上下文分类），用于补充词条
#  用法: extract-strings.cmd <输出.tsv> ["UDB 程序目录"]
#  输出列: 程序集 <TAB> 类型 <TAB> 上下文 <TAB> 英文原文
# =====================================================================
param(
    [string]$OutFile = "",
    [string]$AppDir = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($AppDir)) { $AppDir = Join-Path $root "..\app" }
if (-not (Test-Path -LiteralPath $AppDir)) { Write-Host "[错误] 目录不存在: $AppDir"; exit 1 }
$AppDir = (Resolve-Path -LiteralPath $AppDir).Path

if ([string]::IsNullOrWhiteSpace($OutFile)) { $OutFile = Join-Path $root "strings.tsv" }
elseif (-not [IO.Path]::IsPathRooted($OutFile)) { $OutFile = Join-Path $root $OutFile }
$OutFile = [IO.Path]::GetFullPath($OutFile)

$extractExe = Join-Path $root "tools\extract.exe"
if (-not (Test-Path -LiteralPath $extractExe)) {
    Write-Host "[错误] 缺少 tools\extract.exe，请先运行 patcher\build.cmd。"
    exit 1
}

$asmList = @((Join-Path $AppDir "Builder.exe"))
$asmList += @(Get-ChildItem -Path (Join-Path $AppDir "Plugins") -Filter *.dll -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })

& $extractExe $OutFile @asmList
if ($LASTEXITCODE -ne 0) { Write-Host "[错误] 提取失败。"; exit 1 }

Write-Host "已输出到: $OutFile" -ForegroundColor Green
