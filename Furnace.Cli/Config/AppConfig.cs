using System.Text.RegularExpressions;
using Furnace.Lib.Utility.Extension;
using Newtonsoft.Json;

namespace Furnace.Cli;

public partial class AppConfig
{
    [GeneratedRegex("^[0-9]+[BbKkMmGgTt]?$")]
    private static partial Regex MemoryRegex();
    
    private string _memory = "4g";

    [JsonProperty("memory")]
    public string Memory
    {
        get => _memory;
        set => _memory = MemoryRegex().IsMatch(value)
            ? value
            : throw new FormatException("Invalid memory value");
    }

    
}

public partial class AppConfig
{
    public static string ConfigFileName { get; } = "config.json";

    public async Task WriteConfig()
    {
        var f = Program.RootDirectory.GetFileInfo(ConfigFileName);
        await using var fs = f.OpenWrite();
        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public static async Task<AppConfig> ReadConfig()
    {
        var f = Program.RootDirectory.GetFileInfo(ConfigFileName);
        try
        {
            await using var fs = f.OpenRead();
            using var reader = new StreamReader(fs);
            return JsonConvert.DeserializeObject<AppConfig>(await reader.ReadToEndAsync())!;
        }
        catch (FileNotFoundException)
        {
            return new AppConfig();
        }
    } 
}