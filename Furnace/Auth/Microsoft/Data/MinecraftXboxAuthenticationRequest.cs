using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Utility;

namespace Furnace.Auth.Microsoft.Data;

public partial class MinecraftXboxAuthenticationRequest : IJsonConvertable<MinecraftXboxAuthenticationRequest>
{
    public static MinecraftXboxAuthenticationRequest FromJson(string json) =>
        JsonSerializer.Deserialize<MinecraftXboxAuthenticationRequest>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class MinecraftXboxAuthenticationRequest
{
    [JsonPropertyName("identityToken")] public required string IdentityToken { get; set; }
}