using Furnace.Log;

namespace Furnace.Tasks;

public class FileDownloadTask : Runnable
{
    private readonly HttpClient _client;
    private readonly Uri _uri;
    private readonly FileInfo _targetFile;
    private readonly Logger _logger;
    private readonly bool _overwrite;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;

    public FileDownloadTask(HttpClient client, Uri uri, FileInfo targetFile, bool overwrite = false, int maxRetries = 3,
        TimeSpan? retryTimeout = null)
    {
        _client = client;
        _uri = uri;
        _targetFile = targetFile;
        _logger = LogManager.GetLogger();
        _overwrite = overwrite;
        _maxRetries = maxRetries;
        _timeout = retryTimeout ?? TimeSpan.FromSeconds(5);
    }

    private async Task TryToDownload()
    {
        _logger.T($"Attempting download from {_uri.AbsoluteUri}...");
        var tempFile = new FileInfo(Path.GetTempFileName());
        try
        {
            // Download to a temp file first to ensure a partial file is not saved that is then not overwritten.
            await using var s = await _client.GetStreamAsync(_uri.AbsoluteUri);
            await using (var fs = tempFile.OpenWrite())
            {
                await s.CopyToAsync(fs);
            }
            
            File.Copy(tempFile.FullName, _targetFile.FullName, true);
            
            _logger.T($"Completed download from {_uri.AbsoluteUri}...");
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException requestException)
                _logger.E($"HTTP[{requestException.StatusCode}] at {_uri.AbsoluteUri} - {requestException.Message}");
            else
                _logger.E($"Exception downloading {_uri.AbsoluteUri} -> {_targetFile}");
            _logger.D($"Failed temp file is saved at {tempFile.FullName}");
            _logger.D($"Exception thrown is {ex.GetType()}(\"{ex.Message}\"){ex.StackTrace ?? "No stack trace is present within the exception."}");
            throw;
        }
    }
    
    
    public override async Task RunAsync(CancellationToken ct)
    {
        if (!_overwrite && _targetFile is { Exists: true, Length: > 0 })
        {
            _logger.D($"Skipping existing file: {_targetFile}");
            return;
        }

        Exception? previousException = null;

        for (var i = 0; i < _maxRetries; i++)
        {
            try
            {
                await TryToDownload();
                return;
            }
            catch (Exception ex)
            {
                previousException = ex;
                _logger.I("Retrying download in 5 seconds.");
                await Task.Delay(_timeout, ct);
            }
        }

        if (previousException != null)
        {
            _logger.E("Maximum retries exceeded!");
            throw previousException;
        }
    }
}