using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class XSTSErrorResponse : IJsonConvertable<XSTSErrorResponse>
{
    public static XSTSErrorResponse FromJson(string jsonString) =>
        JsonSerializer.Deserialize<XSTSErrorResponse>(jsonString, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class XSTSErrorResponse
{
    [JsonPropertyName("Identity")] public required string Identity { get; set; }
    [JsonPropertyName("XErr")] public required uint RawCode { get; set; }
    [JsonPropertyName("Message")] public required string Message { get; set; }
    [JsonPropertyName("Redirect")] public required Uri RedirectUri { get; set; }

    [JsonIgnore]
    public XSTSErrorCode ErrorCode => RawCode switch
    {
        2148916233 => XSTSErrorCode.NoXboxAccount,
        2148916235 => XSTSErrorCode.ServiceBanned,
        2148916236 => XSTSErrorCode.AdultVerificationRequired,
        2148916237 => XSTSErrorCode.AdultVerificationRequired,
        2148916238 => XSTSErrorCode.ChildAccount,
        _ => XSTSErrorCode.Other
    };
}