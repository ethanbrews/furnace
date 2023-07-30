namespace Furnace.Lib.Utility;

public static class FileUtil
{
    public static DirectoryInfo CreateUniqueTempDirectory()
    {
        var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Directory.CreateDirectory(uniqueTempDir);
        return new DirectoryInfo(uniqueTempDir);
    }

    private static async Task<T> ReadAsync<T>(this FileInfo file, Func<string, T> converter, CancellationToken ct)
    {
        await using var stream = file.OpenRead();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync(ct);
        return converter.Invoke(text);
    }

    public static async Task CopyDirectoryAsync(DirectoryInfo startDirectory, DirectoryInfo endDirectory)
    {
        //Creates all of the directories and sub-directories
        foreach (var dirInfo in startDirectory.GetDirectories("*", SearchOption.AllDirectories))
        {
            var dirPath = dirInfo.FullName;
            var outputPath = dirPath.Replace(startDirectory.FullName, endDirectory.FullName);
            Directory.CreateDirectory(outputPath);

            foreach (var file in dirInfo.EnumerateFiles())
            {
                await using var sourceStream = file.OpenRead();
                await using var destinationStream = File.Create(outputPath +"/"+ file.Name);
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }

    public static async Task<T> ReadAsync<T>(this FileInfo file, CancellationToken ct) where T : IJsonConvertable<T> =>
        await ReadAsync(file, T.FromJson, ct);
}