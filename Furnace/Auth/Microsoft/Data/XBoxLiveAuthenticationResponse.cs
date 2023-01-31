using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Utility;

namespace Furnace.Auth.Microsoft.Data;

public partial class XBoxLiveAuthenticationResponse : IJsonConvertable<XBoxLiveAuthenticationResponse>
{
    public static XBoxLiveAuthenticationResponse FromJson(string json) => 
        JsonSerializer.Deserialize<XBoxLiveAuthenticationResponse>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class XBoxLiveAuthenticationResponse
{
    [JsonPropertyName("IssueInstant")]
    public DateTime IssueInstant { get; set; }
    
    [JsonPropertyName("NotAfter")]
    public DateTime NotAfter { get; set; }

    [JsonPropertyName("Token")]
    public required string Token { get; set; }
    
    [JsonPropertyName("DisplayClaims")]
    public required XBoxLiveAuthenticationResponseDisplayClaims DisplayClaims { get; set; }
}

public class XBoxLiveAuthenticationResponseDisplayClaims
{
   [JsonPropertyName("xui")]
   public required XBoxLiveAuthenticationResponseUserHash[] Xui { get; set; }
}

public class XBoxLiveAuthenticationResponseUserHash
{
   [JsonPropertyName("uhs")] public required string UserHash { get; set; }
}