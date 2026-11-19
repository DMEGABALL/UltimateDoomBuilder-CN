# =====================================================================
#  生成手机版单文件参考手册 -> docs\index.html
#  需要 .NET Framework 4.x 自带的 csc.exe
# =====================================================================
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = (Resolve-Path (Join-Path $root "..\..")).Path
$src  = Join-Path $root "ManualBuilder.cs"
$exe  = Join-Path $root "ManualBuilder.exe"
$help = Join-Path $repo "src\help\zh"
$out  = Join-Path $repo "docs\index.html"

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $csc)) {
    Write-Host "[错误] 未找到 csc.exe" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path -LiteralPath $help)) {
    Write-Host "[错误] 未找到手册目录: $help" -ForegroundColor Red
    exit 1
}

& $csc /nologo /codepage:65001 /target:exe /optimize+ /r:System.Drawing.dll /r:System.Core.dll "/out:$exe" $src
if ($LASTEXITCODE -ne 0) { Write-Host "[错误] 编译失败" -ForegroundColor Red; exit 1 }

& $exe $help $out
if ($LASTEXITCODE -ne 0) { Write-Host "[错误] 生成失败" -ForegroundColor Red; exit 1 }

Write-Host "完成: $out" -ForegroundColor Green
