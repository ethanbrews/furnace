namespace Furnace.Utility.Extension;

public static class DirectoryInfoExtensions
{
    public static string PathRelativeTo(this FileSystemInfo fsItem, DirectoryInfo root)
    {
        return Path.GetRelativePath(root.FullName, fsItem.FullName);
    }
}