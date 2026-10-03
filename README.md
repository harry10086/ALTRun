# ALTRun v2.0 - 现代极速快速启动利器

<div align="center">

![ALTRun Logo](Res/Carracho.ico)

**专为 Windows 10 与 Windows 11 深度打造的现代化极速启动器**  
*极简键盘交互 · 沉浸 Fluent Design · 拼音首字母模糊检索 · 零依赖绿色单文件*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows)](https://www.microsoft.com/)
[![Version](https://img.shields.io/badge/Version-2.0.0-success)](#)
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Size](https://img.shields.io/badge/Size-~216KB-brightgreen)](#)

</div>

---

## 🌟 项目简介 (Overview)

**ALTRun** 是一款经典的 Windows 快速启动工具，核心宗旨是用**最少的击键次数快速启动程序或网址**。

十几年前，ALTRun 基于 Delphi 2007 诞生。随着 Windows 10 与 Windows 11 的普及，原版在现代系统上面临高分屏模糊、洋红抠图锯齿、剪贴板竞争弹窗、缺少现代命令等局限。

**ALTRun v2.0** 在完整继承原有**极速、按键轻快灵魂与全部快捷方式配置（100% 无缝读取导入）**的基础上，进行了现代重构与系统级深度集成：
- 采用 **Windows 11 Fluent Design（原生圆角、DWM 投影、Mica/亚克力半透明与沉浸式深色模式）**；
- 彻底解决 Win11 下偶发的剪贴板与 OLE 弹窗报错；
- 支持**拼音首字母与全拼模糊检索**（打 `wx` $\to$ `微信`，打 `yy` $\to$ `网易云音乐`）；
- 引入**命令行终端直接执行**（`>` 前缀）与**即时数学计算器**（`=` 前缀）；
- 完美支持 **2K / 4K 屏幕 Per-Monitor V2 矢量级高清缩放**；
- 绿色独立运行，打包产物仅 **~216 KB**，空闲常驻内存经过纯软件渲染与工作集主动修剪优化，稳定保持在 **~3.8 MB** 极低水平。

---

## ✨ 核心特性 (Features)

### 1. 现代化视觉体验 (Win10 / Win11 Fluent & 12款精调主题)
- **DWM 原生特性**：享受 Win11 系统级圆角、立体发光投影、失焦淡出动效。
- **12款设计师主题自由切换（5款暗色 + 7款亮色）**：
  - **🌙 暗色系列**：`黑曜灵动` (Raycast极客风)、`东京之夜` (经典深靛紫)、`赛博薄荷` (护眼极客黑绿)、`暖咖摩卡` (Catppuccin温润拿铁)、`经典系统` (Win11 原生深蓝)；
  - **☀️ 浅色系列**：`珍珠晨曦` (象牙雅白与天青海风)、`樱花粉雪` (浪漫柔白与樱粉高光)、`浅山抹茶` (护眼米白与草木抹茶绿)、`暖阳琥珀` (温馨羊皮纸与蜜金)、`极光白昼` (北欧冷灰雪白与极光靛)、`紫藤花语` (唯美薰衣草淡紫)、`蜜柑苏打` (元气纯白与晨曦暖橙)。
  - 点击主界面右上角 `🎨` 按钮、右键托盘图标或在快捷管理中心中即可一秒无缝热重载！
- **Per-Monitor V2 DPI 高清**：在 100%、125%、150%、200% 等任意缩放比例下，文字与图标始终细腻锐利。
- **大字体舒适管理中心**：快捷方式设置窗口采用 16px 清晰大字号、48px 宽敞行高与高对比度配色规范，彻底告别眯眼看字，选中行与各列信息清晰可辨。
- **一键浏览添加程序/目录**：新增快捷项时，支持一键点击浏览选取可执行文件（`.exe`、`.lnk`）或文件夹，并自动提取名称与拼音快捷词。

### 2. 极速键盘操作与交互 (Keystroke Efficiency)
- **自由定制全局热键**：支持任意组合 `Alt / Ctrl / Shift / Win + 主键`（如 `Alt+Space`、`Ctrl+Space`、`Alt+R`），在管理中心即可一键修改保存并即时生效。
- **数字键直达 (0~9)**：候选结果左侧清晰标注 `1` 到 `0` 序号，按对应数字键直接触发，无需手动回车。
- **管理员身份提权启动**：
  - `Enter`：常规启动；
  - `Ctrl + Enter`：自动通过 UAC 提权（Run as Administrator）启动目标程序。
- **Tab 智能补全**：按 `Tab` 键将当前高亮项的快捷短语填入输入框，方便追加参数。
- **失焦自动隐藏**：点击外部区域或按下 `Esc` 键自动清空并优雅隐藏，绝不抢占工作区焦点。

### 3. 增强搜索与算法 (Smart Matching)
- **中文拼音首字母检索**：无需切换输入法，输入汉字首字母快速过滤（例如：`calc` $\to$ 计算器，`snip` $\to$ 截图工具，`jsq` $\to$ 计算器）。
- **智能频次自适应加权**：常用程序会自动提高权重，排位随使用习惯动态提前。
- **参数传递与剪贴板宏**：
  - 命令行中支持 `%p` 占位符（例如配置 `g` 对应 `https://google.com/search?q=%p`，输入 `g rust` 自动组装发起搜索）；
  - 命令行中支持 `{%c}` 或 `%c` 占位符（一键将当前剪贴板文本作为参数传递）。

### 4. 现代 Windows 命令行与系统工具直达
- **直接运行命令行**：输入 `>` 加任意终端命令（如 `> ping baidu.com`、`> ipconfig`），直接调起 Windows Terminal 执行并保留窗口。
- **即时计算器**：输入 `= 1024*768` 或纯算式 `(15+25)*4`，第一行候选即时显示运算结果，回车直接将结果复制到剪贴板。
- **文件拖入自动添加**：将任意文件、文件夹、`.lnk` 快捷方式拖拽至 ALTRun 搜索框，即可自动提取路径与名称创建新快捷项。

---

## 🚀 预置现代快捷项清单 (Modern Presets)

系统开箱即用集成以下高频命令，无需手动配置：

| 快捷输入 | 名称 / 目标 | 核心说明 |
| :--- | :--- | :--- |
| `wt` | Windows Terminal 终端 | 启动新一代 Windows 终端 |
| `pwsh` | PowerShell 终端 | 启动现代 PowerShell |
| `cmd` | 命令提示符 | 经典 CMD 控制台 |
| `snip` | 截图工具 (`ms-screenclip:`) | 唤起现代矩形/自由截图 |
| `calc` | 计算器 | 启动 Windows 计算器 |
| `task` | 任务管理器 | 启动任务管理器 |
| `set` | Windows 设置主页 | 现代系统设置面板 |
| `app` | 已安装应用 (`ms-settings:appsfeatures`) | 快速卸载/管理应用程序 |
| `net` | 网络与 Internet 设置 | 现代网络与代理配置 |
| `blue` | 蓝牙与外设连接 | 蓝牙设备快速配对 |
| `up` | Windows 更新检查 | 检查并安装系统更新 |
| `vol` | 声音与音量混合器 | 音量合成器调节 |
| `hosts`| 编辑 Hosts 文件 | 记事本快速打开系统 hosts |
| `g` | Google 搜索 (`%p`) | 浏览器发起谷歌查询 |
| `b` | 百度搜索 (`%p`) | 浏览器发起百度查询 |
| `cb` | 百度搜索剪贴板内容 (`{%c}`) | 自动读取剪贴板并发起搜索 |

---

## 📂 旧版快捷设置无缝导入机制 (Data Migration)

您无需手动重新配置快捷项，ALTRun v2.0 原生提供 **100% 兼容方案**：

### 1. 自动检测与导入
- 启动时，ALTRun 会**自动检测**同级目录、父级目录或 `Bin\` 目录下的原版 `ShortCut.ini`；
- 只要检测到旧版配置，会自动将其中的所有快捷词、名称、命令行、参数类型、使用频次无缝导入，并生成现代 `ALTRun_Config.json`。

### 2. 手动导入与拖拽
- **拖拽导入**：直接将旧版 `ShortCut.ini` 拖到 ALTRun 悬浮窗口内，程序将自动批量导入；
- **管理器导入/导出**：右键点击系统托盘图标 $\to$ 选择 **“快捷方式管理...”**（或点击搜索框右侧齿轮 ⚙），在管理器中提供了：
  - `📥 导入旧版 ShortCut`
  - `📤 导出为 ShortCut`（支持将修改同步回原版文件，方便多端备份）。

---

## ⌨️ 常用按键操作一览 (Shortcuts Guide)

| 按键 / 组合键 | 功能说明 |
| :--- | :--- |
| `Alt + R` | 全局唤醒 / 显示 ALTRun 主搜索框（可自定义） |
| `Enter` | 启动当前高亮的快捷项 |
| `Ctrl + Enter` | **以管理员身份提权启动**当前高亮项 (Run as Administrator) |
| `1` ~ `9` / `0` | **数字键直达**：直接启动第 1 到第 10 个搜索结果 |
| `Tab` | 补全当前选中项的快捷词至输入框 |
| `↑` / `↓` | 在候选列表中上下切换选中项 |
| `Esc` | 清空输入内容并隐藏主窗口 |
| `鼠标双击托盘` | 显示主窗口 |
| `右键托盘图标` | 弹出菜单（打开快捷方式管理器、切换开机自启动、退出程序） |

---

## 🛠️ 开发与编译构建流程 (Development & Build)

### 1. 环境准备
- 操作系统：Windows 10 (1903+) 或 Windows 11
- 运行时：[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本

> 💡 **提示**：本项目完全支持免提权部署的便携式 .NET 8 SDK。

### 2. 目录结构
```text
ALTRun/
├── ALTRun-v2/              # v2.0 现代核心工程源码
│   ├── Models/             # 数据实体定义 (ShortCutItem 等)
│   ├── Services/           # 核心服务 (SearchEngine, PinyinHelper, CommandExecutor, DwmHelper)
│   ├── Views/              # 现代 Fluent UI 窗体 (MainWindow, ManageWindow)
│   ├── App.xaml / .cs      # 应用程序入口、托盘图标、单实例互斥锁
│   └── ALTRun.csproj       # 项目工程文件
├── Bin-v2/                 # v2.0 编译产物发布目录 (ALTRun.exe 绿色免安装)
├── Bin/                    # v1.0 历史 Delphi 归档
└── README.md               # 项目使用与开发文档
```

### 3. 本地调试与运行
在仓库根目录下执行：
```powershell
# 运行调试版本
dotnet run --project ALTRun-v2/ALTRun.csproj
```

### 4. 发布单文件绿色版 (Single-File Release)
执行以下命令，即可生成独立的单文件绿色可执行程序：
```powershell
dotnet publish ALTRun-v2/ALTRun.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o Bin-v2
```
构建完成后，在 `Bin-v2/` 目录下将生成纯绿色的 `ALTRun.exe`（单文件仅 ~216 KB），拷贝到任意目录直接运行即可！

---

## 🛡️ 架构与稳定性优化（Win11 报错根除）

针对旧版在 Windows 11 下偶尔弹出错误框的问题，v2.0 做了根本性重构：
1. **剪贴板并发防护**：旧版一次获取失败便抛出 `Cannot open clipboard`；新版内置并发重试等待机制（5 次重试保护），彻底杜绝与 Win11 云剪贴板 (`Win+V`) 的锁竞争冲突。
2. **现代化 COM 与 Shell 托管调用**：规避了旧版 `ResolveLink` 中未处理的 `OleCheck` 崩溃风险，支持 Win11 复杂的现代应用命名空间与特殊协议。
3. **UPI 特权隔离解除**：弃用旧版写死在 Manifest 中的 `requireAdministrator`，改用标准的 `asInvoker` 普通权限运行，完美支持从资源管理器拖入文件，仅在用户按下 `Ctrl+Enter` 时按需提权。

---

## 📄 开源许可证 (License)

本项目遵循开源软件协议，欢迎提交 Issue 与 Pull Request 共同改进！
