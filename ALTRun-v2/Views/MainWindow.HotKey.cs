using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ALTRun.Models;
using ALTRun.Services;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;

namespace ALTRun.Views
{
    public partial class MainWindow : Window
    {
        // 实际生效的热键文案。配置的热键被占用时会自动回退到 Alt+R，
        // 此时托盘 tooltip 与右键菜单必须显示"真正能用的"组合，否则用户会以为设置了却按不出来。
        private string _effectiveHotKeyText = string.Empty;

        public string EffectiveHotKeyText =>
            string.IsNullOrEmpty(_effectiveHotKeyText) ? App.Config.Settings.HotKey : _effectiveHotKeyText;

        private void RegisterConfiguredHotKey(IntPtr hwnd)
        {
            try
            {
                _globalHotKey = new GlobalHotKey();
                _globalHotKey.HotKeyPressed += ShowAndActivate;

                var (modifiers, key) = ParseHotKey(App.Config.Settings.HotKey);
                bool success = _globalHotKey.Register(hwnd, modifiers, key);
                if (success)
                {
                    _effectiveHotKeyText = App.Config.Settings.HotKey;
                    return;
                }

                if (modifiers != KeyModifiers.Alt || key != Key.R)
                {
                    // 配置的热键被其他程序占用：回退 Alt+R，并同步 UI 文案
                    if (_globalHotKey.Register(hwnd, KeyModifiers.Alt, Key.R))
                    {
                        _effectiveHotKeyText = FormatHotKeyText(KeyModifiers.Alt, Key.R);
                        _trayIconService?.UpdateTooltip(string.Format(AppStrings.TrayTooltipFormat, _effectiveHotKeyText));
                        AppLog.Write($"[ALTRun] 热键 {App.Config.Settings.HotKey} 注册失败，已回退 {_effectiveHotKeyText}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] MainWindow.xaml.cs:160 静默异常: {ex.Message}");
            }
        }

        public bool UpdateHotKey(KeyModifiers modifiers, Key key)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return false;

            _globalHotKey ??= new GlobalHotKey();
            _globalHotKey.HotKeyPressed -= ShowAndActivate;
            _globalHotKey.HotKeyPressed += ShowAndActivate;

            bool success = _globalHotKey.Register(hwnd, modifiers, key);
            if (success)
            {
                string hotkeyText = FormatHotKeyText(modifiers, key);
                App.Config.Settings.HotKey = hotkeyText;
                App.Config.Save();
                _effectiveHotKeyText = hotkeyText;

                _trayIconService?.UpdateTooltip(string.Format(AppStrings.TrayTooltipFormat, hotkeyText));
            }
            return success;
        }

        public static (KeyModifiers modifiers, Key key) ParseHotKey(string hotkeyStr)
        {
            KeyModifiers modifiers = KeyModifiers.None;
            Key key = Key.R;

            if (string.IsNullOrWhiteSpace(hotkeyStr))
                return (KeyModifiers.Alt, Key.R);

            var parts = hotkeyStr.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string p = part.Trim();
                if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Alt;
                else if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Control;
                else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Shift;
                else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                    modifiers |= KeyModifiers.Windows;
                else if (Enum.TryParse<Key>(p, true, out var k))
                    key = k;   // Key 枚举本身含 Space，故无需再单独判断 "Space"
            }

            if (modifiers == KeyModifiers.None) modifiers = KeyModifiers.Alt;
            return (modifiers, key);
        }

        public static string FormatHotKeyText(KeyModifiers modifiers, Key key)
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((modifiers & KeyModifiers.Control) != 0) parts.Add("Ctrl");
            if ((modifiers & KeyModifiers.Alt) != 0) parts.Add("Alt");
            if ((modifiers & KeyModifiers.Shift) != 0) parts.Add("Shift");
            if ((modifiers & KeyModifiers.Windows) != 0) parts.Add("Win");
            parts.Add(key == Key.Space ? "Space" : key.ToString());
            return string.Join(" + ", parts);
        }

    }
}
