using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class XboxLiveAuthenticationRequest : IJsonConvertable<XboxLiveAuthenticationRequest>
{
    public static XboxLiveAuthenticationRequest FromJson(string json) => 
        JsonSerializer.Deserialize<XboxLiveAuthenticationRequest>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}
    
public partial class XboxLiveAuthenticationRequest
{
    [JsonPropertyName("Properties")]
    public required XboxLiveAuthenticationRequestProperties Properties { get; set; }
    
    [JsonPropertyName("RelyingParty")]
    public required Uri RelyingParty { get; set; }
    
    [JsonPropertyName("TokenType")]
    public required string TokenType { get; set; }
}

public class XboxLiveAuthenticationRequestProperties
{
    [JsonPropertyName("AuthMethod")]
    public required string AuthenticationMethod { get; set; }
        
    [JsonPropertyName("SiteName")]
    public required string SiteName { get; set; }
        
    [JsonPropertyName("RpsTicket")]
    public required string RpsTicket { get; set; }
}
