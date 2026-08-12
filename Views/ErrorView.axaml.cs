using Avalonia.Controls;
using Avalonia.Interactivity;
using YouTubeDownloader.Controls;

namespace YouTubeDownloader.Views;


public partial class ErrorView : UserControl
{
    private FluentDialog? _dialog;
    public ErrorView()
    {
        InitializeComponent();
    }

    public ErrorView(FluentDialog dialog, string message) : this()
    {
        _dialog = dialog;
        ErrorMessage.Text = message;
    }


    private async void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }
}