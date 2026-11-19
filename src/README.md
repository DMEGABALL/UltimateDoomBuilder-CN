# 汉化工程说明 / Localization Toolkit

本目录是 Ultimate Doom Builder（UDB）简体中文汉化的**全部源码与数据**：词典、内嵌资源、C# 补丁工具、脚本与原始二进制备份。程序本体位于 [`../app/`](../app/)。

汉化方式为**程序集字符串替换**（IL 级），不重新编译 UDB，也不修改功能逻辑。

## 目录

```
src/
├─ translate.tsv          词典：英文原文 <TAB> 中文译文（UTF-8）
├─ config-translate.tsv   游戏配置显示名称词典（标志 / 难度 / 激活方式）
├─ configtrans.ps1        翻译或还原 Configurations\*.cfg
├─ actions/               已汉化的内嵌 Actions.cfg，按程序集分目录
│  ├─ Builder_exe/
│  └─ Plugins_*/
├─ hints/                 已汉化的内嵌 Hints.cfg（各编辑模式的右侧帮助提示）
│  └─ Plugins_*/
├─ backup/                原始英文版二进制、游戏配置与参考手册（用于还原）
│  ├─ Builder.exe
│  ├─ Plugins/
│  ├─ configurations/
│  └─ Refmanual.chm
├─ help/                  参考手册汉化工程
│  ├─ en/                 原版英文页面（解包自 Refmanual.chm，仅作对照）
│  ├─ zh/                 简体中文页面 + Refmanual.hhp + 编译产物
│  ├─ build.cmd / build.ps1   用 hhc.exe 重新编译 CHM
│  └─ README.md           编译与修改说明
├─ patcher/               补丁工具 C# 源码
│  ├─ apply.cs            界面字符串 / 特性 / 内嵌资源替换
│  ├─ setres.cs           内嵌资源读取、替换与 ASCII→UTF8 修正
│  ├─ extract.cs          导出字符串及其 IL 上下文
│  ├─ scan.cs             导出字符串并标注用途（筛查漏译）
│  ├─ toasts.cs           导出通知（Toast）标题与描述
│  └─ build.cmd           使用系统自带 csc.exe 编译
├─ manual/                手机版手册生成器（C#）
│  ├─ ManualBuilder.cs    把手册 HTML 打包成单文件（图片内联、响应式布局）
│  ├─ build.cmd / build.ps1   编译并生成 ..\..\docs\index.html
│  └─ ManualBuilder.exe   编译产物
├─ tools/                 预编译工具与 Mono.Cecil
│  ├─ apply.exe
│  ├─ setres.exe
│  ├─ extract.exe
│  ├─ scan.exe
│  ├─ toasts.exe
│  └─ Mono.Cecil.dll
├─ apply.ps1 / apply.cmd              应用汉化
├─ restore.ps1 / restore.cmd          还原英文版
├─ extract-strings.ps1 / .cmd         导出字符串
└─ fetch-upstream.ps1                 下载对应上游源码
```

## 常用命令

```bat
:: 应用到指定 UDB 目录（省略参数则作用于 ..\app）
apply.cmd "D:\path\to\Ultimate Doom Builder"

:: 还原英文原版
restore.cmd "D:\path\to\Ultimate Doom Builder"

:: 导出全部字符串（含上下文），用于适配新版本
extract-strings.cmd strings.tsv "D:\path\to\Ultimate Doom Builder"

:: 重新编译补丁工具
patcher\build.cmd

:: 单独翻译或还原游戏配置
powershell -ExecutionPolicy Bypass -File configtrans.ps1 -AppDir ..\app
powershell -ExecutionPolicy Bypass -File configtrans.ps1 -AppDir ..\app -Revert

:: 重新编译参考手册（需要 HTML Help Workshop）
help\build.cmd

:: 生成手机版单文件手册（输出 ..\..\docs\index.html）
manual\build.cmd
```

`apply.cmd` / `restore.cmd` 是纯 ASCII 入口，实际逻辑在对应的 `.ps1`（UTF-8 BOM）中，以便正确显示中文提示。执行策略通过 `-ExecutionPolicy Bypass` 绕过，不修改系统设置。

## 词典格式 `translate.tsv`

- UTF-8 编码，每行 `英文原文<TAB>中文译文`；
- `#` 开头为注释，空行忽略；
- 同一英文原文只取第一次出现的译文；
- 支持转义：`\\`、`\t`、`\r`、`\n`；
- 翻译为空的行会被忽略。

### 编写规范

1. **保持首尾空格**：如 ` Shade` → ` 阴影`，标签排版依赖这些空格。
2. **保留快捷键**：`&File` → `文件(&F)`；`F&lip Sidedefs` → `翻转侧面(&L)`。
3. **保留省略号与冒号**：`...` 仍用 `...`，英文 `:` 仍用半角 `:`，不改为全角。
4. **双语术语**：专业名词写作 `中文（English）`，例如 `扇区（Sector）`；短标签可只用中文。
5. **不改内部标识符**：UDMF 字段名、配置键、脚本关键字、路径、URL、WAD/Lump 名称保持原样。
6. **单行消息**要简短，避免撑破固定尺寸控件。

