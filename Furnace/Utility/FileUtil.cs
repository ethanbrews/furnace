namespace Furnace.Utility;

public static class FileUtil
{
    public static DirectoryInfo CreateUniqueTempDirectory()
    {
        var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Directory.CreateDirectory(uniqueTempDir);
        return new DirectoryInfo(uniqueTempDir);
    }

    public static async Task<T> ReadAsync<T>(this FileInfo file, Func<string, T> converter, CancellationToken ct)
    {
        await using var stream = file.OpenRead();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync(ct);
        return converter.Invoke(text);
    }

    public static async Task<T> ReadAsync<T>(this FileInfo file, CancellationToken ct) where T : IJsonConvertable<T> =>
        await ReadAsync(file, T.FromJson, ct);
}