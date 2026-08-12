using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace YouTubeDownloader.Services;

public class GithubUpdateService
{
    private const string ApiUrl =
        "https://api.github.com/repos/frog843/NabezDownloader-Releases/releases/latest";

    private static readonly string UserAgent = "YouTubeDownloader-Updater";

    public string CurrentVersion { get; }

    public GithubUpdateService()
    {
        CurrentVersion = GetCurrentVersion();
    }

    private static string GetCurrentVersion()
    {
        try
        {
            var attr = typeof(GithubUpdateService)
                .Assembly
                .GetName()
                .Version;

            if (attr != null)
            {
                var v = attr.ToString();
                if (v != null && v.StartsWith("0.0.0.0", StringComparison.Ordinal))
                    return "1.0.0";
                return v!;
            }
        }
        catch
        {
        }

        return "1.0.0";
    }

    public class UpdateInfo
    {
        public bool IsAvailable { get; set; }
        public string LatestVersion { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }

    public async Task<UpdateInfo> CheckForUpdateAsync()
    {
        var result = new UpdateInfo
        {
            CurrentVersion = CurrentVersion
        };

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            var json = await client.GetStringAsync(ApiUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var t)
                ? t.GetString() ?? ""
                : "";

            if (string.IsNullOrWhiteSpace(tag))
                return result;

            var latest = NormalizeVersion(tag);

            result.LatestVersion = latest;
            result.ReleaseNotes = root.TryGetProperty("body", out var b)
                ? b.GetString() ?? ""
                : "";
            result.HtmlUrl = root.TryGetProperty("html_url", out var h)
                ? h.GetString() ?? ""
                : "";

            result.DownloadUrl = GetDownloadUrl(root);

            result.IsAvailable = IsNewer(latest, CurrentVersion);
        }
        catch
        {
        }

        return result;
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        if (root.TryGetProperty("assets", out var assets) &&
            assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.TryGetProperty("browser_download_url", out var u))
                {
                    var url = u.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }
            }
        }

        return "";
    }

    private static string NormalizeVersion(string tag)
    {
        var v = tag.Trim();
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            v = v[1..];

        var parts = v.Split('.');
        if (parts.Length < 3)
            v = v + new string('.', 3 - parts.Length) + "0";

        return v;
    }

    private static bool IsNewer(string latest, string current)
    {
        try
        {
            var l = Version.Parse(latest);
            var c = Version.Parse(current);
            return l.CompareTo(c) > 0;
        }
        catch
        {
            return false;
        }
    }
}
