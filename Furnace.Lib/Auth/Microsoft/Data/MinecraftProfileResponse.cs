using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class MinecraftProfileResponse : IJsonConvertable<MinecraftProfileResponse>
{
    public static MinecraftProfileResponse FromJson(string json) =>
        JsonSerializer.Deserialize<MinecraftProfileResponse>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class MinecraftProfileResponse
{
    [JsonPropertyName("id")] public required string Uuid { get; set; }
    [JsonPropertyName("name")] public required string Username { get; set; }
    [JsonPropertyName("skins")] public required MinecraftProfileResponseSkin[] Skins { get; set; }
    [JsonPropertyName("capes")] public required MinecraftProfileResponseSkin[] Capes { get; set; }
    [JsonPropertyName("profileActions")] public object? ProfileActions { get; set; }
}

public class MinecraftProfileResponseSkin
{
    [JsonPropertyName("id")] public required Guid Uuid { get; set; }
    [JsonPropertyName("state")] public required string State { get; set; }
    [JsonPropertyName("url")] public required Uri Url { get; set; }
    [JsonPropertyName("variant")] public string? Variant { get; set; }
    [JsonPropertyName("alias")] public string? Alias { get; set; }
}

