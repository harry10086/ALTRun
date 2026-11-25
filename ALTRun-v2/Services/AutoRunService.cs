using System;
using System.IO;
using Microsoft.Win32;

namespace ALTRun.Services
{
    public static class AutoRunService
    {
        private const string AppName = "ALTRun";
        private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static string StartupShortcutPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{AppName}.lnk");

        public static void ApplyAutoRun(bool enable)
        {
            string exePath = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return;
            }

            // 1. 注册表启动项 (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
            // 规范化添加 --silent 参数，确保开机静默常驻托盘，即使被系统多次拉起也不会误唤醒主界面
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key != null)
                {
                    string desiredValue = $"\"{exePath}\" --silent";
                    string? currentValue = key.GetValue(AppName) as string;

                    if (enable)
                    {
                        // 值没变就不写：避免每次开机启动都做一次无谓的注册表写入（也会引起杀软/系统提示）
                        if (!string.Equals(currentValue, desiredValue, StringComparison.Ordinal))
                        {
                            key.SetValue(AppName, desiredValue);
                        }
                    }
                    else if (currentValue != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] AutoRunService.cs:40 静默异常: {ex.Message}");
            }

            // 2. 清理历史遗留的 Windows 用户启动文件夹 (shell:startup) 快捷方式
            // 避免注册表与启动文件夹同时存在造成双重自启动冲突
            try
            {
                string shortcutPath = StartupShortcutPath;
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"[ALTRun] AutoRunService.cs:52 静默异常: {ex.Message}");
            }
        }

        public static bool CheckStatus()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
