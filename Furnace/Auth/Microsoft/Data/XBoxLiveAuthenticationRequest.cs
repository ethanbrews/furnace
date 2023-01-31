using System.Text.Json;
using Furnace.Minecraft.Data.AssetManifest;
using Furnace.Utility;


namespace Furnace.Auth.Microsoft.Data;

using System.Text.Json.Serialization;

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
