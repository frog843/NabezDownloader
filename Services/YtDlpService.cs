using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Services;

public class YtDlpService
{
    private static readonly string ToolsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YouTubeDownloader",
        "tools");

    private string YtDlpPath =>
        Path.Combine(ToolsFolder, OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp");

    private static string GetCookiesArgument()
    {
        var settings = SettingsService.Load();
        return string.IsNullOrEmpty(settings.CookiesPath)
            ? string.Empty
            : $"--cookies \"{settings.CookiesPath}\"";
    }

    private ProcessStartInfo BuildProcessStartInfo(string arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        info.Environment["PYTHONIOENCODING"] = "utf-8";
        info.Environment["PYTHONUTF8"] = "1";

        return info;
    }

    private static async Task<string> ReadProcessOutputAsync(Process process)
    {
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"yt-dlp error: {error}");

        return output;
    }

    public async Task<(string Title, string Formats)> GetTitleAndFormats(string url)
    {
        string cookiesArgument = GetCookiesArgument();
        string arguments =
            $"--js-runtime deno --encoding utf-8 {cookiesArgument} " +
            $"--no-warnings \"{url}\" " +
            $"--list-formats " +
            $"--print \"---YTD_SEP---\" " +
            $"--print \"%(title)s\"";

        using var process = Process.Start(BuildProcessStartInfo(arguments));
        if (process is null)
            throw new InvalidOperationException("Failed to start yt-dlp process.");

        string fullOutput = await ReadProcessOutputAsync(process);
        return ParseCombinedOutput(fullOutput);
    }

    public async Task<string> GetTitle(string url)
    {
        (string title, _) = await GetTitleAndFormats(url);
        return title;
    }

    public async Task<string> GetFormats(string url)
    {
        (_, string formats) = await GetTitleAndFormats(url);
        return formats;
    }

    private static (string Title, string Formats) ParseCombinedOutput(string fullOutput)
    {
        const string sep = "---YTD_SEP---";

        int sepIndex = fullOutput.IndexOf(sep, StringComparison.Ordinal);
        if (sepIndex < 0)
            return (string.Empty, string.Empty);

        string formatsBlock = fullOutput[..sepIndex];
        string titleBlock = fullOutput[(sepIndex + sep.Length)..];

        string formats = ExtractFormatsText(formatsBlock);
        string title = titleBlock.Split('\n')
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("["))
            ?.Trim() ?? string.Empty;

        return (title, formats);
    }

    private static string ExtractFormatsText(string formatsBlock)
    {
        if (string.IsNullOrWhiteSpace(formatsBlock))
            return string.Empty;

        var lines = formatsBlock.Split('\n');
        var sb = new StringBuilder();
        bool dataStarted = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!dataStarted)
            {
                if (line.TrimStart().StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                    dataStarted = true;
                continue;
            }

            string[] parts = line.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
                continue;

            if (!int.TryParse(parts[0], out _))
                continue;

            string id = parts[0];
            string ext = parts[1];
            string resolution = parts[2];
            string fps = parts[3];

            bool isAudio = resolution.Equals("audio only", StringComparison.OrdinalIgnoreCase);

            if (isAudio)
                continue;

            double bitrate = 0;
            foreach (var p in parts)
            {
                if (p.EndsWith("k") &&
                    double.TryParse(
                        p[..^1],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double kbps))
                {
                    bitrate = kbps;
                    break;
                }
            }

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append($"{id}|{ext}|{resolution}|{fps}|{bitrate}");
        }

        return sb.ToString();
    }

    public async Task<List<string>> GetPlaylistVideoUrls(string playlistUrl)
    {
        string cookiesArgument = GetCookiesArgument();

        using var process = Process.Start(BuildProcessStartInfo(
            $"--js-runtime deno --encoding utf-8 {cookiesArgument} --flat-playlist --dump-json --no-warnings \"{playlistUrl}\""));
        if (process is null)
            throw new InvalidOperationException("Failed to start yt-dlp process.");

        string output = await ReadProcessOutputAsync(process);

        var urls = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(line.Trim());
                if (doc.RootElement.TryGetProperty("webpage_url", out var urlElement))
                {
                    string url = urlElement.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(url))
                        urls.Add(url);
                }
            }
            catch
            {
                continue;
            }
        }

        return urls;
    }
}
