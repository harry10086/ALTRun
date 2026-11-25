namespace ALTRun.Services
{
    /// <summary>
    /// 集中管理用户可见的 UI 文本（菜单标题、MessageBox 标题/正文、按钮、状态提示等）。
    /// 内部数据（内置快捷项名称、扫描黑名单、拼音表）与调试日志不属于 UI 文本，仍保留在各自代码中。
    /// 修改提示语只需改此处，无需在多个窗口间来回查找。
    /// </summary>
    public static class AppStrings
    {
        // ── 通用 ──────────────────────────────────────────────
        public const string AppName = "ALTRun";
        public const string TipTitle = "ALTRun 提示";
        public const string MsgBoxTitleHint = "提示";
        public const string MsgBoxTitleError = "错误";
        public const string OkButton = "确定";

        // ── 托盘菜单 ──────────────────────────────────────────
        public const string MenuShowSearchFormat = "显示搜索框 ({0})";
        public const string MenuManage = "快捷方式管理...";
        public const string MenuScan = "扫描已安装软件...";
        public const string MenuTheme = "外观主题风格";
        public const string MenuDarkHeader = "── 🌙 暗色系列 ──";
        public const string MenuLightHeader = "── ☀️ 浅色系列 ──";
        public const string MenuAutoRun = "开机自启动";
        public const string MenuCheckUpdate = "检查更新...";
        public const string MenuExit = "退出 ALTRun";
        public const string ContextAddNew = "添加新条目";
        public const string ContextEdit = "编辑本条目";
        public const string ContextOpenFolder = "打开目标程序目录";

        // ── 更新检查 ──────────────────────────────────────────
        public const string UpdateTitleNewVersion = "ALTRun 发现新版本";
        public const string UpdateTitleSoftware = "ALTRun 软件更新提示";
        public const string UpdateTitleVersion = "ALTRun 版本与更新";
        public const string UpdateAskDownload = "是否立即前往 GitHub 查看更新日志并下载安装？";
        public const string UpdateAskPage = "是否立即前往 GitHub 页面查看更新日志并下载安装？";
        public const string UpdateAskOpenRepo = "是否打开 GitHub 仓库项目主页？";
        public const string UpdateErrorPrefix = "检查更新出错: ";
        public const string UpdateLatest = "您当前使用的是最新版本，暂无可用更新。";
        public const string UpdateNoticePrefix = "检查更新出现提示: ";
        public const string UpdateBtnChecking = "⏳ 检查中...";
        public const string UpdateBtnDefault = "🔄 检查更新";
        // 三处更新弹窗（后台静默检查 / 托盘菜单 / 管理中心）统一正文，避免同一件事出现多种说法
        public const string UpdateNewVersionBodyFormat = "🎉 发现新版本！\n\n当前版本: {0}\n最新版本: {1}\n\n" + UpdateAskDownload;
        public const string UpdateLatestBodyFormat = "ℹ️ ALTRun 当前已是最新版本 ({0})\n\nGitHub 开源库: {1}\n\n" + UpdateAskOpenRepo;
        public const string UpdateLatestWithNoticeBodyFormat = "ℹ️ ALTRun 版本信息\n\n当前运行版本: {0}\nGitHub 开源库: {1}\n\n{2}\n\n" + UpdateAskPage;
        public const string OpenLinkFailedFormat = "无法打开链接: {0}\n请手动访问: {1}";
        public const string CrashMessageFormat = "程序发生异常: {0}\n{1}";

        // ── 管理窗口 ──────────────────────────────────────────
        public const string ManageTitleFormat = "ALTRun {0} - 快捷方式管理中心";
        public const string ManageTitleFallback = "ALTRun v2.0.8 - 快捷方式管理中心";
        public const string NewShortCutName = "新快捷方式";
        public const string SavedMessage = "快捷项修改已保存！";
        public const string AddedMessage = "已新增快捷方式！";
        public const string DeleteConfirmFormat = "确定要删除快捷项 [{0}] 吗？";
        public const string DeleteConfirmTitle = "确认删除";
        public const string HotKeyNeedModifier = "请至少勾选一个修饰键（例如 Alt 或 Ctrl）！";
        public const string HotKeyNeedMainKey = "请选择主按键！";
        public const string HotKeyUpdatedFormat = "全局唤醒热键已成功更新为: {0}";
        public const string HotKeyUpdatedTitle = "设置成功";
        public const string HotKeyConflict = "热键注册失败！该按键组合可能已被系统或其他运行中的软件占用，请尝试其他组合（例如 Alt+Space 或 Ctrl+Shift+Space）。";
        public const string HotKeyConflictTitle = "热键冲突";

        // ── 导入 / 导出 ───────────────────────────────────────
        public const string ImportSuccessFormat = "成功导入 {0} 个快捷方式！";
        public const string ImportSelectedSuccessFormat = "成功导入 {0} 个软件快捷方式！\n现在您可以直接按唤醒热键 (Alt+R) 输入快捷词或软件名称快速启动了。";
        public const string ImportSelectedSuccessTitle = "导入成功";
        public const string WizardImportSuccessFormat = "初始化导入完成！已成功收录 {0} 个软件快捷方式。";
        public const string ImportTitle = "导入完成";
        public const string ImportEmpty = "未能从文件中解析出有效的快捷方式项，请确保文件包含有效内容。";
        public const string ImportFailPrefix = "导入失败: ";
        public const string ExportSuccessFormat = "已成功导出到: {0}";
        public const string ExportTitle = "导出完成";
        public const string ExportFailPrefix = "导出失败: ";
        public const string SelectProgramTitle = "选择要启动的程序、快捷方式或文件";
        public const string SelectFolderTitle = "选择要快速打开的文件夹目录";
        public const string SelectLegacyTitle = "选择原版 ShortCutList.txt / FavoriteList.txt / INI 文件";
        public const string ExportLegacyTitle = "导出为原版 ShortCutList.txt";
        public const string AlreadyImported = "选中的软件均已存在于快捷列表中，无需重复导入。";
        public const string PleaseSelectSoftware = "请先勾选需要导入的软件！";
        public const string ScanStatusFormat = "共扫描到 {0} 个应用 (新发现 {1} 个)，已勾选 {2} 个待导入";
        public const string ImportSelectedButtonFormat = "✓ 导入勾选软件 ({0})";

        // ── 文件对话框过滤器 ──────────────────────────────────
        public const string ProgramFileFilter = "所有支持的文件 (*.exe;*.lnk;*.bat;*.cmd;*.*)|*.exe;*.lnk;*.bat;*.cmd;*.*|应用程序 (*.exe)|*.exe|桌面快捷方式 (*.lnk)|*.lnk|批处理脚本 (*.bat;*.cmd)|*.bat;*.cmd|所有文件 (*.*)|*.*";
        public const string LegacyImportFilter = "ALTRun 列表文件 (ShortCutList.txt;FavoriteList.txt;*.txt;*.ini)|ShortCutList.txt;FavoriteList.txt;*.txt;*.ini|文本文件 (*.txt)|*.txt|INI 配置文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";
        public const string LegacyExportFilter = "ALTRun 列表文件 (*.txt)|*.txt|INI 配置文件 (*.ini)|*.ini";

        // ── 托盘图标 Tooltip ──────────────────────────────────
        // 版本号直接取程序集版本，避免像硬编码 "v2.0" 那样与 csproj 的 2.0.8 对不上
        public static readonly string TrayTooltipFormat =
            $"ALTRun v{typeof(AppStrings).Assembly.GetName().Version?.ToString(3) ?? "2.0"} - 快速启动 ({{0}})";

        // ── 文件未找到 ────────────────────────────────────────
        public const string FileNotFound = "未找到该文件，请检查";

        // ── 危险系统指令的二次确认 ────────────────────────────
        public const string CloseAllConfirmTitle = "确认关闭全部窗口";
        public const string CloseAllConfirmMessage = "即将向所有可见的普通窗口发送关闭请求（等同逐个点右上角 ×）。\n\n未保存的 Word / Excel 等文档可能因此丢失，是否继续？";

        // ── 配置读取 ──────────────────────────────────────────
        public const string ConfigCorruptTitle = "配置文件读取失败";
        public const string ConfigCorruptMessageFormat = "配置文件内容已损坏（可能因断电或杀毒软件中断写盘），为避免覆盖您的数据，原文件已备份为：\n\n{0}\n\n本次已载入内置默认快捷方式。若需恢复原有内容，可手工修复上述备份文件后改名回 ALTRun_Config.json。";

        // ── 右键菜单：打开目标目录 ────────────────────────────
        public const string OpenFolderFailedFormat = "未能定位到有效的目标目录！\n\n命令行: {0}\n工作目录: {1}";

        // ── 主窗口（MainWindow.xaml）──────────────────────────
        public const string SearchPlaceholder = "输入快捷键 / 拼音首字母 / >命令 / =算式...";
        public const string TooltipThemeMenu = "选择外观主题风格";
        public const string TooltipManage = "快捷方式管理";
        public const string HintRun = "↵ / 单击 启动";
        public const string HintRunAsAdmin = "Ctrl+↵ 提权运行";
        public const string HintNumberKey = "0~9 键直达";
        public const string HintTabComplete = "Tab 补全";
        public const string HintContextMenuDir = "Ctrl+D / 右键 目录";
        public const string HintEscHide = "Esc 隐藏";

        // ── 欢迎向导（WelcomeWizardWindow）─────────────────────
        public static readonly string WizardHeader =
            $"欢迎使用 ALTRun {(typeof(AppStrings).Assembly.GetName().Version is { } v ? v.ToString(2) : "2.0")}";
        public const string WizardSlogan = "极简、极速、无感常驻的经典键盘快捷启动器";
        public const string WizardIntro = "ALTRun 可以自动扫描您电脑上已安装的常用应用（如 Chrome、微信、QQ、VSCode 等），并自动生成快捷拼音缩写，让您开箱即可输入关键词快速启动。";
        public const string WizardFeatureFiltering = "✓ 智能过滤机制：自动剔除所有卸载程序、帮助手册与更新向导";
        public const string WizardFeatureLightweight = "✓ 极致纯文本轻量：不提取多余图片，零内存常驻负担";
        public const string WizardFeatureManageable = "✓ 随时可管：以后可在管理中心随时重新扫描或自定义修改";
        public const string WizardScanning = "⏳ 正在扫描并为您导入常用软件，请稍候...";
        public const string WizardAutoImportButton = "⚡ 自动扫描并导入常用软件";
        public const string WizardSkipButton = "暂不导入";
        public const string WizardReadyTitle = "ALTRun 准备就绪";
        public const string WizardReadySuffix = "\n按下 Alt+R 唤醒 ALTRun 即可开始体验！";

        // ── 快捷方式管理窗口（ManageWindow.xaml）───────────────
        public const string ManageWindowTitle = "ALTRun - 快捷方式管理中心";
        public const string BtnAddNew = "新建项";
        public const string BtnScanApps = "扫描已安装软件";
        public const string BtnDeleteSelected = "删除选中";
        public const string BtnImportList = "导入列表";
        public const string BtnExportList = "导出列表";
        public const string BtnToggleTheme = "快速换主题";
        public const string BtnCheckUpdateShort = "检查更新";
        public const string TooltipAddNew = "新增一个快捷项目";
        public const string TooltipScanApps = "自动检索本机开始菜单与桌面软件，智能过滤并一键批量导入";
        public const string TooltipDeleteSelected = "删除表格中当前选中的快捷方式";
        public const string TooltipImportList = "支持导入 ShortCutList.txt / FavoriteList.txt / INI 等所有格式文件";
        public const string TooltipExportList = "导出为标准 ShortCutList.txt";
        public const string TooltipToggleTheme = "在预置的高质感主题间快速循环切换";
        public const string TooltipCheckUpdate = "检查 GitHub 最新版本与发布动态";
        public const string LabelHotKey = "⌨ 唤醒热键:";
        public const string LabelMainKey = "主键:";
        public const string BtnSaveHotKey = "💾 保存热键";
        public const string TooltipSaveHotKey = "更新全局唤醒热键并立即生效";
        public const string TooltipThemeCombo = "切换主界面与管理器外观配色风格";
        public const string GridHeaderShortCut = "快捷词";
        public const string GridHeaderName = "名称 / 说明";
        public const string GridHeaderCommandLine = "命令行 / 目标路径";
        public const string GridHeaderFreq = "频次";
        public const string LabelShortCut = "快捷词:";
        public const string LabelName = "名 称:";
        public const string LabelCommandLine = "命令行:";
        public const string BtnBrowseFile = "📄 选择文件...";
        public const string BtnBrowseFolder = "📁 选择目录...";
        public const string TooltipBrowseFile = "直接浏览并选取可执行程序(.exe)、桌面快捷方式(.lnk)或任意文档";
        public const string TooltipBrowseFolder = "选取常用文件夹目录";
        public const string BtnSaveEdit = "✔ 保存修改";

        // ── 软件扫描窗口（ScanAppsWindow.xaml）────────────────
        public const string ScanWindowTitle = "ALTRun - 扫描并导入本机已安装软件";
        public const string ScanHeader = "⚡ 本机已安装软件智能扫描";
        public const string ScanDescription = "已自动从开始菜单与桌面检索，并智能过滤卸载程序、帮助手册及辅助工具。您可以勾选想要导入的软件并直接修改快捷词。";
        public const string BtnSelectAllNew = "全选新软件";
        public const string BtnDeselectAll = "全部取消";
        public const string ScanScanning = "⏳ 正在扫描本机应用与快捷方式，请稍候...";
        public const string GridHeaderImport = "导入";
        public const string GridHeaderShortKey = "快捷词 (按键)";
        public const string GridHeaderAppName = "软件名称";
        public const string GridHeaderStatus = "状态";
        public const string GridHeaderPath = "可执行文件路径";
        public const string ScanFooterDefault = "共发现 0 个软件，已勾选 0 个";
        public const string BtnImportSelected = "✓ 导入勾选软件";
        public const string BtnClose = "关闭";
    }
}
