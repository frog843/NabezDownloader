using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using YouTubeDownloader.Controls;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Views;

public partial class UpdateView : UserControl
{
    private FluentDialog? _dialog;
    private string _htmlUrl = "";
    private string _downloadUrl = "";

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
        var url = !string.IsNullOrWhiteSpace(_downloadUrl)
            ? _downloadUrl
            : _htmlUrl;

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        if (_dialog is not null)
            await _dialog.CloseAsync();
    }

    private async void Later_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }
}
