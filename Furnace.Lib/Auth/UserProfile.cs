using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;
using JsonConverter = Furnace.Lib.Auth.Microsoft.Data.JsonConverter;

namespace Furnace.Lib.Auth;

public partial class UserProfile : IJsonConvertable<UserProfile>
{
    public static UserProfile FromJson(string json) => 
        JsonSerializer.Deserialize<UserProfile>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class UserProfile
{
    [JsonPropertyName("clientId")] public required string? ClientId { get; init; }
    [JsonPropertyName("accessToken")] public required string AccessToken { get; init; }
    [JsonPropertyName("uuid")] public required string Uuid { get; init; }
    [JsonPropertyName("username")] public required string Username { get; init; }
    [JsonPropertyName("isDemoUser")] public bool IsDemoUser { get; init; } = true;
    [JsonPropertyName("expires")] public required DateTime? ExpiryTime { get; init; }
    [JsonPropertyName("authType")] public required string AuthTypeString { get; set; }
    
    [JsonPropertyName("selected")] public bool IsSelected { get; set; }
    
    [JsonPropertyName("xuid")] public string? XboxUserId { get; set; }

    [JsonIgnore]
    public UserProfileAuthenticationType? AuthenticationType => AuthTypeString switch
    {
        "msa" => UserProfileAuthenticationType.Microsoft,
        "mojang" => UserProfileAuthenticationType.Yggdrasil,
        _ => null
    };
}