using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YouTubeDownloader.Models;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Views;

public partial class MainWindow : Window
{
    private VideoFormat? CurrentFormat;
    private Button? SelectedQualityButton;
    private string _currentUrl = string.Empty;
    private string _videoTitle = string.Empty;
    private bool _isUpdatingUrl;
    private string? _currentStatusKey = "Status.ReadyToDownload";
    private string? _currentTitleKey = null;
    private string _selectedAudioFormat = "mp3";
    private string _selectedFrequency = "44100";
    private bool _isPlaylist;
    private List<string> _playlistVideoUrls = new();
    private string _selectedResolution = string.Empty;

    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(
        string pszSound,
        IntPtr hmod,
        uint fdwSound);

    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_ALIAS = 0x00010000;
    private const uint SND_FILENAME = 0x00020000;

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private const uint FLASHW_TRAY = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }
#endif

    private void FlashTaskbarIcon()
    {
#if WINDOWS
        try
        {
            var hwnd = GetMainWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                var info = new FLASHWINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = hwnd,
                    dwFlags = FLASHW_TRAY,
                    uCount = 5,
                    dwTimeout = 0
                };
                FlashWindowEx(ref info);
            }
        }
        catch { }
#elif MACOS
        try
        {
            AppKit.NSApplication.SharedApplication.RequestUserAttention(
                AppKit.NSUserAttentionType.InformationalRequest);
        }
        catch { }
#endif
    }

    #if WINDOWS
    private IntPtr GetMainWindowHandle()
    {
        var handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (handle != IntPtr.Zero)
            return handle;

        handle = FindWindow(null, "Nabez Downloader");
        if (handle != IntPtr.Zero)
            return handle;

        return GetActiveWindow();
    }
#endif

