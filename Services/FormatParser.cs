using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YouTubeDownloader.Models;

namespace YouTubeDownloader.Services;

public static class FormatParser
{
    public static List<VideoFormat> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return new List<VideoFormat>();

        var formats = new List<VideoFormat>();

        foreach (var line in output.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split('|');
            if (parts.Length < 4)
                continue;

            string id = parts[0].Trim();
            string ext = parts[1].Trim();
            string resolution = parts[2].Trim();
            string fpsStr = parts[3].Trim();
            string tbrStr = parts.Length > 4 ? parts[4].Trim() : "0";

            if (!int.TryParse(id, out _))
                continue;

            if (resolution.Equals("audio only", StringComparison.OrdinalIgnoreCase))
                continue;

            int fps = 0;
            if (!string.IsNullOrEmpty(fpsStr) &&
                int.TryParse(fpsStr, out int parsedFps))
            {
                fps = parsedFps;
            }

            int height = 0;
            if (!string.IsNullOrEmpty(resolution) && resolution.Contains("x"))
            {
                var sizeParts = resolution.Split('x');
                if (sizeParts.Length == 2 &&
                    int.TryParse(sizeParts[1], out int h))
                {
                    height = h;
                }
            }

            if (height <= 0)
                continue;

            double bitrate = 0;
            if (!string.IsNullOrEmpty(tbrStr) &&
                double.TryParse(
                    tbrStr,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double tbr))
            {
                bitrate = tbr;
            }

            formats.Add(new VideoFormat
            {
                Id = id,
                Extension = ext,
                Resolution = height + "p",
                Codec = "Unknown",
                Bitrate = bitrate,
                Fps = fps,
                Type = "Video"
            });
        }

        return formats
            .GroupBy(x => x.Resolution)
            .Select(g => g
                .OrderByDescending(f => f.Fps)
                .ThenByDescending(f => f.Bitrate)
                .First())
            .ToList();
    }
}
