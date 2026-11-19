# =====================================================================
#  翻译 / 还原游戏配置中的界面显示文本
#  只处理 linedefflags / sectorflags / thingflags / linedefactivations / skills
#  这些块中的名称仅用于显示，不参与引擎逻辑。
#  用法: configtrans.ps1 -AppDir <UDB目录> [-Revert]
# =====================================================================
param(
    [Parameter(Mandatory=$true)][string]$AppDir,
    [switch]$Revert
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mapFile = Join-Path $root "config-translate.tsv"
$backupDir = Join-Path $root "backup\configurations"
$configDir = Join-Path $AppDir "Configurations"

if (-not (Test-Path -LiteralPath $configDir)) {
    Write-Host "  跳过：未找到 Configurations 目录" -ForegroundColor DarkGray
    exit 0
}

# ---- 还原 ----
if ($Revert) {
    if (-not (Test-Path -LiteralPath $backupDir)) { exit 0 }
    $n = 0
    Get-ChildItem -LiteralPath $backupDir -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($backupDir.Length).TrimStart('\')
        $dst = Join-Path $configDir $rel
        if (Test-Path -LiteralPath $dst) {
            Copy-Item -LiteralPath $_.FullName -Destination $dst -Force
            $n++
        }
    }
    Write-Host "  已还原 $n 个游戏配置文件。" -ForegroundColor Green
    exit 0
}

# ---- 读取词典 ----
$map = @{}
foreach ($mf in @($mapFile, (Join-Path $root "config-values.tsv"))) {
    if (-not (Test-Path -LiteralPath $mf)) { continue }
    foreach ($line in [IO.File]::ReadAllLines($mf, [Text.Encoding]::UTF8)) {
        if ($line.Length -eq 0 -or $line[0] -eq '#') { continue }
        $p = $line.Split("`t")
        if ($p.Length -lt 2 -or $p[0].Length -eq 0) { continue }
        if (-not $map.ContainsKey($p[0])) { $map[$p[0]] = $p[1] }
    }
}

# ---- 标题词典（动作名、物件名、分类名等）----
$titles = @{}
$titleFile = Join-Path $root "config-titles.tsv"
if (Test-Path -LiteralPath $titleFile) {
    foreach ($line in [IO.File]::ReadAllLines($titleFile, [Text.Encoding]::UTF8)) {
        if ($line.Length -eq 0 -or $line[0] -eq '#') { continue }
        $p = $line.Split("`t")
        if ($p.Length -lt 2 -or $p[0].Length -eq 0) { continue }
        if (-not $titles.ContainsKey($p[0])) { $titles[$p[0]] = $p[1] }
    }
}

$blocks = @(
    "linedefflags", "sectorflags", "thingflags", "linedefactivations", "skills",
    "linedefflags_udmf", "sectorflags_udmf", "thingflags_udmf",
    "sectortypes", "thingtypes",
    "gen_linedeftypes", "gen_sectortypes", "gen_sectortypes_udmf", "gen_thingtypes",
    "enums", "enums_action2", "enums_chex", "enums_chex3", "enums_doom", "enums_harmony",
    "enums_heretic", "enums_hexen", "enums_notdoom", "enums_strife",
    "teams", "keys", "automapstyle",
    "door_speeds", "flat_speeds", "plat_speeds", "polyobj_speeds", "scroll_speeds",
    "sector_scroll_speeds_x", "sector_scroll_speeds_y", "stair_speeds",
    "linedefrenderstyles", "sectorrenderstyles", "thingrenderstyles",
    "sectorportalrenderstyles", "renderstyleceiling", "renderstylefloor",
    "renderstyles", "floorrenderstyles", "ceilingrenderstyles"
)
$valueRegex = [regex]'^(\s*[^=""]*\s*=\s*")([^"]*)("\s*;.*)$'
$titleRegex = [regex]'^(\s*title\s*=\s*")([^"]*)("\s*;.*)$'
$changedFiles = 0
$changedValues = 0

Get-ChildItem -LiteralPath $configDir -Recurse -File -Filter *.cfg | ForEach-Object {
    $file = $_
    $lines = [IO.File]::ReadAllLines($file.FullName, [Text.Encoding]::UTF8)
    $block = $null
    $depth = 0
    $changed = 0

    # 有些 include 文件是「块片段」（如 Doom_sectors.cfg），没有块名，直接是 0 = "..." 列表
    $isFragment = $false
    foreach ($ln in $lines) {
        $t0 = $ln.Trim()
        if ($t0 -eq "" -or $t0.StartsWith("//")) { continue }
        if ($t0 -match '^-?\d+\s*=\s*"') { $isFragment = $true }
        break
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $trim = $lines[$i].Trim()

        # 标题（动作名 / 物件名 / 分类名等）在任意位置翻译
        $tm = $titleRegex.Match($lines[$i])
        if ($tm.Success) {
            $tv = $tm.Groups[2].Value
            $zh = $null
            if ($titles.ContainsKey($tv)) { $zh = $titles[$tv] }
            elseif ($titles.ContainsKey($tv.Trim())) { $zh = $titles[$tv.Trim()] }
            if ($zh -ne $null) {
                $lines[$i] = $tm.Groups[1].Value + $zh + $tm.Groups[3].Value
                $changed++
                continue
            }
        }

        if ($block -eq $null) {
            if ($blocks -contains $trim.ToLowerInvariant()) { $block = $trim.ToLowerInvariant(); continue }

            # 块片段文件：直接翻译值
            if ($isFragment) {
                $fm = $valueRegex.Match($lines[$i])
                if ($fm.Success) {
                    $fv = $fm.Groups[2].Value
                    $fz = $null
                    if ($map.ContainsKey($fv)) { $fz = $map[$fv] }
                    elseif ($map.ContainsKey($fv.Trim())) { $fz = $map[$fv.Trim()] }
                    if ($fz -ne $null) {
                        $lines[$i] = $fm.Groups[1].Value + $fz + $fm.Groups[3].Value
                        $changed++
                    }
                }
            }
            continue
        }

        if ($trim -eq "{") { $depth++; continue }
        if ($trim -eq "}") {
            $depth--
            if ($depth -le 0) { $block = $null; $depth = 0 }
            continue
        }

        $m = $valueRegex.Match($lines[$i])
        if (-not $m.Success) { continue }
        $value = $m.Groups[2].Value
        $zhv = $null
        if ($map.ContainsKey($value)) { $zhv = $map[$value] }
        elseif ($map.ContainsKey($value.Trim())) { $zhv = $map[$value.Trim()] }
        if ($zhv -eq $null) { continue }
        $lines[$i] = $m.Groups[1].Value + $zhv + $m.Groups[3].Value
        $changed++
    }

    if ($changed -eq 0) { return }

    # 首次修改前备份原始文件
    $rel = $file.FullName.Substring($configDir.Length).TrimStart('\')
    $bak = Join-Path $backupDir $rel
    if (-not (Test-Path -LiteralPath $bak)) {
        $bakParent = Split-Path -Parent $bak
        if (-not (Test-Path -LiteralPath $bakParent)) { New-Item -ItemType Directory -Force -Path $bakParent | Out-Null }
        Copy-Item -LiteralPath $file.FullName -Destination $bak -Force
    }

    [IO.File]::WriteAllLines($file.FullName, $lines, (New-Object System.Text.UTF8Encoding($false)))
    $changedFiles++
    $changedValues += $changed
}

Write-Host "  游戏配置：$changedFiles 个文件，$changedValues 处显示文本。" -ForegroundColor Green
