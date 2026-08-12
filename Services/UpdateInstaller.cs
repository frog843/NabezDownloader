using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace YouTubeDownloader.Services;

public static class UpdateInstaller
{
    private static string AppDataFolder =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "YouTubeDownloader"
        );

    public static async Task DownloadAndInstall(
        string downloadUrl,
        Action<int, string> progress)
    {
        Directory.CreateDirectory(AppDataFolder);

        string currentExe = Process.GetCurrentProcess().MainModule?.FileName
                            ?? Environment.ProcessPath
                            ?? "";

        string newExePath = Path.Combine(
            AppDataFolder,
            "NabezDownloader.update.exe");

        string updaterPath = Path.Combine(
            AppDataFolder,
            "updater.bat");

        progress(0, LanguageService.Get("Update.Downloading"));

        using (var client = new HttpClient())
        {
            using var response = await client.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            long total =
                response.Content.Headers.ContentLength ?? -1;

            using var stream =
                await response.Content.ReadAsStreamAsync();

            using var file = new FileStream(
                newExePath,
                FileMode.Create,
                FileAccess.Write);

            byte[] buffer = new byte[81920];
            long downloaded = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;

                if (total > 0)
                {
                    int percent = (int)(downloaded * 100 / total);
                    progress(
                        percent,
                        string.Format(
                            LanguageService.Get("Update.DownloadingPercent"),
                            percent));
                }
            }
        }

        progress(100, LanguageService.Get("Update.Preparing"));

        ExtractUpdater(updaterPath);

        if (!File.Exists(updaterPath))
            throw new Exception("Updater not found");

        if (string.IsNullOrEmpty(currentExe) || !File.Exists(currentExe))
            throw new Exception("Current executable not found");

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments =
                $"/c \"\"{updaterPath}\" \"{currentExe}\" \"{newExePath}\"\"",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);

        Environment.Exit(0);
    }

    private static void ExtractUpdater(string destination)
    {
        try
        {
            var asm = typeof(UpdateInstaller).Assembly;

            using var stream = asm.GetManifestResourceStream(
                "YouTubeDownloader.Assets.updater.bat");

            if (stream == null)
                return;

            using var file = new FileStream(
                destination,
                FileMode.Create,
                FileAccess.Write);

            stream.CopyTo(file);
        }
        catch
        {
        }
    }
}
