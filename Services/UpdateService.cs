using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace YouTubeDownloader.Services;


public class UpdateService
{

    string ToolsFolder =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "YouTubeDownloader",
            "tools"
        );


    string ToolExt => OperatingSystem.IsWindows() ? ".exe" : "";

    string YtDlp =>
        Path.Combine(
            ToolsFolder,
            "yt-dlp" + ToolExt
        );

    string FFmpeg =>
        Path.Combine(
            ToolsFolder,
            "ffmpeg" + ToolExt
        );

    string Deno =>
        Path.Combine(
            ToolsFolder,
            "deno" + ToolExt
        );

    string YtDlpUrl =>
        OperatingSystem.IsWindows()
            ? "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
            : "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp";

    string FFmpegUrl =>
        OperatingSystem.IsWindows()
            ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
            : "https://evermeet.cx/ffmpeg/getrelease/ffmpeg/zip";

    string DenoUrl
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

            if (OperatingSystem.IsMacOS())
            {
                var arch = RuntimeInformation.ProcessArchitecture;
                return arch switch
                {
                    Architecture.Arm64 =>
                        "https://github.com/denoland/deno/releases/latest/download/deno-aarch64-apple-darwin.zip",
                    _ =>
                        "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-apple-darwin.zip"
                };
            }

            return "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-unknown-linux-gnu.zip";
        }
    }



    public async Task CheckTools(
        Action<int, string> progress)
    {

        Directory.CreateDirectory(
            ToolsFolder);



        var downloader =
            new ToolDownloader();



        if (!File.Exists(YtDlp))
        {

            progress(
                0,
                string.Format(
                    LanguageService.Get("Tool.DownloadingYtDlp"),
                    0)
            );


            await downloader.DownloadFile(
                YtDlpUrl,
                YtDlp,
                progress,
                "Tool.DownloadingYtDlp"
            );

            if (!OperatingSystem.IsWindows())
            {
                try { MakeExecutable(YtDlp); } catch { }
            }

        }



        if (!File.Exists(FFmpeg))
        {

            progress(
                0,
                string.Format(
                    LanguageService.Get("Tool.DownloadingFfmpeg"),
                    0)
            );

            string zipPath =
                Path.Combine(
                    ToolsFolder,
                    "ffmpeg.zip");


            await downloader.DownloadFile(
                FFmpegUrl,
                zipPath,
                progress,
                "Tool.DownloadingFfmpeg"
            );


            progress(
                0,
                LanguageService.Get("Tool.ExtractingFfmpeg")
            );

            ExtractFfmpeg(zipPath);


            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            if (!OperatingSystem.IsWindows())
            {
                try { MakeExecutable(FFmpeg); } catch { }
            }

        }



        if (!File.Exists(Deno))
        {

            progress(
                0,
                string.Format(
                    LanguageService.Get("Tool.DownloadingDeno"),
                    0)
            );

            string denoZipPath =
                Path.Combine(
                    ToolsFolder,
                    "deno.zip");


            await downloader.DownloadFile(
                DenoUrl,
                denoZipPath,
                progress,
                "Tool.DownloadingDeno"
            );


            progress(
                0,
                LanguageService.Get("Tool.ExtractingDeno")
            );

            ExtractDeno(denoZipPath);


            if (File.Exists(denoZipPath))
            {
                File.Delete(denoZipPath);
            }

            if (!OperatingSystem.IsWindows())
            {
                try { MakeExecutable(Deno); } catch { }
            }

        }



        progress(
            100,
            LanguageService.Get("Tool.PrepareComplete")
        );

    }

    private void ExtractFfmpeg(string zipPath)
    {
        string targetName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        using var archive =
            ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals(
                targetName,
                StringComparison.OrdinalIgnoreCase))
            {
                string destPath =
                    Path.Combine(
                        ToolsFolder,
                        entry.FullName);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destPath)!);

                entry.ExtractToFile(
                    destPath,
                    overwrite: true);

                string finalPath =
                    Path.Combine(
                        ToolsFolder,
                        targetName);

                if (!string.Equals(
                    destPath,
                    finalPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(finalPath))
                        File.Delete(finalPath);
                    File.Move(destPath, finalPath);
                }

                break;
            }
        }
    }

    private void ExtractDeno(string zipPath)
    {
        string targetName = OperatingSystem.IsWindows() ? "deno.exe" : "deno";
        using var archive =
            ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals(
                targetName,
                StringComparison.OrdinalIgnoreCase))
            {
                string destPath =
                    Path.Combine(
                        ToolsFolder,
                        entry.FullName);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destPath)!);

                entry.ExtractToFile(
                    destPath,
                    overwrite: true);

                break;
            }
        }
    }

    private void MakeExecutable(string path)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
        }
    }

}