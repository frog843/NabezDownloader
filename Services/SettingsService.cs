using System;
using System.IO;
using System.Text.Json;

namespace YouTubeDownloader.Services;

public class Settings
{
    public bool FirstRunCompleted { get; set; }
    public string CookiesPath { get; set; } = "";
    public string DownloadPath { get; set; } = "";
    public bool SkipTitleCheck { get; set; }
    public bool SkipFormat { get; set; }
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "System";
    public string AudioFormat { get; set; } = "mp3";
    public string Frequency { get; set; } = "44100";
    public string CompletionSound { get; set; } = "default";
    public double WindowWidth { get; set; } = 450;
    public double WindowHeight { get; set; } = 600;
}



public static class SettingsService
{

    static string Folder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YouTubeDownloader"
        );


    static string FilePath =>
        Path.Combine(Folder, "settings.json");



    public static Settings Load()
    {
        if (!File.Exists(FilePath))
            return new Settings();


        string json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<Settings>(json)
               ?? new Settings();
    }




    public static void Save(Settings settings)
    {
        Directory.CreateDirectory(Folder);


        string json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });


        File.WriteAllText(FilePath, json);
    }

}