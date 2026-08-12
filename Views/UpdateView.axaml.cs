using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using YouTubeDownloader.Controls;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Views;

public partial class UpdateView : UserControl
{
    private FluentDialog? _dialog;
    private string _htmlUrl = "";
    private string _downloadUrl = "";
    private bool _installing;

    public UpdateView()
    {
        InitializeComponent();
    }

    public UpdateView(FluentDialog dialog) : this()
    {
        _dialog = dialog;
    }

    public void SetUpdate(
        GithubUpdateService.UpdateInfo info)
    {
        _htmlUrl = info.HtmlUrl;
        _downloadUrl = info.DownloadUrl;

        VersionInfo.Text = string.Format(
            LanguageService.Get("Update.VersionInfo"),
            info.CurrentVersion,
            info.LatestVersion);

        Notes.Text = string.IsNullOrWhiteSpace(info.ReleaseNotes)
            ? LanguageService.Get("Update.NoNotes")
            : info.ReleaseNotes;
    }

    private async void Download_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_installing)
            return;

        var url = !string.IsNullOrWhiteSpace(_downloadUrl)
            ? _downloadUrl
            : _htmlUrl;

        if (string.IsNullOrWhiteSpace(url))
            return;

        _installing = true;

        Buttons.IsVisible = false;
        Progress.IsVisible = true;
        Status.IsVisible = true;

        try
        {
            await UpdateInstaller.DownloadAndInstall(
                url,
                (percent, text) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Progress.Value = percent;
                        Status.Text = text;
                    });
                });

            // Process exits inside installer; this return is only on failure.
        }
        catch (Exception ex)
        {
            _installing = false;
            Buttons.IsVisible = true;
            Progress.IsVisible = false;
            Status.IsVisible = false;
            Status.Text = ex.Message;
        }
    }

    private async void Later_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }
}
