using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace YouTubeDownloader.Services;

public class ToolDownloader
{

    private readonly HttpClient client = new();


    public async Task DownloadFile(
        string url,
        string path,
        Action<int, string> progress,
        string statusKey)
    {

        using var response = await client.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);


        response.EnsureSuccessStatusCode();


        long total =
            response.Content.Headers.ContentLength ?? -1;


        using var stream =
            await response.Content.ReadAsStreamAsync();


        using var file =
            new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write);



        byte[] buffer = new byte[81920];


        long downloaded = 0;


        int read;


        while (
            (read = await stream.ReadAsync(buffer)) > 0)
        {

            await file.WriteAsync(
                buffer.AsMemory(0, read));


            downloaded += read;



            if (total > 0)
            {

                int percent =
                    (int)(
                    downloaded * 100 / total);


                progress(
                    percent,
                    string.Format(
                        LanguageService.Get(statusKey),
                        percent)
                );

            }

        }

    }

}