namespace Furnace.Utility.Extension;

public static class FileExtensions
{
    public static FileInfo GetFileInfo(this DirectoryInfo directoryInfo, string path)
    {
        var fullPath = Path.Join(directoryInfo.FullName, path);
        var fileInfo = new FileInfo(fullPath);
        fileInfo.Directory?.Create();
        return fileInfo;
    }
}