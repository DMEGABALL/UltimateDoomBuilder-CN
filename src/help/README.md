# 参考手册汉化 / Reference Manual Localization

本目录是 **Ultimate Doom Builder 参考手册**（`Refmanual.chm`）的汉化工程。

## 目录

```
help/
├─ en/                      从原版 Refmanual.chm 解包的英文页面（未修改，作对照）
├─ zh/                      简体中文页面（工作副本，可直接编辑）
│  ├─ Refmanual.hhp         编译工程文件
│  ├─ Contents.hhc          目录树（已汉化，GBK 编码，供 hhc 读取）
│  ├─ Index.hhk             索引（原版为空）
│  ├─ *.html、gzdb\...      正文页面
│  └─ Refmanual.chm         编译产物（apply.cmd 会把它复制到 app\）
├─ build.cmd                编译入口（调用 build.ps1）
└─ build.ps1                定位 hhc.exe 并编译
```

## 编译

需要 [HTML Help Workshop](https://www.microsoft.com/en-us/download/details.aspx?id=21138)（提供 `hhc.exe`）：

```bat
help\build.cmd
```

脚本会自动从注册表 / 常见安装路径定位 `hhc.exe`，输出 `zh\Refmanual.chm`。
也可以直接复制到程序目录：

```powershell
powershell -ExecutionPolicy Bypass -File help\build.ps1 -CopyTo ..\..\app
```

## 修改译文

- 正文：直接编辑 `zh\` 下对应 `.html`（UTF-8，`<meta charset="utf-8">`）；
- 目录：编辑 `zh\Contents.hhc` 中 `<param name="Name" value="...">` 的名称。
  **该文件必须以系统 ANSI 编码（简体中文为 GBK/936）保存**，否则编译出的目录会乱码；
- 修改后重新运行 `build.cmd`，再运行 `src\apply.cmd` 把新 CHM 安装到 `app\`。

## 说明

- 页面内的截图仍是英文界面截图，未重制；
- 原版 `Refmanual.chm` 已备份在 `src\backup\Refmanual.chm`，`restore.cmd` 会一并还原；
- 手册内容版权归 Ultimate Doom Builder 及其前身项目（GZDoom Builder、Doom Builder）作者所有，本汉化同样以 GPL-3.0 分发。
