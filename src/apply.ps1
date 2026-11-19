# =====================================================================
#  应用简体中文语言包 / Apply the Simplified Chinese language pack
#  用法: apply.cmd ["UDB 程序目录"]      默认 ..\app
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
$applyExe = Join-Path $root "tools\apply.exe"
$setresExe = Join-Path $root "tools\setres.exe"
$tsv = Join-Path $root "translate.tsv"

# 强制替换表（撤销/重做等拼接片段），不受上下文判断限制
$forceTsv = Join-Path $root "force-translate.tsv"
if (Test-Path -LiteralPath $forceTsv) { $env:UDB_FORCE_DICT = $forceTsv } else { Remove-Item Env:\UDB_FORCE_DICT -ErrorAction SilentlyContinue }

Write-Host "=== 应用汉化 / Applying Simplified Chinese language pack ===" -ForegroundColor Cyan
Write-Host "目标目录 / Target: $AppDir"
Write-Host ""

if (-not (Test-Path -LiteralPath $builder)) {
    Write-Host "[错误] 未找到 Builder.exe：$builder" -ForegroundColor Red
    Write-Host "用法: apply.cmd `"D:\path\to\Ultimate Doom Builder`""
    exit 1
}
if (Get-Process -Name Builder -ErrorAction SilentlyContinue) {
    Write-Host "[错误] Ultimate Doom Builder 正在运行，请先关闭后再应用。" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path -LiteralPath $applyExe) -or -not (Test-Path -LiteralPath $setresExe)) {
    Write-Host "[错误] 缺少 tools\apply.exe 或 tools\setres.exe，请先运行 patcher\build.cmd。" -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 1/4
Write-Host "[1/6] 替换程序集中的界面字符串 ..." -ForegroundColor Yellow
& $applyExe $tsv $AppDir $builder
if ($LASTEXITCODE -ne 0) { throw "apply.exe 处理 Builder.exe 失败" }

$plugins = @(Get-ChildItem -Path (Join-Path $AppDir "Plugins") -Filter *.dll -ErrorAction SilentlyContinue)
if ($plugins.Count -gt 0) {
    $pluginDir = Join-Path $AppDir "Plugins"
    & $applyExe $tsv $pluginDir @($plugins | ForEach-Object { $_.FullName })
    if ($LASTEXITCODE -ne 0) { throw "apply.exe 处理插件失败" }
}

# ---------------------------------------------------------------- 2/4
Write-Host ""
Write-Host "[2/6] 替换内嵌的操作说明 Actions.cfg（动作-分类-通知描述）..." -ForegroundColor Yellow

$resMap = @(
    @("Builder.exe",                    "CodeImp.DoomBuilder.Resources.Actions.cfg",                                  "actions\Builder_exe\Actions.cfg"),
    @("Plugins\AutomapMode.dll",        "CodeImp.DoomBuilder.AutomapMode.Resources.Actions.cfg",                      "actions\Plugins_AutomapMode_dll\Actions.cfg"),
    @("Plugins\BlockmapExplorer.dll",   "CodeImp.DoomBuilder.BlockmapExplorer.Resources.Actions.cfg",                 "actions\Plugins_BlockmapExplorer_dll\Actions.cfg"),
    @("Plugins\BuilderEffects.dll",     "CodeImp.DoomBuilder.BuilderEffects.Resources.Actions.cfg",                   "actions\Plugins_BuilderEffects_dll\Actions.cfg"),
    @("Plugins\BuilderModes.dll",       "CodeImp.DoomBuilder.BuilderModes.Resources.Actions.cfg",                     "actions\Plugins_BuilderModes_dll\Actions.cfg"),
    @("Plugins\ColorPicker.dll",        "CodeImp.DoomBuilder.ColorPicker.Resources.Actions.cfg",                      "actions\Plugins_ColorPicker_dll\Actions.cfg"),
    @("Plugins\NodesViewer.dll",        "CodeImp.DoomBuilder.Plugins.NodesViewer.Resources.Actions.cfg",              "actions\Plugins_NodesViewer_dll\Actions.cfg"),
    @("Plugins\RejectExplorer.dll",     "CodeImp.DoomBuilder.RejectExplorer.Resources.Actions.cfg",                   "actions\Plugins_RejectExplorer_dll\Actions.cfg"),
    @("Plugins\SoundPropagationMode.dll","CodeImp.DoomBuilder.SoundPropagationMode.Resources.Actions.cfg",           "actions\Plugins_SoundPropagationMode_dll\Actions.cfg"),
    @("Plugins\StairSectorBuilder.dll", "CodeImp.DoomBuilder.StairSectorBuilder.Resources.Actions.cfg",              "actions\Plugins_StairSectorBuilder_dll\Actions.cfg"),
    @("Plugins\TagRange.dll",           "CodeImp.DoomBuilder.TagRange.Resources.Actions.cfg",                         "actions\Plugins_TagRange_dll\Actions.cfg"),
    @("Plugins\ThreeDFloorMode.dll",    "ThreeDFloorMode.Resources.Actions.cfg",                                      "actions\Plugins_ThreeDFloorMode_dll\Actions.cfg"),
    @("Plugins\UDBScript.dll",          "CodeImp.DoomBuilder.UDBScript.Resources.Actions.cfg",                        "actions\Plugins_UDBScript_dll\Actions.cfg"),
    @("Plugins\VisplaneExplorer.dll",   "CodeImp.DoomBuilder.Plugins.VisplaneExplorer.Resources.Actions.cfg",         "actions\Plugins_VisplaneExplorer_dll\Actions.cfg")
)

foreach ($item in $resMap) {
    $asm = Join-Path $AppDir $item[0]
    if (-not (Test-Path -LiteralPath $asm)) {
        Write-Host "  跳过缺失文件 / skip: $($item[0])" -ForegroundColor DarkGray
        continue
    }
    & $setresExe setres $asm $item[1] (Join-Path $root $item[2])
    if ($LASTEXITCODE -ne 0) { throw "setres.exe 替换 $($item[1]) 失败" }
}

# ---------------------------------------------------------------- 3/6
Write-Host ""
Write-Host "[3/6] 替换内嵌的模式帮助提示 Hints.cfg（右侧「帮助」面板内容）..." -ForegroundColor Yellow
$hintsMap = @(
    @("Plugins\AutomapMode.dll",           "CodeImp.DoomBuilder.AutomapMode.Resources.Hints.cfg",           "hints\Plugins_AutomapMode_dll\Hints.cfg"),
    @("Plugins\BlockmapExplorer.dll",      "CodeImp.DoomBuilder.BlockmapExplorer.Resources.Hints.cfg",      "hints\Plugins_BlockmapExplorer_dll\Hints.cfg"),
    @("Plugins\BuilderModes.dll",          "CodeImp.DoomBuilder.BuilderModes.Resources.Hints.cfg",          "hints\Plugins_BuilderModes_dll\Hints.cfg"),
    @("Plugins\RejectExplorer.dll",        "CodeImp.DoomBuilder.RejectExplorer.Resources.Hints.cfg",        "hints\Plugins_RejectExplorer_dll\Hints.cfg"),
    @("Plugins\SoundPropagationMode.dll",  "CodeImp.DoomBuilder.SoundPropagationMode.Resources.Hints.cfg",  "hints\Plugins_SoundPropagationMode_dll\Hints.cfg")
)

# Hints.cfg 的内容是 RTF 片段，非 ASCII 字符必须转成 \uNNNN? 形式，
# 否则 RichTextBox 会按 ANSI 代码页解析，导致中文乱码。
function ConvertTo-RtfAscii([string]$srcPath, [string]$dstPath) {
    $text = [IO.File]::ReadAllText($srcPath, [Text.Encoding]::UTF8)
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $text.ToCharArray()) {
        $code = [int]$ch
        if ($code -lt 128) { [void]$sb.Append($ch) }
        else { [void]$sb.Append('\u' + $code + '?') }
    }
    [IO.File]::WriteAllText($dstPath, $sb.ToString(), [Text.Encoding]::ASCII)
}

foreach ($item in $hintsMap) {
    $asm = Join-Path $AppDir $item[0]
    if (-not (Test-Path -LiteralPath $asm)) {
        Write-Host "  跳过缺失文件 / skip: $($item[0])" -ForegroundColor DarkGray
        continue
    }
    $srcCfg = Join-Path $root $item[2]
    $tmpCfg = Join-Path $env:TEMP ("udb_hints_" + [IO.Path]::GetFileName($item[0]) + ".cfg")
    ConvertTo-RtfAscii $srcCfg $tmpCfg
    & $setresExe setres $asm $item[1] $tmpCfg
    if ($LASTEXITCODE -ne 0) { throw "setres.exe 替换 $($item[1]) 失败" }
    Remove-Item -LiteralPath $tmpCfg -Force -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------- 4/6
Write-Host ""
Write-Host "[4/6] 让 Actions.cfg / Hints.cfg / 游戏配置以 UTF-8 读取（避免中文显示为 ???）..." -ForegroundColor Yellow
& $setresExe utf8 $builder "Actions.ActionManager" "LoadActions"
if ($LASTEXITCODE -ne 0) { throw "setres.exe 修改 UTF-8 读取方式失败" }
& $setresExe utf8 $builder "Actions.HintsManager" "LoadHints"
if ($LASTEXITCODE -ne 0) { throw "setres.exe 修改 Hints.cfg 读取方式失败" }
& $setresExe utf8 $builder "IO.UniversalParser" "LoadConfiguration"
if ($LASTEXITCODE -ne 0) { throw "setres.exe 修改游戏配置读取方式失败" }

# ---------------------------------------------------------------- 5/6
Write-Host ""
Write-Host "[5/6] 翻译游戏配置中的标志 / 难度 / 激活方式名称 ..." -ForegroundColor Yellow
& (Join-Path $root "configtrans.ps1") -AppDir $AppDir
if ($LASTEXITCODE -ne 0) { throw "configtrans.ps1 处理游戏配置失败" }

# ---------------------------------------------------------------- 6/6
Write-Host ""
Write-Host "[6/6] 安装已汉化的参考手册（Refmanual.chm）..." -ForegroundColor Yellow
$helpChm = Join-Path $root "help\zh\Refmanual.chm"
if (Test-Path -LiteralPath $helpChm) {
    Copy-Item -LiteralPath $helpChm -Destination (Join-Path $AppDir "Refmanual.chm") -Force
    Write-Host "  已替换 Refmanual.chm" -ForegroundColor Green
} else {
    Write-Host "  未找到 help\zh\Refmanual.chm，跳过（可运行 help\build.cmd 生成）" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "完成。启动 Builder.exe 查看效果。" -ForegroundColor Green
Write-Host "Done. Launch Builder.exe."
exit 0
