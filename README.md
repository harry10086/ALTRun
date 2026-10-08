# ALTRun v2.0 - 现代极速快速启动利器

<div align="center">

![ALTRun Logo](Res/Carracho.ico)

**专为 Windows 10 与 Windows 11 深度打造的现代化极速启动器**  
*极简键盘交互 · 沉浸 Fluent Design · 拼音首字母模糊检索 · 零依赖绿色单文件*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows)](https://www.microsoft.com/)
[![Version](https://img.shields.io/badge/Version-2.0.5-success)](#)
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Size](https://img.shields.io/badge/Size-~216KB-brightgreen)](#)

</div>

---

## 🌟 项目简介 (Overview)

**ALTRun** 是一款经典的 Windows 快速启动工具，核心宗旨是用**最少的击键次数快速启动程序或网址**。

十几年前，ALTRun 基于 Delphi 2007 诞生。随着 Windows 10 与 Windows 11 的普及，原版在现代系统上面临高分屏模糊、洋红抠图锯齿、剪贴板竞争弹窗、缺少现代命令等局限。

**ALTRun v2.0** 在完整继承原有 **极速按键轻快灵魂与全部快捷方式配置（100%无缝读取导入）** 的基础上，进行了现代重构与系统级深度集成：
- 采用 **Windows 11 Fluent Design（原生圆角、DWM 投影、Mica/亚克力半透明与沉浸式深色模式）**；
- 彻底解决 Win11 下偶发的剪贴板与 OLE 弹窗报错；
- 支持**拼音首字母与全拼模糊检索**（打 `wx` $\to$ `微信`，打 `yy` $\to$ `网易云音乐`）；
- 引入**命令行终端直接执行**（`>` 前缀）与**即时数学计算器**（`=` 前缀）；
- 完美支持 **2K / 4K 屏幕 Per-Monitor V2 矢量级高清缩放**；
- 绿色独立运行，打包产物仅 **~216 KB**，空闲常驻内存经过纯软件渲染与工作集主动修剪优化，稳定保持在 **~1 MB** 极低水平。
![altrun1](https://github.mianao.info/https://raw.githubusercontent.com/harry10086/picx-images-hosting/master/altrun/altrun1.webp)
---

## ✨ 核心特性 (Features)

### 1. 现代化视觉体验 (Win10 / Win11 Fluent & 12款精调主题)
- **DWM 原生特性**：享受 Win11 系统级圆角、立体发光投影、失焦淡出动效。
- **窗口随心拖拽移动与呼出自动复位**：支持按住顶部提示区、底部状态栏或空白边框自由拖拽移动；呼出时自动恢复屏幕黄金视线居中位置（无需记忆临时偏移坐标）。
- **窗口尺寸自由拉伸与持久记忆**：四边和四角均可自由缩放拉伸，右下角带有微质感调整手柄（ResizeGrip），尺寸调整后自动持久记忆。
- **12款设计师主题自由切换（5款暗色 + 7款亮色）**：
  - **🌙 暗色系列**：`黑曜灵动` (Raycast极客风)、`东京之夜` (经典深靛紫)、`赛博薄荷` (护眼极客黑绿)、`暖咖摩卡` (Catppuccin温润拿铁)、`经典系统` (Win11 原生深蓝)；
  - **☀️ 浅色系列**：`珍珠晨曦` (象牙雅白与天青海风)、`樱花粉雪` (浪漫柔白与樱粉高光)、`浅山抹茶` (护眼米白与草木抹茶绿)、`暖阳琥珀` (温馨羊皮纸与蜜金)、`极光白昼` (北欧冷灰雪白与极光靛)、`紫藤花语` (唯美薰衣草淡紫)、`蜜柑苏打` (元气纯白与晨曦暖橙)。
  - 点击主界面右上角 `🎨` 按钮、右键托盘图标或在快捷管理中心中即可一秒无缝热重载！
![altrun2](https://github.mianao.info/https://raw.githubusercontent.com/harry10086/picx-images-hosting/master/altrun/altrun2.webp)
- **Per-Monitor V2 DPI 高清**：在 100%、125%、150%、200% 等任意缩放比例下，文字与图标始终细腻锐利。
- **大字体舒适管理中心**：快捷方式设置窗口采用 16px 清晰大字号、48px 宽敞行高与高对比度配色规范，彻底告别眯眼看字，选中行与各列信息清晰可辨。
- **一键浏览添加程序/目录**：新增快捷项时，支持一键点击浏览选取可执行文件（`.exe`、`.lnk`）或文件夹，并自动提取名称与拼音快捷词。
- **⚡ 本机已安装软件智能扫描与首次运行向导 (v2.0.4 新增)**：
  - **首次启动向导**：全新环境无配置文件时，主程序启动自动展示极简向导卡片，一键扫描并收录常用软件（如 Chrome、微信、QQ、VSCode 等）；
  - **随时按需扫描**：在【管理快捷方式】中心或托盘右键菜单随时点击「⚡ 扫描已安装软件」，勾选新装软件一键增量合并；
  - **严苛智能降噪**：内置启发式黑名单机制，自动剔除所有卸载程序（`Uninstall`）、说明文档（`Readme`）、帮助手册（`Help`）与更新修复向导，杜绝误触卸载风险；
  - **自动推导快捷短词**：中文应用自动生成拼音缩写（微信 $\to$ `wx`，网易云 $\to$ `wyy`），英文应用智能提取首字母或常见短键（Chrome $\to$ `chrome`，VSCode $\to$ `code`），且自动去重防冲突；
  - **极致纯文本架构**：与原有快捷方式完全一致，零图标解码与额外渲染开销，常驻内存仅增加几十 KB，保持极致的秒开与轻量。
![altrun3](https://github.mianao.info/https://raw.githubusercontent.com/harry10086/picx-images-hosting/master/altrun/altrun3.webp)

### 2. 极速键盘与鼠标操作交互 (Interaction Efficiency)
- **自由定制全局热键**：支持任意组合 `Alt / Ctrl / Shift / Win + 主键`（如 `Alt+Space`、`Ctrl+Space`、`Alt+R`），在管理中心即可一键修改保存并即时生效。
- **鼠标单击即运行**：鼠标左键单击任意快捷项直接极速启动并隐藏窗口，同时支持 `Ctrl + 单击` 提权运行。
- **数字键智能动态决策**：
  - **有匹配时搜索**：当输入数字后若库中有包含该数字的快捷方式（如搜索 `12306`、`1hshutdown`），自动进入搜索过滤；
  - **无匹配时直达**：当输入的数字无匹配项时（如输入 `c` 后按 `2`，库中无 `c2` 项），自动判定为序号直达，秒级启动对应项；
  - 全面支持主键盘与右手小键盘数字键（NumPad），原生隔离中文输入法干扰。
- **Alt+数字 / Ctrl+数字 无条件直达**：支持 `Alt+1~9/0` 或 `Ctrl+1~9/0`（提权）保底直达候选序号。
- **管理员身份提权启动**：
  - `Enter` 或 `鼠标单击`：常规启动；
  - `Ctrl + Enter` 或 `Ctrl + 单击`：自动通过 UAC 提权（Run as Administrator）启动目标程序。
- **Tab 智能补全**：按 `Tab` 键将当前高亮项的快捷短语填入输入框，方便追加参数。
- **失焦自动隐藏**：点击外部区域或按下 `Esc` 键自动清空并优雅隐藏，绝不抢占工作区焦点。

### 3. 增强搜索与算法 (Smart Matching)
- **中文拼音首字母检索**：无需切换输入法，输入汉字首字母快速过滤（例如：`calc` $\to$ 计算器，`snip` $\to$ 截图工具，`jsq` $\to$ 计算器）。
- **智能频次自适应加权**：常用程序会自动提高权重，排位随使用习惯动态提前。
- **参数传递与剪贴板宏**：
  - 命令行中支持 `%p` 占位符（例如配置 `g` 对应 `https://google.com/search?q=%p`，输入 `g rust` 自动组装发起搜索）；
  - 命令行中支持 `{%c}` 或 `%c` 占位符（一键将当前剪贴板文本作为参数传递）。

### 4. 原生系统控制与窗口管理 (Native System & Window Controller)
**彻底摆脱历史包袱**，无需寻找古老第三方的 `WinCtl.exe`，全部基于 Windows 原生 Win32 API 与 Shell COM 接口重构：
- **🖥️ 极速显示桌面 (`desktop`)**：采用原生 Shell COM `ToggleDesktop()` 接口，零延迟瞬间切换显示桌面（体验完全等同于 `Win + D`）。
- **🪟 智能前台窗口控制**：呼出 ALTRun 时自动记忆上一活动窗口（`LastForegroundWindow`），直达控制前台工作区：
  - **置顶 / 取消置顶 (`top` / `untop`)**：一键锁定目标窗口为最顶层悬浮，或恢复普通层级；
  - **最小化 / 最大化 / 关闭 (`minwin` / `maxwin` / `closewin`)**：秒级调控前台窗口尺寸与关闭；
  - **关闭所有普通窗口 (`closeall`)**：安全向所有活动窗口发送关闭信号，智能避开系统托盘与桌面。
- **⚡ 原生电源与系统安全**：
  - **系统睡眠 (`sleep`)**：通过系统底层 `SetSuspendState` 触发真正的浅睡眠，彻底告别旧版误入深度休眠（Hibernate）的问题；
  - **锁定屏幕 (`lock`)**：瞬间锁定 Windows 屏幕（等同于 `Win + L`）；
  - **开箱即用运行窗口 (`r`)**：**直接输入 `r` 回车即可打开系统原生“运行 (Win+R)”对话框**，若输入 `r cmd` 则直接携带参数执行。

### 5. 现代 Windows 命令行、算式计算与宏参数
- **直接运行命令行**：输入 `>` 加任意终端命令（如 `> ping baidu.com`、`> ipconfig`），直接调起 Windows Terminal 执行并保留窗口。
- **即时计算器**：输入 `= 1024*768` 或纯算式 `(15+25)*4`，第一行候选即时显示运算结果，回车直接将结果复制到剪贴板。
- **文件拖入自动添加**：将任意文件、文件夹、`.lnk` 快捷方式拖拽至 ALTRun 搜索框，即可自动提取路径与名称创建新快捷项。
- **参数宏与剪贴板宏**：
  - 支持 `{%p}` 或 `%p` 占位符（例如配置 `g` 对应 `https://google.com/search?q=%p`，输入 `g rust` 自动组装发起搜索）；
  - 支持 `{%c}` 或 `%c` 占位符（一键将当前剪贴板文本作为参数传递）；
  - 支持 `@+`（最大化启动）、`@-`（最小化启动）、`@`（静默无黑框运行）旧版高级窗口修饰符。

---

## 🚀 开箱即用预置快捷清单 (Out-of-the-Box Presets)

> 💡 **零配置开箱即用**：下载解压后无需任何配置文件，启动即可直接使用以下全套系统、工具与网络搜索快捷方式：

### 1. 系统控制与窗口管理
| 快捷键 | 功能说明 | 对应原生指令 | 效果描述 |
| :--- | :--- | :--- | :--- |
| `desktop` | **显示桌面** | `win:desktop` | 极速切换桌面（等同 Win+D） |
| `top` | **置顶当前窗口** | `win:top` | 自动切换上一个活动窗口的置顶状态 |
| `untop` | **取消窗口置顶** | `win:untop` | 恢复窗口为常规层级 |
| `minall` | **最小化所有窗口** | `win:minall` | 瞬间最小化全部窗口（等同 Win+M） |
| `maxall` | **还原所有窗口** | `win:maxall` | 还原最小化窗口（等同 Win+Shift+M） |
| `closewin` | **关闭当前窗口** | `win:close` | 优雅关闭前台活动应用 |
| `closeall` | **关闭所有普通窗口** | `win:closeall` | 批量关闭所有正在运行的应用 |
| `lock` | **锁定屏幕** | `sys:lock` | 快速锁屏保护隐私（等同 Win+L） |
| `sleep` | **系统睡眠** | `sys:sleep` | 原生底层浅睡眠 |
| `shutdown` | **立即关机** | `shutdown.exe /s /t 0` | 系统快速关机 |
| `reboot` | **立即重启** | `shutdown.exe /r /t 0` | 系统重启 |

### 2. 核心系统工具与常用终端
| 快捷键 | 功能说明 | 对应程序/协议 | 效果描述 |
| :--- | :--- | :--- | :--- |
| `r` | **系统运行** | `{%p}` | **单独回车唤起 Win+R**，带参即直接执行 |
| `wt` | **Windows Terminal** | `wt.exe` | 调起现代终端 |
| `pwsh` | **PowerShell 终端** | `powershell.exe` | 调起 PowerShell |
| `cmd` | **命令提示符** | `cmd.exe` | 经典命令控制台 |
| `calc` | **计算器** | `calc.exe` | 启动系统计算器 |
| `snip` | **截图工具** | `ms-screenclip:` | 唤起屏幕截图与草图 |
| `task` | **任务管理器** | `taskmgr.exe` | 监控系统进程与资源占用 |
| `pad` | **记事本** | `notepad.exe` | 启动系统记事本 |
| `reg` | **注册表编辑器** | `regedit.exe` | 启动注册表管理 |
| `dev` | **设备管理器** | `devmgmt.msc` | 查看与更新硬件驱动 |
| `hosts` | **编辑 Hosts 文件** | `notepad.exe ...\hosts` | 记事本直达 hosts 配置 |

### 3. Windows 10/11 现代设置
| 快捷键 | 功能说明 | 对应协议 | 效果描述 |
| :--- | :--- | :--- | :--- |
| `set` | **Windows 设置主页** | `ms-settings:` | 打开系统设置首页 |
| `app` | **已安装应用** | `ms-settings:appsfeatures` | 快速卸载与管理应用程序 |
| `net` | **网络与 Internet** | `ms-settings:network` | 配置 Wi-Fi、以太网与代理 |
| `blue` | **蓝牙与设备** | `ms-settings:bluetooth` | 蓝牙耳机与外设快速配对 |
| `up` | **Windows 更新** | `ms-settings:windowsupdate` | 检查并安装系统补丁 |
| `vol` | **声音与音量** | `ms-settings:sound` | 调节声音设备与应用音量合成器 |

### 4. 常用 Shell 目录与控制面板
| 快捷键 | 功能说明 | 对应路径 | 效果描述 |
| :--- | :--- | :--- | :--- |
| `pc` | **此电脑** | `explorer.exe shell:::{...}` | 打开我的电脑驱动器列表 |
| `down` | **下载文件夹** | `shell:Downloads` | 直达用户个人下载目录 |
| `startup` | **系统启动目录** | `shell:startup` | 打开开机自启动快捷方式目录 |
| `recent` | **最近打开文件** | `shell:Recent` | 查看近期使用的文档和文件 |
| `control` | **传统控制面板** | `control.exe` | 打开经典控制面板 |
| `sys` | **系统高级属性** | `Sysdm.cpl` | 环境变量与高级系统设置 |

### 5. 增强搜索与剪贴板宏
| 快捷键 | 功能说明 | 对应指令 | 效果描述 |
| :--- | :--- | :--- | :--- |
| `g` | **Google 搜索** | `https://google.com/search?q=%p` | 示例：输入 `g c#` 搜索 C# |
| `b` | **百度搜索** | `https://baidu.com/s?wd=%p` | 示例：输入 `b 天气` 搜索天气 |
| `gh` | **GitHub 搜索** | `https://github.com/search?q=%p` | 示例：输入 `gh wpf` 搜索仓库 |
| `cg` | **谷歌搜索剪贴板** | `https://google.com/search?q={%c}` | 自动将当前剪切板文字送入谷歌搜索 |
| `cb` | **百度搜索剪贴板** | `https://baidu.com/s?wd={%c}` | 自动将当前剪切板文字送入百度搜索 |

---

## 📂 旧版快捷设置无缝导入与自愈升级 (Data Migration & Self-Healing)

您无需手动重新配置快捷项，ALTRun v2.0 提供 **全自动兼容与自愈机制**：

### 1. 自动检测与无感自愈
- 启动时自动检测同级目录的 `ShortCutList.txt` 或 `ShortCut.ini`；
- **智能自愈修复**：旧版配置中遗留的 `@.\WinCtl.exe MinAll` 等已失效的历史指令，会在加载时**自动无感升级为现代原生协议**；
- **智能补齐**：自动补全缺失的系统级基础快捷指令，确保功能永远完整可用。

### 2. 手动导入与拖拽
- **拖拽导入**：直接将旧版 `ShortCutList.txt` / `ShortCut.ini` 拖到 ALTRun 悬浮窗口内，程序将自动批量导入；
- **管理中心导入/导出**：右键点击系统托盘图标 $\to$ 选择 **“快捷方式管理...”**（或点击搜索框右侧齿轮 ⚙），在管理器中提供了：
  - `📥 导入列表`：支持导入旧版列表并与当前配置合并加权；
  - `📤 导出列表`：导出为标准清单，方便跨设备备份。

---

## ⌨️ 常用按键操作一览 (Shortcuts Guide)

| 按键 / 组合键 | 功能说明 |
| :--- | :--- |
| `Alt + R` | 全局唤醒 / 显示 ALTRun 主搜索框（可在管理中心随心自定义） |
| `Enter` | 启动当前高亮的快捷项 |
| `Ctrl + Enter` | **以管理员身份提权启动**当前高亮项 (Run as Administrator) |
| `1` ~ `9` / `0` | **数字键直达**：直接启动第 1 到第 10 个搜索结果，无需移动光标 |
| `Tab` | 自动补全当前选中项的快捷词至输入框，便于后续拼接参数 |
| `↑` / `↓` | 在候选列表中上下切换选中项 |
| `Esc` | 清空输入内容并隐藏主窗口 |
| `鼠标双击托盘` | 显示主窗口 |
| `右键托盘图标` | 弹出菜单（管理快捷方式、切换 12 款精美主题、开机自启、退出） |

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

## 📝 版本更新历史 (Changelog)

- **v2.0.5 (2026-10)**:
  - 🛠️ **修复已安装软件扫描复选框勾选问题**：修复扫描窗口中复选框 `IsEnabled` 逻辑反向绑定的缺陷，支持手动自由勾选/取消勾选任意新软件，并实现已存在项目的智能禁用与状态悬浮说明；
  - 🎨 **精简管理中心界面布局**：移除快捷方式管理中心右上角冗余的版本号徽章与提示，统一由窗口左上角标题栏规范展示，界面更清爽聚焦。

- **v2.0.4 (2026-10)**:
  - 🌟 **新增本机已装软件智能扫描与首次运行向导**：
    - 深入 Windows 开始菜单与桌面快捷方式，支持一键批量检索并导入常用软件（如 Chrome、微信、QQ、VSCode 等）；
    - 内置严苛的黑名单与路径降噪算法，彻底屏蔽 `C:\Windows\`（System32/SysWOW64）系统组件、Windows 管理工具、`*setup*`、`*server*`、`*deployer*`、Windows Installer 缓存及长字符串/GUID 垃圾项，规避误触与干扰；
    - 全新启动检测无配置时，自动弹出卡片式向导引导一键收录常用软件，实现零门槛开箱即用；
    - 管理中心工具栏与系统托盘菜单均新增「⚡ 扫描已安装软件」快捷入口，支持表格列宽自由拖拽调整与悬浮提示；
    - 坚持纯文本极简架构，零图片解码开销，常驻运行内存与旧版本一致，轻巧无感。
  - 🖱️ **鼠标与交互体验深度打磨**：
    - 托盘右键菜单增加子菜单斜向平滑移动防抖保护（Diagonal Movement Buffer），彻底解决鼠标斜移切入主题二级菜单时容易闪退消失的问题；
    - 快捷方式管理中心工具栏与窗口标题栏实时展示当前程序版本号；
    - 单击候选项目即刻运行，并支持 `Ctrl + 单击` 提权运行；
    - 数字键智能动态决策：有匹配项时进入搜索，无匹配项时判定为序号直达秒级启动。
  - 🪟 **窗口尺寸与布局体验优化**：
    - 解决候选列表滚动位置残留问题（重新唤醒或输入新搜索词时自动平滑复位至顶部第 1 项）；
    - 全面支持窗口随意拖拽、尺寸自由拉伸调整与窗口大小持久记忆。

---

## 📄 开源许可证 (License)

本项目遵循开源软件协议，欢迎提交 Issue 与 Pull Request 共同改进！
