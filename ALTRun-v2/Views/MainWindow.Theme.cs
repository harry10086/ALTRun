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
        private MenuItem CreateThemeMenuItem(ThemeDefinition th, string currentThemeId, double fontSize = 13.5)
        {
            var itemTh = new MenuItem
            {
                IsChecked = th.Id.Equals(currentThemeId, StringComparison.OrdinalIgnoreCase),
                FontSize = fontSize
            };

            var panel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 原生矢量真实彩色圆点 (杜绝黑白 Emoji 与字体排版错位)
            var circle = new System.Windows.Shapes.Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = new SolidColorBrush(th.AccentColor),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0x88, 0x88, 0x88)),
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var tbName = new TextBlock
            {
                Text = th.Name,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            tbName.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            var tbDesc = new TextBlock
            {
                Text = $"({th.Description})",
                FontSize = fontSize - 1.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            tbDesc.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

            panel.Children.Add(circle);
            panel.Children.Add(tbName);
            panel.Children.Add(tbDesc);

            itemTh.Header = panel;
            itemTh.Click += (s, ev) => SetTheme(th.Id);
            return itemTh;
        }

        public void SetTheme(string themeId)
        {
            var theme = ThemeManager.GetTheme(themeId);
            App.Config.Settings.Theme = theme.Id;
            App.Config.Settings.DarkMode = theme.IsDark;
            App.Config.Save();
            ApplyTheme(theme);
        }

        public void ToggleTheme()
        {
            var allThemes = ThemeManager.Themes;
            var current = ThemeManager.GetTheme(App.Config.Settings.Theme, App.Config.Settings.DarkMode);
            int idx = allThemes.FindIndex(x => x.Id == current.Id);
            int nextIdx = (idx + 1) % allThemes.Count;
            SetTheme(allThemes[nextIdx].Id);
        }

        public void ApplyTheme(ThemeDefinition theme)
        {
            ThemeManager.ApplyTheme(this, theme);
            ThemeManager.ApplyThemeToAll(theme);
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            string currentThemeId = App.Config.Settings.Theme;

            var darkHeader = new MenuItem { Header = AppStrings.MenuDarkHeader, IsEnabled = false, FontSize = 12 };
            menu.Items.Add(darkHeader);
            foreach (var th in ThemeManager.Themes.Where(t => t.IsDark))
            {
                menu.Items.Add(CreateThemeMenuItem(th, currentThemeId, 13.5));
            }

            menu.Items.Add(new Separator());

            var lightHeader = new MenuItem { Header = AppStrings.MenuLightHeader, IsEnabled = false, FontSize = 12 };
            menu.Items.Add(lightHeader);
            foreach (var th in ThemeManager.Themes.Where(t => !t.IsDark))
            {
                menu.Items.Add(CreateThemeMenuItem(th, currentThemeId, 13.5));
            }

            menu.PlacementTarget = BtnTheme;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

    }
}
