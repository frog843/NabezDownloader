using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using YouTubeDownloader.Controls;
using YouTubeDownloader.Services;
using System;
using System.IO;


namespace YouTubeDownloader.Views;


public partial class SettingsView : UserControl
{

    private FluentDialog? _dialog;

    private Settings settings;

    private string ShortenPath(string text)
    {
        if (text.Length <= 33)
            return text;

        return text.Substring(0, 33) + "...";
    }


    public SettingsView()
    {
        InitializeComponent();
        settings = SettingsService.Load();
        SetVersionText();
    }

    private void SetVersionText()
    {
        try
        {
            var version = typeof(GithubUpdateService)
                .Assembly
                .GetName()
                .Version;

            VersionText.Text = version != null
                ? version.ToString()
                : "1.0.0";
        }
        catch
        {
            VersionText.Text = "1.0.0";
        }
    }

    public SettingsView(FluentDialog dialog) : this()
    {
        _dialog = dialog;

        LanguageComboBox.SelectedIndex = LanguageService.Language switch
        {
            "Russian" => 1,
            "English" => 2,
            _ => 0
        };

        LanguageComboBox.SelectionChanged += (_, _) =>
        {
            LanguageService.Language = LanguageComboBox.SelectedIndex switch
            {
                1 => "Russian",
                2 => "English",
                _ => "System"
            };

            LanguageService.ApplyLanguage();
            LanguageService.Save();
        };

        if (!string.IsNullOrEmpty(settings.DownloadPath))
        {
            FolderButton.Content =
                ShortenPath(settings.DownloadPath);
        }


        if (!string.IsNullOrEmpty(settings.CookiesPath))
        {
            CookiesButton.Content =
                ShortenPath(settings.CookiesPath);
        }


        SkipTitleCheck.IsChecked = settings.SkipTitleCheck;

        SkipFormatCheck.IsChecked = settings.SkipFormat;


        ThemeComboBox.SelectedIndex = ThemeService.Theme switch
        {
            "System" => 0,
            "Dark" => 1,
            "Light" => 2,
            _ => 0
        };

        ThemeComboBox.SelectionChanged += (_, _) =>
        {
            ThemeService.Theme = ThemeComboBox.SelectedIndex switch
            {
                1 => "Dark",
                2 => "Light",
                _ => "System"
            };

            ThemeService.ApplyTheme();
            ThemeService.Save();
        };


        SoundButton.Content = settings.CompletionSound == "default"
            ? LanguageService.Get("Button.SelectSound")
            : ShortenPath(settings.CompletionSound);


        SoundButton.Click += async (_, _) =>
        {
            var files =
                await TopLevel.GetTopLevel(this)!
                .StorageProvider
                .OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = LanguageService.Get("SoundPicker.Title"),
                        AllowMultiple = false,

                        FileTypeFilter =
                        [
                            new FilePickerFileType(LanguageService.Get("SoundPicker.Filter"))
                {
                    Patterns = ["*.wav"]
                }
                        ]
                    });


            if (files.Count > 0)
            {
                settings.CompletionSound =
                    files[0].Path.LocalPath;

                SoundButton.Content =
                    ShortenPath(settings.CompletionSound);

                SettingsService.Save(settings);
            }
        };


        BackButton.Click += async (_, _) =>
        {
            settings.SkipTitleCheck = SkipTitleCheck.IsChecked == true;
            settings.SkipFormat = SkipFormatCheck.IsChecked == true;

            SettingsService.Save(settings);

            if (_dialog is not null)
                await _dialog.CloseAsync();
        };

        ResetButton.Click += async (_, _) =>
        {
            var content = new StackPanel { Spacing = 12 };
/*
            var title = new TextBlock
            {
                Text = "Предупреждение",
                FontSize = 26,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            }; */

            var message = new TextBlock
            {
                Text = LanguageService.Get("Message.ResetConfirm"),
                Foreground = ThemeService.GetBrush("SubtitleTextBrush"),
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 350,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var yesButton = new Button
            {
                Content = LanguageService.Get("Button.Yes"),
                Background = ThemeService.GetBrush("ResetButtonBrush"),
                Foreground = ThemeService.GetBrush("AccentTextBrush"),
                CornerRadius = new CornerRadius(8),
                Width = 80,
                HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var noButton = new Button
            {
                Content = LanguageService.Get("Button.No"),
                Background = ThemeService.GetBrush("CardBackgroundBrush"),
                Foreground = ThemeService.GetBrush("HeadingTextBrush"),
                CornerRadius = new CornerRadius(8),
                Width = 80,
                HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            yesButton.Click += async (_, __) =>
            {
                if (_dialog is not null)
                    await _dialog.CloseAsync();

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "YouTubeDownloader");
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);

                Environment.Exit(0);
            };

            noButton.Click += async (_, __) =>
            {
                if (_dialog is not null)
                    await _dialog.CloseAsync();
            };

            buttons.Children.Add(yesButton);
            buttons.Children.Add(noButton);

           // content.Children.Add(title);
            content.Children.Add(message);
            content.Children.Add(buttons);

            if (_dialog is not null)
                await _dialog.ShowAsync(LanguageService.Get("Warning.Title"), content);
        };

        FolderButton.Click += async (_, _) =>
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
                settings.DownloadPath =
                    folders[0].Path.LocalPath;


                FolderButton.Content =
                    ShortenPath(settings.DownloadPath);


                SettingsService.Save(settings);
            }
        };



        CookiesButton.Click += async (_, _) =>
        {
            var files =
                await TopLevel.GetTopLevel(this)!
                .StorageProvider
                .OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = LanguageService.Get("CookiesPicker.Title"),
                        AllowMultiple = false,

                        FileTypeFilter =
                        [
                            new FilePickerFileType(LanguageService.Get("CookiesPicker.Filter"))
                {
                    Patterns = ["*.txt"]
                }
                        ]
                    });


            if (files.Count > 0)
            {
                settings.CookiesPath =
                    files[0].Path.LocalPath;


                SettingsService.Save(settings);


                CookiesButton.Content =
                    ShortenPath(settings.CookiesPath);
            }
        };

    }

}
