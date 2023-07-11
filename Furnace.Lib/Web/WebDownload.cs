namespace Furnace.Lib.Web;

public struct WebDownload
{
    public Uri Uri;
    public FileInfo TargetFile;
    public FileInfo TempFile;
    public int FailuresLeft;
    public bool OverwriteExisting;

    public WebDownload(Uri uri, FileInfo targetFile)
    {
        Uri = uri;
        TargetFile = targetFile;
        TempFile = new FileInfo(Path.GetTempFileName());
        // TODO: Update this from config.
        FailuresLeft = 3; 
        OverwriteExisting = false;
    }
}