using System.Diagnostics;
using System.Dynamic;
using Furnace.Log;
using Furnace.Utility;

namespace Furnace.Tasks;

public static class HttpJsonGetTask
{
    public static HttpJsonGetTaskInner<T> Create<T>(HttpClient client, Uri uri) where T : IJsonConvertable<T>
        => new (
        client,
        uri,
        T.FromJson,
        3,
        TimeSpan.FromSeconds(5)
    );
    
    public static HttpJsonGetTaskInner<T> Create<T>(HttpClient client, Uri uri, int maxRetries, TimeSpan retryTimeout) where T : IJsonConvertable<T>
        => new (
            client,
            uri,
            T.FromJson,
            maxRetries,
            retryTimeout
        );
    
    public static HttpJsonGetTaskInner<T> Create<T>(HttpClient client, Uri uri, Func<string, T> converter)
        => new HttpJsonGetTaskInner<T>(
        client,
        uri,
        converter,
        3,
        TimeSpan.FromSeconds(5)
    );
    
    public static HttpJsonGetTaskInner<T> Create<T>(HttpClient client, Uri uri, Func<string, T> converter, int maxRetries, TimeSpan retryTimeout)
        => new (
            client,
            uri,
            converter,
            maxRetries,
            retryTimeout
        );

    public class HttpJsonGetTaskInner<T> : Runnable
{
    private readonly HttpClient _client;
    private readonly Uri _uri;
    private static readonly Logger _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;
    private readonly Func<string, T> _converter;

    static HttpJsonGetTaskInner()
    {
        _logger = new Logger("HttpGetJson");
    }

    public HttpJsonGetTaskInner(HttpClient client, Uri uri, Func<string, T> converter, int maxRetries, TimeSpan retryTimeout)
    {
        _client = client;
        _uri = uri;
        _maxRetries = maxRetries;
        _timeout = retryTimeout;
        _converter = converter;
    }

    private async Task<Stream> TryToDownload()
    {
        _logger.T($"Attempting GET from {_uri.AbsoluteUri}...");
        try
        {
            // Download to a temp file first to ensure a partial file is not saved that is then not overwritten.
            await using var ws = await _client.GetStreamAsync(_uri.AbsoluteUri);
            var ms = new MemoryStream();
            await ws.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            _logger.T($"Completed GET from {_uri.AbsoluteUri}...");
            return ms;

        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException requestException)
                _logger.E(
                    $"HTTP[{requestException.StatusCode}] at {_uri.AbsoluteUri} - {requestException.Message}");
            else
                _logger.E($"Exception GET-ing data from {_uri.AbsoluteUri}");
            _logger.D(
                $"Exception thrown is {ex.GetType()}(\"{ex.Message}\"){ex.StackTrace ?? "No stack trace is present within the exception."}");
            throw;
        }
    }

    public new async Task<T> RunAsync(CancellationToken ct) => await RunAsync(null, ct);

    public override async Task<T> RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        Exception? previousException = null;

        for (var i = 0; i < _maxRetries; i++)
        {
            try
            {
                var stream = await TryToDownload();
                return _converter.Invoke(await new StreamReader(stream).ReadToEndAsync(ct));
            }
            catch (Exception ex)
            {
                previousException = ex;
                _logger.I("Retrying download in 5 seconds.");
                await Task.Delay(_timeout, ct);
            }
        }

        if (previousException == null)
            throw new UnreachableException($"Should not reach this point with unset {nameof(previousException)}");
        
        _logger.E("Maximum retries exceeded!");
        throw previousException;
    }
}
}