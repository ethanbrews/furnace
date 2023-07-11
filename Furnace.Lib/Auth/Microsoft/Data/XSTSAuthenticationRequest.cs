using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class XSTSAuthenticationRequest : IJsonConvertable<XSTSAuthenticationRequest>
{
    public static XSTSAuthenticationRequest FromJson(string json) =>
        JsonSerializer.Deserialize<XSTSAuthenticationRequest>(json, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class XSTSAuthenticationRequest
{
    [JsonPropertyName("Properties")]
    public required XSTSAuthenticationRequestProperties Properties { get; set; }
    
    [JsonPropertyName("RelyingParty")]
    public required Uri RelyingParty { get; set; }
    
    [JsonPropertyName("TokenType")]
    public required string TokenType { get; set; }
}

public class XSTSAuthenticationRequestProperties
{
    [JsonPropertyName("SandboxId")]
    public required string SandboxId { get; set; }
    
    [JsonPropertyName("UserTokens")]
    public required string[] UserTokens { get; set; }
}