using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using YouTubeDownloader.Services;
using YouTubeDownloader.Views; //zizzi

namespace YouTubeDownloader;

public partial class App : Application
{

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public override void OnFrameworkInitializationCompleted()
    {
        ThemeService.Load();
        ThemeService.ApplyTheme();

        LanguageService.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {

            desktop.MainWindow = new MainWindow();

        }


        base.OnFrameworkInitializationCompleted();

    }

}