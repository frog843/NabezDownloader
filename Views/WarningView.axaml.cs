using Avalonia.Controls;
using Avalonia.Interactivity;
using YouTubeDownloader.Controls;

namespace YouTubeDownloader.Views;


public partial class WarningView : UserControl
{
    private FluentDialog? _dialog;
    public WarningView()
    {
        InitializeComponent();
    }

    public WarningView(FluentDialog dialog) : this()
    {
        _dialog = dialog;
    }


    private async void Ok_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }
}