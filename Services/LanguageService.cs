using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Markup.Xaml;

namespace YouTubeDownloader.Services;

public static class LanguageService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YouTubeDownloader",
        "settings.json");

    private static readonly Dictionary<string, string> Ru = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> Current = new(StringComparer.OrdinalIgnoreCase);

    public static string Language { get; set; } = "System";

    public static event Action? LanguageChanged;

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        Ru["App.Title"] = "Nabez Downloader";
        Ru["Settings.Title"] = "Настройки";
        Ru["Error.Title"] = "Ошибка";
        Ru["Warning.Title"] = "Предупреждение";
        Ru["FolderPicker.Title"] = "Выберите папку загрузки";
        Ru["FolderPicker.ContextMenu"] = "Изменить папку загрузки";
        Ru["CookiesPicker.Title"] = "Выберите cookies.txt";
        Ru["CookiesPicker.Filter"] = "Cookie files";

        Ru["SoundPicker.Title"] = "Выберите звуковой файл";
        Ru["SoundPicker.Filter"] = "WAV files";

        Ru["Status.Done"] = "Готово";
        Ru["Status.Error"] = "Ошибка";
        Ru["Status.Downloading"] = "Скачивание";
        Ru["Status.DownloadingVideo"] = "Скачивание видео...";
        Ru["Status.DownloadingAudio"] = "Скачивание аудио...";
        Ru["Status.Waiting"] = "Ожидание...";
        Ru["Status.ReadyToDownload"] = "Нажмите кнопку \"Скачать\"";
        Ru["Status.UpdatingTools"] = "Обновление инструментов...";
        Ru["Status.ExtractingTools"] = "Распаковка инструментов...";
        Ru["Status.ConvertingTo"] = "Конвертируется в {0}...";
        Ru["Status.Merging"] = "Объединение...";
        Ru["Dialog.Preparing"] = "Подготовка программы";
        Ru["Dialog.PreparingStatus"] = "Подготовка...";
        Ru["Status.GettingFormats"] = "yt-dlp: получение качеств...";
        Ru["Status.GettingFormatsStatus"] = "Получение качеств...";
        Ru["Status.GettingTitle"] = "yt-dlp: получение названия...";
        Ru["Status.GettingTitleStatus"] = "Получение названия...";
        Ru["Status.GettingFormatsList"] = "Получение списка качеств...";
        Ru["Status.DownloadingPlaylist"] = "Скачивание плейлиста...";
        Ru["Status.DownloadingVideoOfPlaylist"] = "Скачивание видео {0} из {1}...";
        Ru["Message.PlaylistDetected"] = "Обнаружен плейлист. Качество берётся для первого видео и применяется ко всем.";
        Ru["Message.PlaylistDownloadComplete"] = "Плейлист скачан!";
        Ru["Message.NoVideoFilesFound"] = "Видео файлы не найдены.";
        Ru["Message.NoAudioFilesFound"] = "Аудио файлы не найдены.";

        Ru["Playlist.Videos"] = "видео";

        Ru["Section.Cookies"] = "Youtube Cookies";
        Ru["Section.Cookies.Desc"] = "Выберите cookies.txt для входа в аккаунт";
        Ru["Section.Folder"] = "Папка загрузки";
        Ru["Section.Folder.Desc"] = "Выберите, куда будут сохраняться видео";
        Ru["Section.Interface"] = "Интерфейс";
        Ru["Section.Language"] = "Язык";
        Ru["Section.Language.System"] = "Системный";
        Ru["Section.Language.Russian"] = "Русский";
        Ru["Section.Language.English"] = "Английский";
        Ru["Section.Theme"] = "Тема";
        Ru["Section.Quality"] = "Качество видео";
        Ru["Section.Formats"] = "Настройки";
        Ru["Section.VideoFormat"] = "Видео формат";
        Ru["Section.AudioFormat"] = "Аудио формат";
        Ru["Section.Frequency"] = "Частота";
        Ru["Section.Download"] = "Скачивание";
        Ru["Section.Reset"] = "Сбросить все настройки";
        Ru["Section.Reset.Desc"] = "Удалит все настройки";

        Ru["Section.Sound"] = "Звук";
        Ru["Section.Sound.Desc"] = "Звук при завершении скачивания";
        Ru["Section.Sound.File"] = "Файл";

        Ru["Button.SelectCookies"] = "Выбрать cookies.txt";
        Ru["Button.SelectFolder"] = "Выбрать папку";
        Ru["Button.SelectSound"] = "Выбрать звук";
        Ru["Button.CheckQuality"] = "Проверить качество";
        Ru["Button.Download"] = "Скачать";
        Ru["Button.ResetAll"] = "Сбросить всё";
        Ru["Button.Back"] = "← Назад";
        Ru["Button.Ok"] = "ОК";
        Ru["Button.Yes"] = "Да";
        Ru["Button.No"] = "Нет";

        Ru["Checkbox.SkipTitle"] = "Пропускать получение названия";
        Ru["Checkbox.DownloadVideo"] = "Скачать видео отдельно";
        Ru["Checkbox.DownloadAudio"] = "Скачать аудио отдельно";
        Ru["Checkbox.SkipFormat"] = "Пропускать формат";

        Ru["Format.MP4"] = "MP4  ›";
        Ru["Format.MP3"] = "MP3  ›";
        Ru["Format.Frequency"] = "44100 Hz  ›";

        Ru["Placeholder.Url"] = "Вставьте Youtube ссылку";

        Ru["Message.ResetConfirm"] = "Вы хотите удалить все настройки и инструменты?\n(потребует перезапуск)";
        Ru["Message.CookiesMissing"] = "Отсутствие файла cookies. Выберите его в настройках";
        Ru["Message.WarningBoth"] = "Если выбрать обе галочки,\nто скачается отдельно видео и аудио.";
         Ru["Message.FormatNotSelected"] = "Формат не выбран. Выберите качество видео.";

        Ru["Tool.DownloadingYtDlp"] = "Скачивание yt-dlp... {0}%";
        Ru["Tool.DownloadingFfmpeg"] = "Скачивание ffmpeg... {0}%";
        Ru["Tool.ExtractingFfmpeg"] = "Распаковка ffmpeg...";
        Ru["Tool.DownloadingDeno"] = "Скачивание deno... {0}%";
        Ru["Tool.ExtractingDeno"] = "Распаковка deno...";
        Ru["Tool.PrepareComplete"] = "Подготовка завершена!";

        Ru["Section.Version"] = "Версия";

        Ru["Update.Available"] = "Доступно обновление";
        Ru["Update.VersionInfo"] = "Текущая версия: {0}  →  Новая: {1}";
        Ru["Update.NoNotes"] = "Подробности на странице релиза.";
        Ru["Update.Download"] = "Скачать";
        Ru["Update.Later"] = "Позже";
        Ru["Update.Downloading"] = "Скачивание обновления...";
        Ru["Update.DownloadingPercent"] = "Скачивание обновления... {0}%";
        Ru["Update.Preparing"] = "Подготовка обновления...";

        En["App.Title"] = "Nabez Downloader";
        En["Settings.Title"] = "Settings";
        En["Error.Title"] = "Error";
        En["Warning.Title"] = "Warning";
        En["FolderPicker.Title"] = "Select download folder";
        En["FolderPicker.ContextMenu"] = "Change download folder";
        En["CookiesPicker.Title"] = "Select cookies.txt";
        En["CookiesPicker.Filter"] = "Cookie files";

        En["SoundPicker.Title"] = "Select sound file";
        En["SoundPicker.Filter"] = "WAV files";

        En["Status.Done"] = "Done";
        En["Status.Error"] = "Error";
        En["Status.Downloading"] = "Downloading";
        En["Status.DownloadingVideo"] = "Downloading video...";
        En["Status.DownloadingAudio"] = "Downloading audio...";
        En["Status.Waiting"] = "Waiting...";
        En["Status.ReadyToDownload"] = "Press the \"Download\" button";
        En["Status.UpdatingTools"] = "Updating tools...";
        En["Status.ExtractingTools"] = "Extracting tools...";
        En["Status.ConvertingTo"] = "Converting to {0}...";
        En["Status.Merging"] = "Merging...";
        En["Dialog.Preparing"] = "Preparing";
        En["Dialog.PreparingStatus"] = "Preparing...";
        En["Status.GettingFormats"] = "yt-dlp: getting formats...";
        En["Status.GettingFormatsStatus"] = "Getting formats...";
        En["Status.GettingTitle"] = "yt-dlp: getting title...";
        En["Status.GettingTitleStatus"] = "Getting title...";
        En["Status.GettingFormatsList"] = "Getting formats list...";

        En["Section.Cookies"] = "Youtube Cookies";
        En["Section.Cookies.Desc"] = "Select cookies.txt to sign in";
        En["Section.Folder"] = "Download folder";
        En["Section.Folder.Desc"] = "Select where videos will be saved";
        En["Section.Interface"] = "Interface";
        En["Section.Language"] = "Language";
        En["Section.Language.System"] = "System";
        En["Section.Language.Russian"] = "Russian";
        En["Section.Language.English"] = "English";
        En["Section.Theme"] = "Theme";
        En["Section.Quality"] = "Video quality";
        En["Section.Formats"] = "Settings";
        En["Section.VideoFormat"] = "Video format";
        En["Section.AudioFormat"] = "Audio format";
        En["Section.Frequency"] = "Frequency";
        En["Section.Download"] = "Download";
        En["Section.Reset"] = "Reset all settings";
        En["Section.Reset.Desc"] = "Will delete all settings";

        En["Section.Sound"] = "Sound";
        En["Section.Sound.Desc"] = "Sound when download completes";
        En["Section.Sound.File"] = "File";

        En["Button.SelectCookies"] = "Select cookies.txt";
        En["Button.SelectFolder"] = "Select folder";
        En["Button.SelectSound"] = "Select sound";
        En["Button.CheckQuality"] = "Check quality";
        En["Button.Download"] = "Download";
        En["Button.ResetAll"] = "Reset all";
        En["Button.Back"] = "← Back";
        En["Button.Ok"] = "OK";
        En["Button.Yes"] = "Yes";
        En["Button.No"] = "No";

        En["Checkbox.SkipTitle"] = "Skip title fetch";
        En["Checkbox.DownloadVideo"] = "Download video separately";
        En["Checkbox.DownloadAudio"] = "Download audio separately";
        En["Checkbox.SkipFormat"] = "Skip format";

        En["Format.MP4"] = "MP4  ›";
        En["Format.MP3"] = "MP3  ›";
        En["Format.Frequency"] = "44100 Hz  ›";

        En["Placeholder.Url"] = "Paste Youtube URL";

        En["Message.ResetConfirm"] = "Delete all settings and tools?\n(requires restart)";
        En["Message.CookiesMissing"] = "Missing cookies file. Select it in settings";
        En["Message.WarningBoth"] = "If both boxes are checked,\nvideo and audio will be downloaded separately.";
         En["Message.FormatNotSelected"] = "Format not selected. Please choose a video quality.";
        En["Message.NoVideoFilesFound"] = "Video files not found.";
        En["Message.NoAudioFilesFound"] = "Audio files not found.";

        En["Status.DownloadingVideo"] = "Downloading video...";
        En["Status.DownloadingAudio"] = "Downloading audio...";
        En["Tool.DownloadingYtDlp"] = "Downloading yt-dlp... {0}%";
        En["Tool.DownloadingFfmpeg"] = "Downloading ffmpeg... {0}%";
        En["Tool.ExtractingFfmpeg"] = "Extracting ffmpeg...";
        En["Tool.DownloadingDeno"] = "Downloading deno... {0}%";
        En["Tool.ExtractingDeno"] = "Extracting deno...";
        En["Tool.PrepareComplete"] = "Preparation complete!";

        En["Section.Version"] = "Version";

        En["Update.Available"] = "Update available";
        En["Update.VersionInfo"] = "Current: {0}  →  New: {1}";
        En["Update.NoNotes"] = "See the release page for details.";
        En["Update.Download"] = "Download";
        En["Update.Later"] = "Later";
        En["Update.Downloading"] = "Downloading update...";
        En["Update.DownloadingPercent"] = "Downloading update... {0}%";
        En["Update.Preparing"] = "Preparing update...";

        En["Status.DownloadingPlaylist"] = "Downloading playlist...";
        En["Status.DownloadingVideoOfPlaylist"] = "Downloading video {0} of {1}...";
        En["Message.PlaylistDetected"] = "Playlist detected. Quality is taken from the first video and applied to all.";
        En["Message.PlaylistDownloadComplete"] = "Playlist downloaded!";

        En["Playlist.Videos"] = "videos";

        Load();
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null && !string.IsNullOrEmpty(settings.Language))
                {
                    Language = settings.Language;
                }
            }
        }
        catch
        {
        }
        ApplyLanguage();
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = SettingsService.Load();
            settings.Language = Language;
            SettingsService.Save(settings);
        }
        catch
        {
        }
    }

    public static string GetEffectiveLanguage()
    {
        if (Language == "System")
        {
            return IsSystemRussian() ? "Russian" : "English";
        }
        return Language == "Russian" ? "Russian" : "English";
    }

    public static void ApplyLanguage()
    {
        if (Application.Current == null)
            return;

        var effective = GetEffectiveLanguage();
        Current.Clear();

        var source = effective == "Russian" ? Ru : En;
        foreach (var kv in source)
            Current[kv.Key] = kv.Value;

        var resources = Application.Current.Resources;
        foreach (var kv in Current)
        {
            if (resources.TryGetResource(kv.Key, null, out var existing) && existing is string)
            {
                resources[kv.Key] = kv.Value;
            }
            else
            {
                resources.Add(kv.Key, kv.Value);
            }
        }

        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (Current.TryGetValue(key, out var value))
            return value;

        if (Ru.TryGetValue(key, out var ru))
            return ru;

        return key;
    }

    public static void CycleLanguage()
    {
        Language = Language switch
        {
            "System" => "Russian",
            "Russian" => "English",
            "English" => "System",
            _ => "System"
        };
        ApplyLanguage();
        Save();
    }

    private static bool IsSystemRussian()
    {
        try
        {
            var locale = System.Globalization.CultureInfo.CurrentUICulture;
            return locale.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}

