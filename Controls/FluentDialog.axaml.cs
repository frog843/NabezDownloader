using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace YouTubeDownloader.Controls;

public partial class FluentDialog : UserControl
{

    public event Action? BackRequested;

    private ScaleTransform DialogScale;


    public FluentDialog()
    {
        InitializeComponent();


        DialogScale = new ScaleTransform(0.85, 0.85);


        DialogBox.RenderTransform = DialogScale;

    }





    // Старый запуск (yt-dlp, ffmpeg)
    public async Task ShowAsync()
    {
        Root.IsVisible = true;


        await OpenAnimation();
    }





    // Новый универсальный режим
    public async Task ShowAsync(
        string title,
        Control content,
        double padding = 30)
    {

        Title.Text = title;

        DialogContent.Content = content;


        SetPadding(padding);


        Root.IsVisible = true;


        await OpenAnimation();

    }





    private async Task OpenAnimation()
    {

        for (double i = 0; i <= 1; i += 0.05)
        {

            double ease =
                1 - Math.Pow(1 - i, 3);


            Root.Opacity = ease;


            DialogScale.ScaleX =
                0.85 + (0.15 * ease);


            DialogScale.ScaleY =
                0.85 + (0.15 * ease);


            await Task.Delay(10);

        }

    }





    public async Task CloseAsync()
    {

        for (double i = 0; i <= 1; i += 0.05)
        {

            double ease =
                i * i;


            Root.Opacity =
                1 - ease;


            DialogScale.ScaleX =
                1 - (0.08 * ease);


            DialogScale.ScaleY =
                1 - (0.08 * ease);


            await Task.Delay(10);

        }


        Root.IsVisible = false;

        Progress.IsVisible = false;
        Status.IsVisible = false;

    }





    public void SetProgress(int value, string text)
    {
        Progress.IsVisible = true;
        Status.IsVisible = true;

        Progress.Value = value;
        Status.Text = text;
    }
    public void RequestClose()
    {
        BackRequested?.Invoke();
    }
    public Thickness DialogPadding
    {
        get;
        set;
    } = new Thickness(30);
    public void SetPadding(double value)
    {
        DialogBox.Padding = new Thickness(value);
    }
}