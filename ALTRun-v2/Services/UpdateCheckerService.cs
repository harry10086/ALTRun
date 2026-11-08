using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;

namespace ALTRun.Services
{
    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class UpdateCheckerService
    {
        public const string RepoUrl = "https://github.com/harry10086/ALTRun";
        public const string ReleasesUrl = "https://github.com/harry10086/ALTRun/releases";
        private const string LatestApiUrl = "https://api.github.com/repos/harry10086/ALTRun/releases/latest";

        public static string CurrentVersionString
        {
            get
            {
                var ver = typeof(UpdateCheckerService).Assembly.GetName().Version;
                return ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.0.6";
            }
        }

        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            var info = new UpdateInfo
            {
                CurrentVersion = CurrentVersionString,
                ReleaseUrl = ReleasesUrl
            };

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(6);
                client.DefaultRequestHeaders.Add("User-Agent", "ALTRun-App");

                string json = await client.GetStringAsync(LatestApiUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tag_name", out var tagElem))
                {
                    string tagName = tagElem.GetString() ?? "";
                    info.LatestVersion = tagName.Trim();

                    if (root.TryGetProperty("html_url", out var urlElem))
                    {
                        info.ReleaseUrl = urlElem.GetString() ?? ReleasesUrl;
                    }

                    if (root.TryGetProperty("body", out var bodyElem))
                    {
                        info.ReleaseNotes = bodyElem.GetString() ?? "";
                    }

                    info.HasUpdate = IsNewerVersion(info.CurrentVersion, info.LatestVersion);
                }
            }
            catch (Exception ex)
            {
                info.ErrorMessage = ex.Message;
            }

            return info;
        }

        public static bool IsNewerVersion(string currentTag, string remoteTag)
        {
            try
            {
                var cur = ParseVersion(currentTag);
                var rem = ParseVersion(remoteTag);
                return rem > cur;
            }
            catch
            {
                return false;
            }
        }

        private static Version ParseVersion(string tag)
        {
            string clean = tag.Trim().TrimStart('v', 'V');
            int dashIdx = clean.IndexOf('-');
            if (dashIdx > 0) clean = clean.Substring(0, dashIdx);

            var parts = clean.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out int v0) ? v0 : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int v1) ? v1 : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out int v2) ? v2 : 0;

            return new Version(major, minor, build);
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"无法打开链接: {ex.Message}\n请手动访问: {url}", "ALTRun", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