## 实现细节

### 1. 界面字符串（`apply.cs`）

遍历程序集的 IL，对每条 `ldstr` 判断其后续用途：

- 若紧随 `set_Text` / `set_Title` / `set_ToolTipText` 等属性赋值，或消息框、状态栏、撤销记录等调用 → 翻译；
- 若用于 `set_Name`、`set_ImageKey`、配置读取、比较、路径、正则、日志等 → 跳过；
- 同时处理 `[Description]`、`[Category]`、`[DisplayName]` 特性，以及任意特性中名为 `DisplayName` / `Description` 的命名参数（如 `[EditMode]`、`[FieldDescription]`）；
- 对程序集内嵌的 `.resources` 中的字符串资源一并替换。

替换结果写回原程序集（Mono.Cecil），不改变方法体结构与控件布局。

### 2. 动作与通知（`setres.cs` + `actions/`）

UDB 的动作分类、动作名、说明与通知描述存放在各程序集内嵌的 `Actions.cfg` 文本资源中。汉化版整体替换该资源：

```bat
setres.exe setres Builder.exe CodeImp.DoomBuilder.Resources.Actions.cfg actions\Builder_exe\Actions.cfg
```

### 3. 模式帮助（`setres.cs` + `hints/`）

右侧「帮助」面板的内容来自各程序集内嵌的 `Hints.cfg`（RTF 片段，按 `class` / `group` 组织）。`hints/` 中保存中文版，`apply.ps1` 在写入前把非 ASCII 字符转成 RTF 的 `\uNNNN?` 形式（`ConvertTo-RtfAscii`），再替换资源：

```bat
setres.exe setres BuilderModes.dll CodeImp.DoomBuilder.BuilderModes.Resources.Hints.cfg hints\Plugins_BuilderModes_dll\Hints.cfg
```

> 不转义直接写入会乱码：`RichTextBox.SelectedRtf` 按 ANSI 解释内容，`\uNNNN?` 才是与代码页无关的写法。

### 4. 编码修正

`ActionManager.LoadActions()` 原本以 `Encoding.ASCII` 读取 `Actions.cfg`，`HintsManager.LoadHints()` 读取 `Hints.cfg`，`UniversalParser.LoadConfiguration()` 读取游戏配置，均以 ASCII 解码，中文会变成 `?`。`setres.exe utf8` 将这些 `get_ASCII` 调用改为 `get_UTF8`：

```bat
setres.exe utf8 Builder.exe "Actions.ActionManager" "LoadActions"
setres.exe utf8 Builder.exe "Actions.HintsManager" "LoadHints"
setres.exe utf8 Builder.exe "IO.UniversalParser" "LoadConfiguration"
```

这是仅有的三处逻辑级改动。命令是幂等的：已修正时会输出 `already UTF-8`。

### 5. 游戏配置（`configtrans.ps1` + `config-translate.tsv`）

`Configurations\*.cfg` 中只翻译以下块内的引号文本：

- `linedefflags` / `sectorflags` / `thingflags`：编辑对话框中的标志名称；
- `linedefactivations`：动作激活方式；
- `skills`：难度名称。

配置键、数值、`prefix`、脚本标识符等保持不变。首次修改前会把原文件备份到 `backup\configurations\`，`restore` 时一并还原。

## 适配新版本

1. 安装新版 UDB，或将其文件覆盖到 `../app/`；
2. `extract-strings.cmd strings.tsv` 导出字符串；
3. 将 `strings.tsv` 中的英文原文与 `translate.tsv` 对比：新增条目补译，变更条目更新；
4. 若新版 `Actions.cfg` 结构变化，用 `setres.exe getres <程序集> <资源名>` 导出原文后重新翻译；
5. 运行 `apply.cmd`，逐项检查界面与日志输出。

`apply.exe` 对未匹配的条目静默跳过，方便逐步补齐。

## 兼容性

- 工具基于 .NET Framework 4.x（Windows 10/11 自带），使用 `csc.exe` 即可编译；
- `Mono.Cecil.dll` 为 0.11.5，MIT 许可，见 [`tools/Mono.Cecil.LICENSE.txt`](tools/Mono.Cecil.LICENSE.txt)；
- 程序本体与上游一致，仅替换字符串、`Actions.cfg`、游戏配置显示文本，并修正两处文本编码读取方式。

## 许可

本目录中的词典、资源与工具代码作为 UDB 衍生作品的一部分，以 **GNU GPL v3** 授权。详见仓库根目录 [`LICENSE`](../LICENSE)、[`NOTICE.md`](../NOTICE.md) 与 [`README.md`](../README.md) 的「许可与合规」。
