using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ALTRun.Services
{
    public class TrayIconService : IDisposable
    {
        private NotifyIcon? _notifyIcon;

        public event Action? LeftClicked;
        public event Action? RightClicked;

        public void Initialize(IntPtr hWnd, string tooltip)
        {
            _notifyIcon = new NotifyIcon();

            // 1. 图标加载：优先从本程序 .exe 提取高清主图标，其次从 ALTRun.ico，若无则使用系统默认应用图标
            try
            {
                string exePath = Environment.ProcessPath ?? "";
                if (File.Exists(exePath))
                {
                    _notifyIcon.Icon = Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch { }

            if (_notifyIcon.Icon == null)
            {
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALTRun.ico");
                    if (File.Exists(iconPath))
                    {
                        _notifyIcon.Icon = new Icon(iconPath);
                    }
                }
                catch { }
            }

            if (_notifyIcon.Icon == null)
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            string safeTip = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            _notifyIcon.Text = safeTip;
            _notifyIcon.Visible = true;

            // 自动配置 Win11 注册表: 将托盘图标设置为始终常驻任务栏 (IsPromoted = 1)，避免被折叠收进箭头
            EnsureTrayIconPromoted();

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    LeftClicked?.Invoke();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    RightClicked?.Invoke();
                }
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                LeftClicked?.Invoke();
            };
        }

        public static void EnsureTrayIconPromoted()
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                using var notifyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true);
                if (notifyKey == null) return;

                foreach (var subKeyName in notifyKey.GetSubKeyNames())
                {
                    using var subKey = notifyKey.OpenSubKey(subKeyName, true);
                    if (subKey != null)
                    {
                        var exePath = subKey.GetValue("ExecutablePath") as string;
                        if (!string.IsNullOrEmpty(exePath) && exePath.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
                        {
                            subKey.SetValue("IsPromoted", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch { }
        }

        public void UpdateTooltip(string tooltip)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            }
        }

        public void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            // NotifyIcon handles its own message loop
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
