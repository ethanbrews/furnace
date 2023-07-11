namespace Furnace.Lib.Web;

public class JsonDownload
{
    public Uri Uri;
    public int FailuresLeft;
    public bool OverwriteExisting;

    public JsonDownload(Uri uri)
    {
        Uri = uri;
        // TODO: Update this from config.
        FailuresLeft = 3; 
        OverwriteExisting = false;
    }
}