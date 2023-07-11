using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class MinecraftXboxAuthenticationResponse : IJsonConvertable<MinecraftXboxAuthenticationResponse>
{
    public static MinecraftXboxAuthenticationResponse FromJson(string json) =>
        JsonSerializer.Deserialize<MinecraftXboxAuthenticationResponse>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class MinecraftXboxAuthenticationResponse
{
    [JsonPropertyName("username")] public required string Username { get; set; }
    [JsonPropertyName("roles")] public required object[] Roles { get; set; }
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }
    [JsonPropertyName("token_type")] public required string TokenType { get; set; }
    [JsonPropertyName("expires_in")] public required int ExpiresInSeconds { get; set; }
}