using System.Threading.Channels;
using Furnace.Log;
using Furnace.Utility;

namespace Furnace.Web;

public static class WebService
{
    public static HttpClient Client { get; }
    private static readonly Logger Logger = Logger.GetLogger();

    static WebService()
    {
        Client = new HttpClient();
    }

    public static async Task DownloadFileAsync(Uri uri, FileInfo targetFile, CancellationToken ct) =>
        await DownloadFileAsync(new WebDownload(uri, targetFile), ct);

    public static async Task<T> GetJson<T>(Uri uri, CancellationToken ct) where T : IJsonConvertable<T> =>
        await GetJsonObjectAsync(new JsonDownload(uri), T.FromJson, ct);
    
    public static async Task<T> GetJson<T>(Uri uri, Func<string, T> converter, CancellationToken ct) =>
        await GetJsonObjectAsync(new JsonDownload(uri), converter, ct);

    private static async Task DownloadFileAsync(WebDownload download, CancellationToken ct)
    {

        if (download is { TargetFile: { Exists: true, Length: > 0 }, OverwriteExisting: false })
        {
            Logger.T($"Skipping existing file with overwrite disabled: {download.TargetFile.FullName}");
            return;
        }

        try
        {
            // Download to a temp file first to ensure a partial file is not left behind on error.
            await using var s = await Client.GetStreamAsync(download.Uri.AbsoluteUri, ct);
            await using (var fs = download.TempFile.OpenWrite())
            {
                await s.CopyToAsync(fs, ct);
            }
            File.Copy(download.TempFile.FullName, download.TargetFile.FullName, true);
            Logger.T($"Downloaded {download.Uri}");
        }
        catch (Exception ex)
        {
            if (download.FailuresLeft > 0)
            {
                download.FailuresLeft--;
                await DownloadFileAsync(download, ct);
            }
            else
            {
                if (ex is HttpRequestException requestException)
                    Logger.W($"HTTP[{requestException.StatusCode}] at {download.Uri.AbsoluteUri} - {requestException.Message}");
                else
                    Logger.W($"Exception downloading {download.Uri.AbsoluteUri} -> {download.TargetFile}");
                Logger.D($"Failed temp file is saved at {download.TempFile.FullName}");
                Logger.D($"Exception thrown is {ex.GetType()}(\"{ex.Message}\"){ex.StackTrace ?? "No stack trace is present within the exception."}");
                throw;
            }
        }
    }

    private static async Task<Stream> GetAsync(JsonDownload download, CancellationToken ct)
    {
        Logger.T($"Attempting GET from {download.Uri.AbsoluteUri}...");
        try
        {
            // Download to a temp file first to ensure a partial file is not saved that is then not overwritten.
            await using var ws = await Client.GetStreamAsync(download.Uri.AbsoluteUri, ct);
            var ms = new MemoryStream();
            await ws.CopyToAsync(ms, ct);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;

        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException requestException)
                Logger.E(
                    $"HTTP[{requestException.StatusCode}] at {download.Uri.AbsoluteUri} - {requestException.Message}");
            else
                Logger.E($"Exception GET-ing data from {download.Uri.AbsoluteUri}");
            Logger.D(
                $"Exception thrown is {ex.GetType()}(\"{ex.Message}\"){ex.StackTrace ?? "No stack trace is present within the exception."}");
            throw;
        }
    }

    private static async Task<T> GetJsonObjectAsync<T>(JsonDownload download, Func<string, T> converter, CancellationToken ct)
    {
        Stream stream;
        try
        {
            stream = await GetAsync(download, ct);
        }
        catch (Exception _)
        {
            if (download.FailuresLeft <= 0) throw;
            download.FailuresLeft--;
            return await GetJsonObjectAsync(download, converter, ct);
        }
        
        return converter.Invoke(await new StreamReader(stream).ReadToEndAsync(ct));
    }
}