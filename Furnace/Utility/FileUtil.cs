namespace Furnace.Utility;

public static class FileUtil
{
    public static DirectoryInfo CreateUniqueTempDirectory()
    {
        var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Directory.CreateDirectory(uniqueTempDir);
        return new DirectoryInfo(uniqueTempDir);
    }
}