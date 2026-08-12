using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace YouTubeDownloader.Services;

public static class ThemeService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YouTubeDownloader",
        "settings.json");

        public static string Theme { get; set; } = "System";

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null && !string.IsNullOrEmpty(settings.Theme))
                {
                    Theme = settings.Theme;
                }
            }
        }
        catch
        {
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = SettingsService.Load();
            settings.Theme = Theme;
            SettingsService.Save(settings);
        }
        catch
        {
        }
    }

    public static string GetEffectiveTheme()
    {
        if (Theme == "System")
        {
            return IsSystemInDarkMode() ? "Dark" : "Light";
        }
        return Theme;
    }

    public static void ApplyTheme()
    {
        var effectiveTheme = GetEffectiveTheme();

        if (Application.Current == null)
            return;

        Application.Current.RequestedThemeVariant = effectiveTheme == "Dark"
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        if (effectiveTheme == "Dark")
        {
            SetDarkTheme();
        }
        else
        {
            SetLightTheme();
        }
    }

    private static void SetDarkTheme()
    {
        SetBrush("AppBackgroundBrush", Color.Parse("#1c1c1c"));
        SetBrush("ContentBackgroundBrush", Color.Parse("#202020"));
        SetBrush("CardBackgroundBrush", Color.Parse("#2B2B2B"));
        SetBrush("SectionCardBackgroundBrush", Color.Parse("#303030"));
        SetBrush("InnerRowBackgroundBrush", Color.Parse("#323232"));
        SetBrush("ProgressTrackBackgroundBrush", Color.Parse("#3A3A3A"));
        SetBrush("AccentBrush", Color.Parse("#0078D4"));
        SetBrush("AccentHoverBrush", Color.Parse("#106EBE"));
        SetBrush("AccentPressedBrush", Color.Parse("#005A9E"));
        SetBrush("AccentTextBrush", Color.Parse("#FFFFFF"));
        SetBrush("HeadingTextBrush", Color.Parse("#FFFFFF"));
        SetBrush("LabelTextBrush", Color.Parse("#D0D0D0"));
        SetBrush("SubtitleTextBrush", Color.Parse("#AAAAAA"));
        SetBrush("ResetButtonBrush", Color.Parse("#D32F2F"));
        SetBrush("QualityHoverBrush", Color.Parse("#383838"));
        SetBrush("QualityPressedBrush", Color.Parse("#444444"));
        SetBrush("QualitySelectedBrush", Color.Parse("#0078D4"));
        SetBrush("DialogOverlayBrush", Color.Parse("#99000000"));
    }

    private static void SetLightTheme()
    {
        SetBrush("AppBackgroundBrush", Color.Parse("#FFFFFF"));
        SetBrush("ContentBackgroundBrush", Color.Parse("#F8F8F8"));
        SetBrush("CardBackgroundBrush", Color.Parse("#FFFFFF"));
        SetBrush("SectionCardBackgroundBrush", Color.Parse("#F5F5F5"));
        SetBrush("InnerRowBackgroundBrush", Color.Parse("#F0F0F0"));
        SetBrush("ProgressTrackBackgroundBrush", Color.Parse("#E8E8E8"));
        SetBrush("AccentBrush", Color.Parse("#0078D4"));
        SetBrush("AccentHoverBrush", Color.Parse("#106EBE"));
        SetBrush("AccentPressedBrush", Color.Parse("#005A9E"));
        SetBrush("AccentTextBrush", Color.Parse("#FFFFFF"));
        SetBrush("HeadingTextBrush", Color.Parse("#1A1A1A"));
        SetBrush("LabelTextBrush", Color.Parse("#555555"));
        SetBrush("SubtitleTextBrush", Color.Parse("#666666"));
        SetBrush("ResetButtonBrush", Color.Parse("#D32F2F"));
        SetBrush("QualityHoverBrush", Color.Parse("#E8E8E8"));
        SetBrush("QualityPressedBrush", Color.Parse("#D0D0D0"));
        SetBrush("QualitySelectedBrush", Color.Parse("#0078D4"));
        SetBrush("DialogOverlayBrush", Color.Parse("#80000000"));
    }

    private static void SetBrush(string key, Color color)
    {
        if (Application.Current == null)
            return;

        object? value = null;
        Application.Current.TryGetResource(key, null, out value);
        if (value is SolidColorBrush brush)
        {
            brush.Color = color;
        }
    }

    public static bool IsSystemInDarkMode()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "defaults",
                    Arguments = "read -g AppleInterfaceStyle",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    return stdout.Contains("Dark", StringComparison.OrdinalIgnoreCase) ||
                           stderr.Contains("Dark", StringComparison.OrdinalIgnoreCase);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                var gtkTheme = Environment.GetEnvironmentVariable("GTK_THEME");
                if (!string.IsNullOrEmpty(gtkTheme))
                    return gtkTheme.Contains("dark", StringComparison.OrdinalIgnoreCase);
            }
            else if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int i) return i == 0;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        return true;
    }

    public static void CycleTheme()
    {
        Theme = Theme switch
        {
            "System" => "Dark",
            "Dark" => "Light",
            "Light" => "System",
            _ => "System"
        };
        ApplyTheme();
        Save();
    }

    public static string GetThemeDisplayName()
    {
        return Theme switch
        {
            "System" => "Системная",
            "Dark" => "Тёмная",
            "Light" => "Белая",
            _ => "Системная"
        };
    }

    public static IBrush GetBrush(string key)
    {
        if (Application.Current == null)
            return Brushes.Transparent;

        object? value = null;
        Application.Current.TryGetResource(key, null, out value);
        return value as IBrush ?? Brushes.Transparent;
    }
}
