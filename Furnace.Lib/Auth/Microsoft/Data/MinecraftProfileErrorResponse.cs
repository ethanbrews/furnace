using System.Text.Json;
using System.Text.Json.Serialization;
using Furnace.Lib.Utility;

namespace Furnace.Lib.Auth.Microsoft.Data;

public partial class MinecraftProfileErrorResponse : IJsonConvertable<MinecraftProfileErrorResponse>
{
    public static MinecraftProfileErrorResponse FromJson(string jsonString) =>
        JsonSerializer.Deserialize<MinecraftProfileErrorResponse>(jsonString, JsonConverter.Settings)!;

    public string ToJson() => JsonSerializer.Serialize(this, JsonConverter.Settings);
}

public partial class MinecraftProfileErrorResponse
{
    [JsonPropertyName("path")] public required string Path { get; set; }
    [JsonPropertyName("errorType")] public required string ErrorType { get; set; }
    [JsonPropertyName("error")] public required string Error { get; set; }
    [JsonPropertyName("errorMessage")] public required string ErrorMessage { get; set; }
    [JsonPropertyName("DeveloperMessage")] public required string DeveloperMessage { get; set; }

    public string AsHumanReadable() => 
        $"MinecraftProfileErrorResponse {{\n\tPath = {Path}\n\tErrorType = {ErrorType}\n\tError = {Error}\n\tErrorMessage = {ErrorMessage}\n\tDeveloperMessage = {DeveloperMessage}\n}}";
}