# Ultimate Doom Builder 简体中文汉化 / Simplified Chinese Localization

| 项目 | 说明 |
|---|---|
| 汉化版本 | **v1.3.1** |
| 适用版本 | Ultimate Doom Builder R4333（3.0.0.4333，64 位） |
| 发布名称 | `UltimateDoomBuilder-CN-v1.3.1-R4333` |
| 上游源码 | <https://github.com/UltimateDoomBuilder/UltimateDoomBuilder>（commit `bfb9653`） |
| 许可协议 | GNU GPL v3（与原项目一致） |
| 项目性质 | 非官方社区汉化，与 UDB 官方无关 |

> 版本号规则：`v1.3.1` 是**汉化包自身**的版本，`R4333` 是所基于的 **UDB 上游版本**。两者一一对应，升级上游时汉化版本号另行递增。

> 上游目前最新版本为 R4336。本汉化基于已完整验证的 **R4333**；如需用于更新版本，请参照「[适配新版 UDB](#适配新版-udb)」流程，词典未覆盖的新文本会保持英文。

> [!IMPORTANT]
> **AI 生成声明**  
> 本项目的**全部译文均由 AI（大语言模型）生成**，人工仅负责制定术语表、提供上下文、校验格式与少量修正。  
> 译文可能存在不准确、生硬或词不达意之处，欢迎通过 Issue / PR 指正。

---

## 简介

[Ultimate Doom Builder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder)（简称 UDB）是一款开源的 Doom 地图编辑器。程序本身没有多语言机制，界面文本全部硬编码在程序集中。

本汉化通过**替换程序集内的界面字符串**实现简体中文界面：不重新编译、不改动任何功能逻辑、不需要 .NET SDK。同时保留完整的词典与工具链，可自行修改译文或适配新版本。

汉化采用 **中英双语** 呈现：专业术语写作 `中文（English）`，方便对照 UDB 文档、教程与社区讨论中的英文术语；空间紧张的小标签只保留中文，以保证原有布局不被撑破。

## 下载与发布包

| 文件 | 内容 | 体积 |
|---|---|---|
| `UltimateDoomBuilder-CN-v1.3.1-R4333-win64.zip` | 程序本体，解压即用（含汉化参考手册） | 约 14 MB |
| `UltimateDoomBuilder-CN-v1.3.1-R4333-src.zip` | 汉化源码：词典、工具链、手册工程、原始备份 | 约 21 MB |
| `UltimateDoomBuilder-CN-v1.3.1-R4333-manual-mobile.html` | 手机版参考手册（单文件、离线可用） | 约 5.7 MB |

> 手机版手册也可以直接在线看：<https://dmegaball.github.io/UltimateDoomBuilder-CN/>（GitHub Pages，由 `docs/index.html` 生成）。

发布包由仓库中的 `app/` 与 `src/` 原样打包（源码包附带 `LICENSE`）。不下载压缩包时，也可以直接克隆仓库使用 `app/Builder.exe`。

## 手机版参考手册

CHM 只能在 Windows 上查看，因此另外生成了一份**单文件 HTML**：`docs/index.html`（发布时命名为 `…-manual-mobile.html`）。

- 所有页面、图片、样式都内联在一个文件里，**离线可用**，拷到手机即可用浏览器打开；
- 手机端为抽屉式目录 + 正文卡片布局，表格可横向滑动，图片自适应宽度；
- 支持目录搜索、上一页 / 下一页、`#/页面@锚点` 深链接、记住上次阅读位置；
- 桌面浏览器打开时自动变为左侧目录的宽屏布局；
- 在线版：<https://dmegaball.github.io/UltimateDoomBuilder-CN/>

重新生成（修改 `src\help\zh\` 后）：

```bat
src\manual\build.cmd
```

## 目录结构

本仓库将**程序本体**与**汉化源码**分开存放：

```
udb/
├─ app/                  # 程序本体：可直接运行的汉化版 UDB（绿色便携）
│  ├─ Builder.exe        # 已汉化的主程序
│  ├─ Plugins/           # 已汉化的插件
│  ├─ Configurations/    # 游戏配置（标志、难度等显示名称已汉化）
│  ├─ Refmanual.chm      # 已汉化的参考手册
│  └─ ...
├─ src/                  # 汉化源码：词典、补丁工具、脚本、原始备份
│  ├─ translate.tsv      # 词典：英文原文 <TAB> 中文译文
│  ├─ config-translate.tsv # 游戏配置显示名称词典（标志 / 难度 / 激活方式）
│  ├─ config-titles.tsv  # 游戏配置标题词典（动作名 / 物件名 / 分类名）
│  ├─ config-values.tsv  # 配置选项词典（UDMF 标志 / 扇区效果 / 泛化选项）
│  ├─ force-translate.tsv # 强制替换表（撤销/重做等拼接片段）
│  ├─ configtrans.ps1    # 翻译 / 还原游戏配置
│  ├─ actions/           # 已汉化的内嵌 Actions.cfg（动作/通知说明）
│  ├─ hints/             # 已汉化的内嵌 Hints.cfg（模式帮助提示）
│  ├─ help/              # 参考手册汉化工程（HTML + hhp + 编译脚本）
│  ├─ patcher/           # 补丁工具的 C# 源码与编译脚本
│  ├─ manual/            # 手机版手册生成器（C#，输出 docs/index.html）
│  ├─ tools/             # 预编译工具（apply / setres / extract / scan）
│  ├─ backup/            # 原始英文版二进制、配置与手册备份（用于还原）
│  ├─ apply.cmd          # 应用汉化
│  ├─ restore.cmd        # 还原英文版
│  └─ extract-strings.cmd # 从程序集导出字符串（适配新版本）
├─ docs/                 # 手机版参考手册（GitHub Pages 源，index.html）
├─ README.md
├─ CHANGELOG.md          # 更新日志
├─ VERSION               # 汉化包版本号
├─ LICENSE               # GNU GPL v3 全文
├─ NOTICE.md             # 版权、第三方组件与修改说明
├─ .gitattributes
└─ .gitignore
```

## 快速开始

### 直接使用

运行 `app\Builder.exe` 即可，界面为简体中文。无需安装，整个目录可放在任意位置。

如果之前用过英文版，个人设置保存在 `%LOCALAPPDATA%\Doom Builder` 下，与本程序目录无关。

### 在现有 UDB 安装上应用汉化

```bat
src\apply.cmd "D:\path\to\Ultimate Doom Builder"
```

省略参数时默认作用于同仓库的 `..\app`。脚本依次完成 6 个步骤：

1. 替换 `Builder.exe` 与 `Plugins\*.dll` 中的界面字符串（含 `[EditMode]`、`[FieldDescription]`、`[ErrorChecker]`、`[FindReplace]` 等特性文本）；
2. 替换内嵌的 `Actions.cfg`（动作分类、动作名、说明、通知）；
3. 替换内嵌的 `Hints.cfg`（右侧「帮助」面板中各模式的操作提示）；
4. 修正 `Actions.cfg` / `Hints.cfg` / 游戏配置的读取编码（ASCII → UTF-8）；
5. 翻译游戏配置中的线段/扇区/物件标志、难度与激活方式名称（仅显示文本）；
6. 安装汉化后的 `Refmanual.chm`（参考手册）。

> 运行环境：Windows 10/11；需要 .NET Framework 4.7.2+（系统通常自带）与 PowerShell。  
> 应用前请关闭 UDB，脚本会检查进程占用。

### 还原英文版

```bat
src\restore.cmd "D:\path\to\Ultimate Doom Builder"
```

从 `src\backup\` 恢复原始的 `Builder.exe`、插件 DLL 与游戏配置文件。

### UDB 升级之后

升级会覆盖 `Builder.exe` 与 `Plugins\*.dll`。重新运行一次 `apply.cmd` 即可；词典中未匹配到的条目会自动跳过，不会报错。若要适配新版本新增的文本，见下文「维护与适配」。

## 汉化范围

| 模块 | 内容 |
|---|---|
| 主界面 | 菜单、工具栏提示、状态栏、标签页、对话框标题与按钮 |
| 编辑窗口 | 扇区 / 线段 / 物件 / 顶点编辑，含 UDMF 扩展属性与标志 |
| 游戏配置 | 线段/扇区/物件标志、难度名称、动作激活方式（显示文本） |
| 配置数据 | 动作名、物件名、分类名、扇区效果名、UDMF 标志、泛化动作/效果选项 |
| 模式与面板 | 编辑模式名称、侧栏面板标题、撤销/重做记录 |
| 地图分析 | 检查项名称（`[ErrorChecker]`）、分析窗口按钮 |
| 查找替换 | 搜索类型（`[FindReplace]`）、对话框字段 |
| 视图开关 | 渲染雾 / 渲染天空 / 经典渲染（视觉模式）等工具栏按钮 |
| 参考手册 | `Refmanual.chm` 全部 91 个页面（编辑模式、窗口、游戏配置、GZDoom 特性等） |
| 模式帮助 | 右侧「帮助」面板中各模式的操作提示（`Hints.cfg`，193 条） |
| 首选项 | 界面、控制、外观、脚本编辑器、粘贴、恢复、通知及各插件页 |
| 控制-动作 | 动作分类、动作名称、动作说明（内嵌 `Actions.cfg`） |
| 通知弹窗 | 通知标题与描述 |
| 插件 | 3D 地板、UDBScript、自动地图、节点查看器、标签浏览器、楼梯生成器、块图浏览器等 |
| 其他 | 属性网格说明、工具提示、多行帮助文本 |

规模统计（R4333）：

- 界面词典条目 **2367** 条（`translate.tsv`）；
- 配置标题（动作/物件/分类名）**2874** 条（`config-titles.tsv`）；
- 配置选项（UDMF 标志、扇区效果、泛化选项、枚举下拉）**1147** 条（`config-values.tsv`）；
- 程序集内实际替换 **3874** 处（主程序 1953 处，插件 1921 处）；
- 游戏配置显示文本 **7989** 处（62 个 `.cfg` 文件）；
- 替换内嵌 `Actions.cfg` **14** 个。

> 游戏配置中的动作名、物件名、扇区效果名、UDMF 标志等显示文本现已全部汉化（见 `src\config-titles.tsv`、`src\config-values.tsv`），仅保留游戏/引擎专有名词（如 `Polyobject`、`BFG9000`）。

### 术语对照

| English | 界面写法 |
|---|---|
| Sector | 扇区（Sector） |
| Linedef | 线段（Linedef） |
| Sidedef | 侧面（Sidedef） |
| Thing | 物件（Thing） |
| Vertex | 顶点（Vertex） |
| Surface | 表面（Surface） |
| Texture | 贴图（Texture） |
| Flat | 平面贴图（Flat） |
| Brightness | 亮度（Brightness） |
| Tag | 标签（Tag） |
| Action | 动作（Action） |
| Args | 参数（Args） |
| Flags | 标志（Flags） |
| Nodebuilder | 节点构建器（Nodebuilder） |
| Visual Mode | 视觉模式（Visual Mode） |
| Classic Modes | 经典模式（Classic Modes） |
| 3D Floor | 3D 地板（3D Floor） |
| Control Sector | 控制扇区（Control Sector） |
| Game Configuration | 游戏配置（Game Configuration） |
| Prefab | 预制件（Prefabs） |
| Blockmap | 块图（Blockmap） |

完整术语表与逐条译文见 [`src/translate.tsv`](src/translate.tsv)。

## 实现原理

1. **界面字符串**：遍历程序集 IL，按上下文判断每个字符串的用途（控件文本、消息框、状态栏、属性说明等），用词典替换，并避开控件名、配置键、UDMF 字段名、脚本关键字等内部标识符。
2. **特性文本**：`[DisplayName]`、`[Description]`、`[Category]` 以及项目自定义特性（如 `[EditMode(DisplayName = "...")]`、`[FieldDescription(Description = "...")]`）中的显示文本一并替换，用于模式名称、UDMF 字段说明等。
3. **动作与通知**：UDB 的动作名称/说明存放于各程序集内嵌的 `Actions.cfg` 文本资源，整体替换为汉化版本。
4. **模式帮助**：右侧「帮助」面板的内容来自各程序集内嵌的 `Hints.cfg`（RTF 片段）。译文按 RTF 的 `\uNNNN?` 形式转义后写入，避免 RichTextBox 按 ANSI 代码页解析导致乱码。
5. **游戏配置**：`Configurations\*.cfg` 中线段/扇区/物件标志、难度名称、激活方式等**纯显示文本**按块替换；配置键、数值、脚本标识符保持不变。
6. **编码修正**：`ActionManager.LoadActions()`、`HintsManager.LoadHints()` 与 `UniversalParser.LoadConfiguration()` 原本以 `Encoding.ASCII` 读取文本，改为 `Encoding.UTF8`，否则中文会显示为 `?`。这是仅有的三处逻辑级改动，只影响文本解码。
7. **参考手册**：把原版 `Refmanual.chm` 解包为 HTML，逐页翻译后修改 `<meta charset>` 为 UTF-8，再用 HTML Help Workshop 的 `hhc.exe` 重新编译成 CHM（目录 `Contents.hhc` 以 GBK 保存，供编译器识别）。

补丁不改写 C# / C++ 源文件，也不重新编译，因此无需 Visual Studio 或 .NET SDK。工具基于 [Mono.Cecil](https://github.com/jbevain/cecil)。

## 维护与适配

### 修改译文

编辑 `src\translate.tsv`（格式：`英文原文<TAB>中文译文`，支持 `\r\n`、`\t` 转义，`#` 开头为注释），然后重新运行：

```bat
src\apply.cmd
```

游戏配置中的标志/难度/激活方式译文在 `src\config-translate.tsv`，单独重新应用配置：

```powershell
powershell -ExecutionPolicy Bypass -File src\configtrans.ps1 -AppDir app
```

参考手册的译文在 `src\help\zh\`。修改后需要 HTML Help Workshop 重新编译：

```bat
src\help\build.cmd
src\apply.cmd
```

### 适配新版 UDB

1. 用新版本覆盖 `app\` 中的程序；
2. `src\restore.cmd` 还原英文（或使用全新安装）；
3. 运行 `src\extract-strings.cmd strings.tsv` 导出全部字符串（输出到 `src\strings.tsv`）；
4. 与 `translate.tsv` 对比，补充新增或变更的条目；
5. 运行 `src\apply.cmd` 验证。

### 重新编译补丁工具

```bat
src\patcher\build.cmd
```

需要 Windows 自带的 .NET Framework 4.x 编译器（`csc.exe`），产物输出到 `src\tools\`。

### 获取上游源码

```powershell
src\fetch-upstream.ps1
```

将 R4333（commit `bfb9653`）的官方源码下载到 `src\upstream-src\`（已被 `.gitignore` 忽略）。

## 已知限制

- **拼接句**：代码中用片段动态拼接的句子（如 `Cannot ...` + 变量）仍可能显示英文，出现中英混排；
- **数据文件**：动作名、物件名、分类名、扇区效果、UDMF 标志与泛化选项均已汉化；仅保留游戏/引擎专有名词与内部标识符（如 `Polyobject`、`BFG9000`、`ZDoom`）；
- **固定尺寸控件**：极少数长标签可能显示不全，可在 `translate.tsv` 中改短后重新应用；
- **系统对话框**：Windows 标准对话框按钮由系统语言决定；
- **参考手册**：`Refmanual.chm` 已整体汉化（91 个页面），但页面内的**界面截图仍是英文**，未逐张重制；
- **字体**：沿用原版 UI 字体（Microsoft Sans Serif），中文由系统字体回退渲染，未改变控件尺寸与布局。

## 许可与合规

本项目分发的是**修改后的 Ultimate Doom Builder 二进制文件**，属于 GPLv3 第 5 条所指的基于原程序的衍生作品，因此**整体以 GNU GPL v3 授权**。许可证全文见 [`LICENSE`](LICENSE)，程序目录中亦保留原始 [`app/LICENSE.txt`](app/LICENSE.txt)。

依据 GPLv3 的要求，对本程序所做的修改如下：

1. 替换程序集内的界面字符串常量（IL 中的 `ldstr` 操作数与 `[Description]` / `[Category]` / `[DisplayName]` / `[EditMode]` / `[FieldDescription]` / `[ErrorChecker]` / `[FindReplace]` 等特性中的显示文本）；
2. 替换内嵌的 `Actions.cfg`（动作/通知）与 `Hints.cfg`（模式帮助提示）文本资源；
3. 翻译 `Configurations\*.cfg` 中标志、难度、激活方式等显示名称；
4. 将 `ActionManager.LoadActions()`、`HintsManager.LoadHints()` 与 `UniversalParser.LoadConfiguration()` 中的 `Encoding.ASCII` 改为 `Encoding.UTF8`；
5. 将 `Refmanual.chm` 替换为翻译后的参考手册（正文与目录，界面截图保持原样）。

除此之外未修改任何代码逻辑。

**对应源码（Corresponding Source）：**

- 上游完整源码：Ultimate Doom Builder R4333，commit [`bfb9653`](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder/tree/bfb9653951445c7a4e4c46b1600e14358e901e44)；
- 本项目全部修改内容：本仓库 [`src/`](src/) 目录（`translate.tsv` 词典、`actions/` 资源、`patcher/` 工具源码与脚本）。

本汉化不修改上游源文件，而是对编译产物做等价替换；借助上述词典与工具，可从官方 R4333 构建完整复现本仓库中的汉化二进制。若需要打包好的上游源码副本或有其他合规疑问，请提 Issue。

本语言包不附加任何额外限制，亦不提供任何担保。再次分发时请同样保留 GPLv3 许可、原始版权声明与本说明。原始程序版权归其作者所有（CodeImp、MaxED、ZZYZX、boris、dpJudas 等，详见 `app/LICENSE.txt` 与上游仓库）。

**第三方组件：** 汉化工具使用 Mono.Cecil（MIT），其余组件随 UDB 原样分发，遵循各自许可。详见 [`NOTICE.md`](NOTICE.md)。

**商标声明：** Doom、id Software、ZeniMax 等名称与商标归其各自权利人所有。本项目不包含任何游戏本体、WAD 或商业素材。

## 上传到 GitHub

本目录已按 Git 仓库结构整理，并附带 `.gitattributes` / `.gitignore`。在仓库根目录执行：

```bat
git init
git add .
git commit -m "UDB 简体中文汉化 v1.0.0 (R4333)"
git branch -M main
git remote add origin https://github.com/<your-name>/<repo-name>.git
git push -u origin main
```


## 致谢

- [Ultimate Doom Builder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder) 及其前身 GZDoom Builder、Doom Builder 2 的全体开发者；
- [Mono.Cecil](https://github.com/jbevain/cecil)；
- Doom 社区的中文教程与术语资料。

## English Summary

An unofficial **Simplified Chinese localization** of **Ultimate Doom Builder R4333** (64-bit). The shipped assemblies are patched at the IL level — no recompilation and no gameplay logic changes. Professional terms are presented bilingually as `中文（English）` (e.g. `扇区（Sector）`), while compact labels stay Chinese-only so the original layout is preserved.

- **`app/`** — ready-to-run localized build;
- **`src/`** — translation dictionary, C# patcher sources, embedded `Actions.cfg`, backup binaries and scripts;
- Apply with `src\apply.cmd "path\to\UDB"`, restore with `src\restore.cmd`.

Licensed under **GNU GPL v3**, same as the original project. The complete list of modifications and the corresponding source are described in the [License & Compliance](#许可与合规) section. All translated text was produced by AI with human-defined terminology and light review; corrections are welcome.
