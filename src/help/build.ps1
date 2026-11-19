# =====================================================================
#  编译简体中文参考手册 / Build the Chinese reference manual
#  需要 HTML Help Workshop（hhc.exe）
#  用法: build.ps1 [-CopyTo <UDB目录>]
# =====================================================================
param([string]$CopyTo = "")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "zh\Refmanual.hhp"
$chm  = Join-Path $root "zh\Refmanual.chm"

function Find-Hhc {
    $candidates = @()
    try {
        $keys = @(
            'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
        )
        $entry = Get-ItemProperty $keys -ErrorAction SilentlyContinue |
                 Where-Object { $_.DisplayName -match 'HTML Help' } | Select-Object -First 1
        if ($entry -and $entry.UninstallString) {
            $dir = Split-Path -Parent ($entry.UninstallString -replace '\s+Uninstall.*$', '')
            if ($dir) { $candidates += (Join-Path $dir 'hhc.exe') }
        }
    } catch { }
    $candidates += @(
        'C:\Program Files (x86)\HTML Help Workshop\hhc.exe',
        'C:\Program Files\HTML Help Workshop\hhc.exe'
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { return $c }
    }
    $cmd = Get-Command hhc.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$hhc = Find-Hhc
if (-not $hhc) {
    Write-Host "[错误] 未找到 hhc.exe，请先安装 HTML Help Workshop。" -ForegroundColor Red
    exit 1
}

Write-Host "使用编译器: $hhc" -ForegroundColor DarkGray
Push-Location (Join-Path $root "zh")
try {
    & $hhc "Refmanual.hhp"
} finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $chm)) {
    Write-Host "[错误] 编译失败，未生成 Refmanual.chm" -ForegroundColor Red
    exit 1
}

Write-Host ("已生成: " + $chm + "  (" + [math]::Round((Get-Item $chm).Length / 1KB, 1) + " KB)") -ForegroundColor Green

if ($CopyTo -ne "") {
    $dst = Join-Path $CopyTo "Refmanual.chm"
    Copy-Item -LiteralPath $chm -Destination $dst -Force
    Write-Host "已复制到: $dst" -ForegroundColor Green
}