#if WINDOWS
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
#endif

    private static void PlayForegroundSound()
    {
        try
        {
            var settings = SettingsService.Load();
            if (settings.CompletionSound != "default" && !string.IsNullOrEmpty(settings.CompletionSound) && File.Exists(settings.CompletionSound))
            {
                PlaySound(settings.CompletionSound, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
            }
            else
            {
                PlaySound("Foreground", IntPtr.Zero, SND_ASYNC | SND_ALIAS);
            }
        }
        catch
        {
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        StatusText.Text = LanguageService.Get(_currentStatusKey);

        DownloadVideoCheck.PropertyChanged += DownloadCheckChanged;
        DownloadAudioCheck.PropertyChanged += DownloadCheckChanged;

        AudioFormatCombo.Items.Add("mp3");
        AudioFormatCombo.Items.Add("ogg");
        AudioFormatCombo.Items.Add("wav");
        AudioFormatCombo.Items.Add("webm");
        AudioFormatCombo.Items.Add("m4a");
        AudioFormatCombo.SelectedIndex = 0;
        AudioFormatCombo.SelectionChanged += AudioFormatCombo_SelectionChanged;

        FrequencyCombo.Items.Add("44100 Hz");
        FrequencyCombo.Items.Add("48000 Hz");
        FrequencyCombo.Items.Add("96000 Hz");
        FrequencyCombo.SelectedIndex = 0;
        FrequencyCombo.SelectionChanged += FrequencyCombo_SelectionChanged;

        var savedSettings = SettingsService.Load();
        if (!string.IsNullOrEmpty(savedSettings.AudioFormat))
        {
            _selectedAudioFormat = savedSettings.AudioFormat;
            AudioFormatCombo.SelectedItem = savedSettings.AudioFormat;
        }
        if (!string.IsNullOrEmpty(savedSettings.Frequency))
        {
            _selectedFrequency = savedSettings.Frequency;
            FrequencyCombo.SelectedItem = savedSettings.Frequency + " Hz";
        }

        LanguageService.LanguageChanged += UpdateButtonFontSizes;
        LanguageService.LanguageChanged += UpdateLocalizedTexts;
        UpdateButtonFontSizes();

        Opened += async (_, _) =>
        {
            var settings = SettingsService.Load();

            if (!settings.FirstRunCompleted)
            {
                Dialog.IsVisible = true;

                await Dialog.ShowAsync();

                var updater = new UpdateService();

                await updater.CheckTools(
                    (percent, text) =>
                    {
                        Dialog.SetProgress(
                            percent,
                            text);
                    });

                settings.FirstRunCompleted = true;
                SettingsService.Save(settings);

                await Dialog.CloseAsync();
            }

            await CheckForAppUpdate();
        };
    }

    private async Task CheckForAppUpdate()
    {
        try
        {
            var updater = new GithubUpdateService();
            var info = await updater.CheckForUpdateAsync();

            if (!info.IsAvailable)
                return;

            var updateView = new UpdateView(Dialog);
            updateView.SetUpdate(info);

            await Dialog.ShowAsync(
                LanguageService.Get("Update.Available"),
                updateView);
        }
        catch
        {
        }
    }

    private void AudioFormatCombo_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (AudioFormatCombo.SelectedItem is string format)
        {
            _selectedAudioFormat = format;
            var settings = SettingsService.Load();
            settings.AudioFormat = format;
            SettingsService.Save(settings);
        }
    }

    private void FrequencyCombo_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (FrequencyCombo.SelectedItem is string freq)
        {
            _selectedFrequency = freq.Split(' ')[0];
            var settings = SettingsService.Load();
            settings.Frequency = _selectedFrequency;
            SettingsService.Save(settings);
        }
    }

    private async void OpenSettings(
        object? sender,
        RoutedEventArgs e)
    {
        await Dialog.ShowAsync(
            LanguageService.Get("Settings.Title"),
            new SettingsView(Dialog)
        );
    }

    private async void ChangeDownloadFolder(
        object? sender,
        RoutedEventArgs e)
    {
        var folders =
            await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = LanguageService.Get("FolderPicker.Title"),
                    AllowMultiple = false
                });

        if (folders.Count > 0)
        {
            var settings = SettingsService.Load();
            settings.DownloadPath = folders[0].Path.LocalPath;
            SettingsService.Save(settings);
        }
    }

    private void OpenDownloadFolder(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var settings = SettingsService.Load();
            if (!string.IsNullOrEmpty(settings.DownloadPath) && Directory.Exists(settings.DownloadPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = settings.DownloadPath,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private async void YoutubeUrl_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_isUpdatingUrl)
            return;

        _isUpdatingUrl = true;

        try
        {
            if (string.IsNullOrWhiteSpace(YoutubeUrl!.Text))
            {
                _currentUrl = string.Empty;
                _isPlaylist = false;
                _playlistVideoUrls.Clear();
                VideoTitleText.Text = string.Empty;
                PlaylistIndicator.Text = string.Empty;
                _currentTitleKey = null;
                StatusText.Text = string.Empty;
                _currentStatusKey = null;
                QualityList.Children.Clear();
                return;
            }

            if ((YoutubeUrl!.Text).StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
            {
                _currentUrl = "https://" + YoutubeUrl.Text["https://www.".Length..];
                YoutubeUrl.Text = _currentUrl["https://".Length..];
                YoutubeUrl.CaretIndex = YoutubeUrl.Text.Length;
            }
            else if ((YoutubeUrl!.Text).StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _currentUrl = YoutubeUrl.Text;
                YoutubeUrl.Text = _currentUrl["https://".Length..];
                YoutubeUrl.CaretIndex = YoutubeUrl.Text.Length;
            }
            else
            {
                _currentUrl = "https://" + YoutubeUrl.Text;
            }

            _isPlaylist = IsPlaylistUrl(_currentUrl);

            if (_isPlaylist)
            {
                PlaylistIndicator.Text = LanguageService.Get("Message.PlaylistDetected");
            }
            else
            {
                PlaylistIndicator.Text = string.Empty;
            }
        }
        finally
        {
            _isUpdatingUrl = false;
        }
    }

    private async void CheckQuality_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentUrl))
            return;

        try
        {
            var service = new YtDlpService();
            var settings = SettingsService.Load();

            string title;
            string formats;

            if (_isPlaylist)
            {
                _playlistVideoUrls = await service.GetPlaylistVideoUrls(_currentUrl);

                if (_playlistVideoUrls.Count == 0)
                {
                    SetStatus("Status.Error");
                    await Dialog.ShowAsync(
                        LanguageService.Get("Error.Title"),
                        new ErrorView(Dialog, "No videos found in playlist."));
                    return;
                }

                if (settings.SkipTitleCheck)
                {
                    title = ExtractVideoId(_playlistVideoUrls[0]);
                    SetTitle("Status.GettingFormats");
                    formats = await service.GetFormats(_playlistVideoUrls[0]);
                }
                else
                {
                    SetTitle("Status.GettingTitle");
                    var result = await service.GetTitleAndFormats(_playlistVideoUrls[0]);
                    title = result.Title;
                    formats = result.Formats;
                }
            }
            else
            {
                if (settings.SkipTitleCheck)
                {
                    title = ExtractVideoId(_currentUrl);
                    SetTitle("Status.GettingFormats");
                    formats = await service.GetFormats(_currentUrl);
                }
                else
                {
                    SetTitle("Status.GettingTitle");
                    var result = await service.GetTitleAndFormats(_currentUrl);
                    title = result.Title;
                    formats = result.Formats;
                }
            }

            _videoTitle = title;

            VideoTitleText.Text = _videoTitle;
            _currentTitleKey = null;

            var qualities = FormatParser.Parse(formats)
                .OrderByDescending(x => GetHeight(x.Resolution))
                .ToList();

            UpdateQualityList(qualities);

            if (_isPlaylist)
            {
                PlaylistIndicator.Text = $"{LanguageService.Get("Playlist.Videos")}: {_playlistVideoUrls.Count}";
            }
        }
        catch (Exception ex)
        {
            _currentTitleKey = null;
            VideoTitleText.Text = string.Empty;
            SetStatus("Status.Error");
            string message = ex.Message;
            if (message.Contains("cookies", StringComparison.OrdinalIgnoreCase))
            {
                message = LanguageService.Get("Message.CookiesMissing");
            }
            await Dialog.ShowAsync(
                LanguageService.Get("Error.Title"),
                new ErrorView(Dialog, message));
        }
    }

    private void UpdateQualityList(List<VideoFormat> formats)
    {
        QualityList.Children.Clear();

        foreach (var format in formats)
        {
            if (format.Extension != "mp4")
                continue;

            var button = new Button
            {
                Background = ThemeService.GetBrush("CardBackgroundBrush"),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(15),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            var grid = new Grid();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Star });

            var radio = new RadioButton
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                GroupName = "Quality",
                IsChecked = false
            };

            Grid.SetColumn(radio, 0);
            grid.Children.Add(radio);

            var stack = new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(12, 0, 0, 0)
            };

            var title = new TextBlock
            {
                Text = format.Resolution,
                Foreground = ThemeService.GetBrush("HeadingTextBrush"),
                FontSize = 16
            };

            var subtitle = new TextBlock
            {
                Text = format.Codec is "Unknown"
                    ? $"{format.Fps} FPS • {format.Bitrate:F0} Kbps"
                    : $"{format.Codec.ToUpper()} • {format.Fps} FPS • {format.Bitrate:F0} Kbps",
                Foreground = ThemeService.GetBrush("SubtitleTextBrush"),
                FontSize = 13
            };

            stack.Children.Add(title);
            stack.Children.Add(subtitle);

            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);

            button.Content = grid;

            button.Click += (_, _) =>
            {
                if (SelectedQualityButton != null)
                {
                    SelectedQualityButton.Classes.Remove("selected");
                }

                SelectedQualityButton = button;
                button.Classes.Add("selected");

                CurrentFormat = format;
                _selectedResolution = format.Resolution;
                radio.IsChecked = true;
            };

            QualityList.Children.Add(button);
        }
    }

    private int GetHeight(string resolution)
    {
        if (resolution.EndsWith("p"))
        {
            string number = resolution.Replace("p", "");
            if (int.TryParse(number, out int height))
            {
                return height;
            }
        }

        return 0;
    }

    private async void Download_Click(
        object? sender,
        RoutedEventArgs e)
    {
        SetStatus("Status.ReadyToDownload");
        ProgressText.Text = "0%";
        DownloadProgress.Value = 0;

        if (string.IsNullOrWhiteSpace(_currentUrl))
            return;

        var settings = SettingsService.Load();
        var downloader = new DownloadService();

        bool video = DownloadVideoCheck.IsChecked == true;
        bool audio = DownloadAudioCheck.IsChecked == true;

        if (_isPlaylist && _playlistVideoUrls.Count > 0)
        {
            try
            {
                await DownloadPlaylistAsync(
                    _playlistVideoUrls,
                    _selectedResolution,
                    settings,
                    video,
                    audio);
                SetStatus("Status.Done");
            }
            catch (Exception ex)
            {
                _currentTitleKey = null;
                VideoTitleText.Text = string.Empty;
                SetStatus("Status.Error");
                string message = ex.Message;
                if (message.Contains("cookies", StringComparison.OrdinalIgnoreCase))
                {
                    message = LanguageService.Get("Message.CookiesMissing");
                }
                await Dialog.ShowAsync(
                    LanguageService.Get("Error.Title"),
                    new ErrorView(Dialog, message));
            }
        }
        else
        {
            if (CurrentFormat == null)
            {
                await Dialog.ShowAsync(
                    LanguageService.Get("Error.Title"),
                    new ErrorView(Dialog, LanguageService.Get("Message.FormatNotSelected")));
                return;
            }

            try
            {
                await downloader.Download(
                    _currentUrl,
                    CurrentFormat?.Id ?? "best",
                    settings.DownloadPath,
                    video,
                    audio,
                    _videoTitle,
                    _selectedAudioFormat,
                    _selectedFrequency,
                    settings.SkipFormat,
                    ParseHeight(CurrentFormat?.Resolution),
                    (percent, statusText) =>
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            DownloadProgress.Value = percent;
                            ProgressText.Text = percent + "%";
                            StatusText.Text = statusText;
                            _currentStatusKey = null;
                        });
                    });
                SetStatus("Status.Done");
                PlayForegroundSound();
                FlashTaskbarIcon();
            }
            catch (Exception ex)
            {
                _currentTitleKey = null;
                VideoTitleText.Text = string.Empty;
                SetStatus("Status.Error");
                string message = ex.Message;
                if (message.Contains("cookies", StringComparison.OrdinalIgnoreCase))
                {
                    message = LanguageService.Get("Message.CookiesMissing");
                }
                await Dialog.ShowAsync(
                    LanguageService.Get("Error.Title"),
                    new ErrorView(Dialog, message));
            }
        }
    }

    private async Task DownloadPlaylistAsync(
        List<string> videoUrls,
        string resolution,
        Settings settings,
        bool downloadVideo,
        bool downloadAudio)
    {
        var ytDlp = new YtDlpService();
        var downloader = new DownloadService();

        int total = videoUrls.Count;

        for (int i = 0; i < total; i++)
        {
            string videoUrl = videoUrls[i];
            int currentIndex = i + 1;

            Dispatcher.UIThread.Invoke(() =>
            {
                StatusText.Text = string.Format(
                    LanguageService.Get("Status.DownloadingVideoOfPlaylist"),
                    currentIndex, total);
                _currentStatusKey = null;
            });

            string title;
            string formatId;

            try
            {
                SetTitle("Status.GettingTitle");
                var formatResult = await ytDlp.GetTitleAndFormats(videoUrl);
                title = formatResult.Title;
                string formatsOutput = formatResult.Formats;

                formatId = FindFormatIdByResolution(formatsOutput, resolution);

                if (string.IsNullOrEmpty(formatId))
                {
                    formatId = "best";
                }
            }
            catch
            {
                title = Path.GetFileName(videoUrl);
                formatId = "best";
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                VideoTitleText.Text = title;
                _currentTitleKey = null;
            });

            await downloader.Download(
                videoUrl,
                formatId,
                settings.DownloadPath,
                downloadVideo,
                downloadAudio,
                title,
                _selectedAudioFormat,
                _selectedFrequency,
                settings.SkipFormat,
                ParseHeight(resolution),
                (percent, statusText) =>
                {
                    double baseProgress = (double)i / total * 100;
                    double videoProgress = percent / 100.0 * (100.0 / total);
                    int totalProgress = (int)(baseProgress + videoProgress);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        DownloadProgress.Value = totalProgress;
                        ProgressText.Text = totalProgress + "%";
                        StatusText.Text = statusText;
                        _currentStatusKey = null;
                    });
                });
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            StatusText.Text = LanguageService.Get("Message.PlaylistDownloadComplete");
            ProgressText.Text = "100%";
            DownloadProgress.Value = 100;
            PlayForegroundSound();
            FlashTaskbarIcon();
        });
    }

    private static string ExtractVideoId(string url)
    {
        try
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath;

            if (path.StartsWith("/watch", StringComparison.OrdinalIgnoreCase))
            {
                string query = uri.Query;
                int idx = query.IndexOf("v=", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string value = query.Substring(idx + 2);
                    int amp = value.IndexOf('&');
                    if (amp >= 0)
                        value = value.Substring(0, amp);
                    return value;
                }
            }

            if (path.StartsWith("/shorts/", StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring("/shorts/".Length).TrimEnd('/');
            }
        }
        catch
        {
        }

        return url;
    }

    private static bool IsPlaylistUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath.ToLowerInvariant();
            string query = uri.Query.ToLowerInvariant();

            if (path.Contains("/playlist"))
                return true;

            if (query.Contains("list="))
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string FindFormatIdByResolution(string formatsOutput, string targetResolution)
    {
        int targetHeight = ParseHeight(targetResolution);

        if (targetHeight <= 0)
            return "best";

        var formats = FormatParser.Parse(formatsOutput);

        var match = formats
            .Where(f => ParseHeight(f.Resolution) == targetHeight)
            .OrderByDescending(f => f.Fps)
            .ThenByDescending(f => f.Bitrate)
            .FirstOrDefault();

        if (match is not null)
            return match.Id;

        return $"bestvideo[height<={targetHeight}]";
    }

    private static int ParseHeight(string? resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            return 0;

        var trimmed = resolution.Trim();
        if (trimmed.EndsWith("p", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^1];

        return int.TryParse(trimmed, out int h) ? h : 0;
    }

    private async void DownloadCheckChanged(
        object? sender,
        AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(CheckBox.IsChecked))
            return;

        bool isAudio = DownloadAudioCheck.IsChecked == true;
        AudioFormatRow.IsVisible = isAudio;
        FrequencyRow.IsVisible = isAudio;

        if (DownloadVideoCheck.IsChecked == true &&
            DownloadAudioCheck.IsChecked == true)
        {
            await Dialog.ShowAsync(
                LanguageService.Get("Warning.Title"),
                new WarningView(Dialog),
                15);
        }
    }

    private void UpdateButtonFontSizes()
    {
        if (LanguageService.GetEffectiveLanguage() == "English")
        {
            CheckQualityButton.FontSize = 16;
            DownloadButton.FontSize = 18;
        }
        else
        {
            CheckQualityButton.FontSize = 14;
            DownloadButton.FontSize = 24;
        }
    }

    private void SetStatus(string key)
    {
        _currentStatusKey = key;
        StatusText.Text = LanguageService.Get(key);
    }

    private void SetTitle(string key)
    {
        _currentTitleKey = key;
        VideoTitleText.Text = LanguageService.Get(key);
    }

    private void UpdateLocalizedTexts()
    {
        if (_currentStatusKey != null)
            StatusText.Text = LanguageService.Get(_currentStatusKey);
        if (_currentTitleKey != null)
            VideoTitleText.Text = LanguageService.Get(_currentTitleKey);
        UpdateButtonFontSizes();
    }
}


