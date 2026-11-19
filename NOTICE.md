# 版权与第三方声明 / Notices

本仓库包含对 **Ultimate Doom Builder** 的汉化修改，以及用于生成这些修改的工具与数据。

## 1. Ultimate Doom Builder

- 项目主页：<https://github.com/UltimateDoomBuilder/UltimateDoomBuilder>
- 本仓库基于版本：R4333（3.0.0.4333），commit `bfb9653951445c7a4e4c46b1600e14358e901e44`
- 许可：GNU General Public License v3.0（见 [`LICENSE`](LICENSE) 与 [`app/LICENSE.txt`](app/LICENSE.txt)）
- 版权归 Ultimate Doom Builder 及其前身项目的贡献者所有，包括但不限于 CodeImp、MaxED、ZZYZX、boris、dpJudas。

`app/` 目录中的程序是修改后的二进制分发版本，依据 GPLv3 以相同许可再分发。修改清单与对应源码说明见根目录 README 的「许可与合规」一节。

修改内容包括：程序集界面字符串、内嵌 `Actions.cfg` 与 `Hints.cfg`、`Configurations/*.cfg` 中的标志/难度/激活方式显示名称、参考手册 `Refmanual.chm`，以及三处文本解码方式（ASCII → UTF-8）。

## 2. 汉化内容

`src/translate.tsv`、`src/config-translate.tsv`、`src/actions/`、`src/help/`、`src/patcher/`、`src/*.ps1`、`src/*.cmd` 中的汉化数据与工具代码，作为上述衍生作品的一部分，同样以 **GPL-3.0** 授权。

译文由 AI 生成，人工制定术语表并做校验；不对译文准确性作任何担保。

## 3. 第三方组件

| 组件 | 用途 | 许可 |
|---|---|---|
| [Mono.Cecil](https://github.com/jbevain/cecil) 0.11.5 | 读写 .NET 程序集（汉化工具） | MIT，见 [`src/tools/Mono.Cecil.LICENSE.txt`](src/tools/Mono.Cecil.LICENSE.txt) |
| ScintillaNET | UDB 脚本编辑器组件（随原程序分发） | MIT |
| SharpCompress | UDB 压缩包支持（随原程序分发） | MIT |
| System.Buffers / System.Memory / System.Numerics.Vectors / System.Runtime.CompilerServices.Unsafe | .NET 运行时依赖（随原程序分发） | MIT |
| TabControlEX | UDB 标签控件（随原程序分发） | 见上游项目说明 |

其余随 UDB 分发的组件遵循其各自许可；本仓库未对它们做额外修改。

## 4. 商标

Doom、id Software、ZeniMax、Bethesda 等名称与商标归其各自权利人所有。本项目为非官方社区项目，与上述公司及 Ultimate Doom Builder 官方无隶属或背书关系，且不包含任何游戏本体、WAD 或商业素材。
