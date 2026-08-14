using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace YouTubeDownloader.Controls;

public interface IFluentStretchContent
{
    void SetViewport(double width, double height);
}

public partial class FluentDialog : UserControl
{

    public event Action? BackRequested;

    private ScaleTransform DialogScale;

    private IFluentStretchContent? _stretchContent;


    public FluentDialog()
    {
        InitializeComponent();


        DialogScale = new ScaleTransform(0.85, 0.85);


        DialogBox.RenderTransform = DialogScale;


        UpdateSize();

        SizeChanged += (_, _) => UpdateSize();

    }



    private void UpdateSize()
    {

        double w = Bounds.Width;

        double h = Bounds.Height;


        if (w <= 0 || h <= 0)
            return;


        const double standardWidth = 450;


        double width =
            Math.Min(
                w - 20,
                Math.Max(400, w * (400.0 / standardWidth)));


        DialogBox.Width = width;


        if (_stretchContent != null)
        {

            DialogBox.MaxHeight = h - 40;

            _stretchContent.SetViewport(w, h);

        }
        else
        {

            DialogBox.MaxHeight = 450;

        }

    }





    // Старый запуск (yt-dlp, ffmpeg)
    public async Task ShowAsync()
    {
        _stretchContent = null;

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

        _stretchContent = content as IFluentStretchContent;

        SetPadding(padding);

        Root.IsVisible = true;

        UpdateSize();

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