namespace YouTubeDownloader.Models;

public class VideoFormat
{
    public string Id { get; set; } = "";
    public string Resolution { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Type { get; set; } = "";
    public int Fps { get; set; }
    public string Codec { get; set; } = "";
    public double Bitrate { get; set; }
    public bool IsVideo =>
        Type == "Video";


    public override string ToString()
    {
        if (Type == "Audio")
            return $"🎵 Audio {Extension}";

        return $"{Resolution} {Fps} FPS {Extension}";
    }
}