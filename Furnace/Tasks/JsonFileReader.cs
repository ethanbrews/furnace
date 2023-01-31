using Furnace.Utility;

namespace Furnace.Tasks;

public static class JsonFileReader
{
    public static JsonFileReaderInner<T> Read<T>(FileInfo file) where T : IJsonConvertable<T> =>
        new(file, T.FromJson);

    public static JsonFileReaderInner<T> Read<T>(FileInfo file, Func<string, T> converter) =>
        new(file, converter);

    public class JsonFileReaderInner<T> : Runnable
    {
        private readonly FileInfo _file;
        private readonly Func<string, T> _converter;

        public JsonFileReaderInner(FileInfo file, Func<string, T> converter)
        {
            _converter = converter;
            _file = file;
        }

        public override async Task<T> RunAsync(CancellationToken ct)
        {
            using var reader = new StreamReader(_file.OpenRead());
            return _converter.Invoke(await reader.ReadToEndAsync(ct));
        }
    }
    
}