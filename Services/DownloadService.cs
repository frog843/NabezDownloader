using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Services;

public class DownloadService
{
    private static readonly string ToolsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YouTubeDownloader",
        "tools");

    private static string YtDlpPath =>
        Path.Combine(ToolsFolder, OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp");

    private static string FFmpegPath =>
        FindFfmpegInTools();

    private static string FindFfmpegInTools()
    {
        string ffmpegName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        if (Directory.Exists(ToolsFolder))
        {
            var found = Directory.GetFiles(
                ToolsFolder,
                ffmpegName,
                SearchOption.AllDirectories
            ).FirstOrDefault();

            if (found is not null)
                return found;
        }

        return Path.Combine(ToolsFolder, ffmpegName);
    }

    private static readonly ProcessStartInfo YtDlpStartInfo = new()
    {
        FileName = YtDlpPath,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
    };

    public async Task Download(
        string url,
        string formatId,
        string outputFolder,
        bool downloadVideo,
        bool downloadAudio,
        string title,
        string audioFormat,
        string frequency,
        bool skipFormat,
        int maxHeight,
        Action<int, string> progress)
    {
        var settings = SettingsService.Load();

        string videoSelector = VideoFormatSelector(formatId, maxHeight);
        string audioSelector = AudioFormatSelector();

        if (downloadVideo && downloadAudio)
        {
            await DownloadCombined(url, videoSelector, outputFolder, title, progress);
        }
        else if (downloadVideo)
        {
            await RunYtDlp(
                url,
                videoSelector,
                outputFolder,
                progress,
                "video");
        }
        else if (downloadAudio)
        {
            if (!skipFormat && !File.Exists(FFmpegPath))
            {
                progress(0, LanguageService.Get("Status.UpdatingTools"));
                var updater = new UpdateService();
                await updater.CheckTools(
                    (p, _) => progress(p / 2, LanguageService.Get("Status.ExtractingTools")));
            }

            string audioFormatArgument = skipFormat ? audioSelector : "";

            await RunYtDlp(
                url,
                audioFormatArgument,
                outputFolder,
                progress,
                "audio");

            string[] audioFiles = Directory.GetFiles(outputFolder, "*_audio.*");
            if (audioFiles.Length == 0)
                throw new InvalidOperationException(LanguageService.Get("Message.NoAudioFilesFound"));

            string sanitizedTitle = SanitizeFileName(title);
            string outputFile = Path.Combine(outputFolder, sanitizedTitle + "." + audioFormat);

            progress(80, string.Format(LanguageService.Get("Status.ConvertingTo"), audioFormat.ToUpper()));
            await ConvertAudio(audioFiles[0], outputFile, audioFormat, frequency, progress);

            try { File.Delete(audioFiles[0]); } catch { }

            progress(100, LanguageService.Get("Status.Done"));
        }
        else
        {
            await DownloadCombined(url, videoSelector, outputFolder, title, progress);
        }
    }

    private static string VideoFormatSelector(string formatId, int maxHeight)
    {
        if (!string.IsNullOrWhiteSpace(formatId) &&
            (formatId.Contains('[') || formatId.Contains("best") || formatId.Contains('+')))
        {
            return formatId.Contains('/') ? formatId : formatId + "/best";
        }

        if (!string.IsNullOrWhiteSpace(formatId) && int.TryParse(formatId, out _))
            return formatId + "+bestaudio/best";

        if (maxHeight > 0)
            return $"bestvideo[height<={maxHeight}]+bestaudio/best[height<={maxHeight}]";

        return "best";
    }

    private static string AudioFormatSelector()
    {
        return "bestaudio/best";
    }

    private async Task DownloadCombined(
        string url,
        string formatSelector,
        string outputFolder,
        string title,
        Action<int, string> progress)
    {
        if (!File.Exists(FFmpegPath))
        {
            progress(0, LanguageService.Get("Status.UpdatingTools"));
            var updater = new UpdateService();
            await updater.CheckTools(
                (p, _) => progress(p / 2, LanguageService.Get("Status.ExtractingTools")));
        }

        string tempFolder = Path.Combine(outputFolder, "yt_temp");
        Directory.CreateDirectory(tempFolder);

        foreach (var oldFile in Directory.GetFiles(tempFolder, "temp_video.*"))
        {
            try { File.Delete(oldFile); } catch { }
        }
        foreach (var oldFile in Directory.GetFiles(tempFolder, "temp_audio.*"))
        {
            try { File.Delete(oldFile); } catch { }
        }

        progress(0, LanguageService.Get("Status.DownloadingVideo"));

        await RunYtDlp(
            url,
            formatSelector,
            tempFolder,
            (p, _) => progress(p / 2, LanguageService.Get("Status.DownloadingVideo")),
            "video",
            "temp_video.%(ext)s");

        string[] videoFiles = Directory.GetFiles(tempFolder, "temp_video.*");
        string[] audioFiles = Directory.GetFiles(tempFolder, "temp_audio.*");

        if (videoFiles.Length == 0)
            throw new InvalidOperationException(LanguageService.Get("Message.NoVideoFilesFound"));

        string sanitizedTitle = SanitizeFileName(title);
        string outputFile = Path.Combine(outputFolder, sanitizedTitle + ".mp4");

        if (audioFiles.Length == 0)
        {
            string videoTemp = videoFiles[0];
            string videoExt = Path.GetExtension(videoTemp);
            if (!videoExt.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                progress(90, LanguageService.Get("Status.Merging"));
                await RunFfmpegCopy(videoTemp, outputFile, progress);
            }
            else
            {
                progress(90, LanguageService.Get("Status.Merging"));
                File.Move(videoTemp, outputFile, true);
            }
        }
        else
        {
            string videoTemp = videoFiles[0];
            string audioTemp = audioFiles[0];

            progress(90, LanguageService.Get("Status.Merging"));
            await RunFfmpeg(videoTemp, audioTemp, outputFile, progress);
        }

        Directory.Delete(tempFolder, true);

        progress(100, LanguageService.Get("Status.Done"));
    }

    private async Task RunFfmpegCopy(string inputPath, string outputPath, Action<int, string> progress)
    {
        var info = new ProcessStartInfo
        {
            FileName = FFmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            Arguments = $"-y -i \"{inputPath}\" -c copy \"{outputPath}\""
        };

        using var process = Process.Start(info);
        if (process is null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}.");
    }

    private static string SanitizeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "video";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            title = title.Replace(c, '_');
        }

        if (title.Length > 100)
            title = title.Substring(0, 100);

        return title.Trim();
    }

    private async Task ConvertAudio(
        string inputPath,
        string outputPath,
        string audioFormat,
        string frequency,
        Action<int, string> progress)
    {
        string codec = audioFormat.ToLowerInvariant() switch
        {
            "mp3" => "libmp3lame",
            "ogg" => "libvorbis",
            "wav" => "pcm_s16le",
            "webm" => "libvorbis",
            "m4a" => "aac",
            _ => "aac"
        };

        string tempOutput = Path.Combine(
            Path.GetDirectoryName(outputPath) ?? "",
            Path.GetFileNameWithoutExtension(outputPath) + "_tmp." + audioFormat);

        var info = new ProcessStartInfo
        {
            FileName = FFmpegPath,
            Arguments =
                $"-i \"{inputPath}\" " +
                $"-vn " +
                $"-codec:a {codec} " +
                $"-ar {frequency} " +
                $"\"{tempOutput}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(info);
        if (process is null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            var match = System.Text.RegularExpressions.Regex.Match(
                e.Data,
                @"time=(\d+):(\d+):(\d+\.?\d*)");

            if (match.Success)
            {
                int h = int.Parse(match.Groups[1].Value);
                int m = int.Parse(match.Groups[2].Value);
                double s = double.Parse(match.Groups[3].Value,
                    System.Globalization.CultureInfo.InvariantCulture);

                double totalSeconds = h * 3600 + m * 60 + s;
                progress((int)(totalSeconds / 100), string.Format(LanguageService.Get("Status.ConvertingTo"), audioFormat.ToUpper()));
            }
        };

        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException("ffmpeg failed to convert audio.");

        if (File.Exists(outputPath))
            try { File.Delete(outputPath); } catch { }

        File.Move(tempOutput, outputPath);
    }

    private async Task RunFfmpeg(
        string videoPath,
        string audioPath,
        string outputPath,
        Action<int, string> progress)
    {
        var info = new ProcessStartInfo
        {
            FileName = FFmpegPath,
            Arguments =
                $"-i \"{videoPath}\" " +
                $"-i \"{audioPath}\" " +
                $"-c copy " +
                $"\"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(info);
        if (process is null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            var match = System.Text.RegularExpressions.Regex.Match(
                e.Data,
                @"time=(\d+):(\d+):(\d+\.?\d*)");

            if (match.Success)
            {
                int h = int.Parse(match.Groups[1].Value);
                int m = int.Parse(match.Groups[2].Value);
                double s = double.Parse(match.Groups[3].Value,
                    System.Globalization.CultureInfo.InvariantCulture);

                double totalSeconds = h * 3600 + m * 60 + s;
                progress((int)(totalSeconds / 100), LanguageService.Get("Status.Merging"));
            }
        };

        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException("ffmpeg failed to merge video and audio.");
    }

    private static void UpdateProgress(string line, Action<int> progress)
    {
        var match =
            System.Text.RegularExpressions.Regex.Match(
                line,
                @"(\d+(?:\.\d+)?)%");

        if (!match.Success)
            return;

        double value =
            double.Parse(
                match.Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture);

        progress((int)value);
    }

    private async Task RunYtDlp(
        string url,
        string format,
        string outputFolder,
        Action<int, string> progress,
        string type,
        string? outputTemplate = null)
    {
        var errorLines = new List<string>();

        string cookiesPath = SettingsService.Load().CookiesPath;

        string statusText = type == "video"
            ? LanguageService.Get("Status.DownloadingVideo")
            : LanguageService.Get("Status.DownloadingAudio");

        string formatArgument =
            string.IsNullOrEmpty(format)
                ? ""
                : $"-f {format} ";

        string template =
            outputTemplate ??
            $"%(title)s_{type}.%(ext)s";

        var info = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        info.Environment["PYTHONIOENCODING"] = "utf-8";
        info.Environment["PYTHONUTF8"] = "1";
        info.Environment["PATH"] =
            ToolsFolder + Path.PathSeparator +
            Environment.GetEnvironmentVariable("PATH");

        info.Arguments =
            $"--newline " +
            $"--encoding utf-8 " +
            $"--js-runtime deno " +
            $"{formatArgument}" +
            $"{(string.IsNullOrEmpty(cookiesPath) ? "" : $"--cookies \"{cookiesPath}\" ")}" +
            $"-o \"" + Path.Combine(outputFolder, template) + "\" " +
            $"\"{url}\"";

        using var process = Process.Start(info);
        if (process is null)
            throw new InvalidOperationException("Failed to start yt-dlp process.");

        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            UpdateProgress(e.Data, p => progress(p, statusText));
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            errorLines.Add(e.Data);
            UpdateProgress(e.Data, p => progress(p, statusText));
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"yt-dlp exited with code {process.ExitCode}.\n" +
                string.Join("\n", errorLines));
    }
}
