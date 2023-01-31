using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json;
using Furnace.Auth.Microsoft.Data;
using Furnace.Log;
using Microsoft.Identity.Client;
using Logger = Furnace.Log.Logger;

namespace Furnace.Auth.Microsoft;

using XSTSAuthenticationResponse = XBoxLiveAuthenticationResponse;

public class MicrosoftAuth
{
    private readonly HttpClient _httpClient;

    private const string XboxAuthUri = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XSTSAuthUri = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MinecraftAuthUri = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MinecraftEntitlementsUri = "https://api.minecraftservices.com/entitlements/mcstore";
    private const string MinecraftProfileUri = "https://api.minecraftservices.com/minecraft/profile";
    private readonly Logger _logger;

    public MicrosoftAuth()
    {
        _httpClient = new HttpClient();
        _logger = LogManager.GetLogger();
    }
    
    private static PublicClientApplicationOptions GetOptions() => new PublicClientApplicationOptions
    {
        ClientId = "1179be7f-9713-4440-b90f-c31396be5210",
        RedirectUri = "http://localhost:25566",
        AadAuthorityAudience = AadAuthorityAudience.PersonalMicrosoftAccount
    };

    public async Task<UserProfile> AuthenticateAsync()
    {
        var msAuth = await AuthenticateWithMicrosoftAsync();
        var xboxLiveAuth = await AuthenticateWithXboxLiveAsync(msAuth);
        var xstsAuth = await AuthenticateXSTSLiveAsync(xboxLiveAuth);
        var mcAuth = await AuthenticateWithMinecraftAsync(xstsAuth);
        var ownsGame = await DoesUserOwnGameAsync(mcAuth);
        var mcProfile = await GetUserProfileAsync(mcAuth);
        return new UserProfile
        {
            Uuid = mcProfile.Uuid,
            AccessToken = mcAuth.AccessToken,
            ClientId = null,
            Username = mcProfile.Username,
            ExpiryTime = DateTime.Now.AddSeconds(mcAuth.ExpiresInSeconds),
            IsDemoUser = !ownsGame,
            AuthTypeString = "msa",
            XboxUserId = xstsAuth.Token
        };
    }

    private static async Task<AuthenticationResult> AuthenticateWithMicrosoftAsync()
    {
        var options = GetOptions(); // your own method
        var app = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
            .Build();

        var token = await app.AcquireTokenInteractive(new[]{"XboxLive.signin", "XboxLive.offline_access"}).ExecuteAsync();
        return token;
    }

    private async Task<XBoxLiveAuthenticationResponse> AuthenticateWithXboxLiveAsync(AuthenticationResult msAuthResult)
    {
        var response = await _httpClient.PostAsJsonAsync(new Uri(XboxAuthUri), new XboxLiveAuthenticationRequest
        {
            Properties = new XboxLiveAuthenticationRequestProperties
            {
                AuthenticationMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={msAuthResult.AccessToken}"
            },
            RelyingParty = new Uri("http://auth.xboxlive.com"),
            TokenType = "JWT"
        });

        response.EnsureSuccessStatusCode();

        using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());
        return XBoxLiveAuthenticationResponse.FromJson(await streamReader.ReadToEndAsync());
    }

    private async Task<XSTSAuthenticationResponse> AuthenticateXSTSLiveAsync(XBoxLiveAuthenticationResponse xboxResponse)
    {
        var response = await _httpClient.PostAsJsonAsync(new Uri(XSTSAuthUri), new XSTSAuthenticationRequest
        {
            Properties = new XSTSAuthenticationRequestProperties
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xboxResponse.Token }
            },
            RelyingParty = new Uri("rp://api.minecraftservices.com/"),
            TokenType = "JWT"
        });

        if (!response.IsSuccessStatusCode)
        {
            string? responseContent = null;
            try
            {
                using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());
                responseContent = await streamReader.ReadToEndAsync();
                var error = XSTSErrorResponse.FromJson(responseContent);
                throw new AuthenticationException(error.ErrorCode switch
                {
                    XSTSErrorCode.NoXboxAccount =>
                        "The authentication response indicates no XBox account is associated with this Microsoft account",
                    XSTSErrorCode.AdultVerificationRequired =>
                        "Adult verification (via Xbox) is required before authentication will be granted",
                    XSTSErrorCode.ServiceBanned => "The XSTS service is unavailable in this region",
                    XSTSErrorCode.ChildAccount =>
                        "This service is unavailable for child accounts that are not in a family",
                    XSTSErrorCode.Other =>
                        $"An unknown error response was received: {{ Code = {error.RawCode}, Message = \"{error.Message}\"}}",
                    _ => throw new UnreachableException("Enum should never be an undefined value")
                });
            }
            catch (JsonException) { }
            finally
            {
                _logger.E($"Non-success status code with response: '{responseContent}'");
                response.EnsureSuccessStatusCode();
            }
        }
        
        using var sr = new StreamReader(await response.Content.ReadAsStreamAsync());
        return XSTSAuthenticationResponse.FromJson(await sr.ReadToEndAsync());
    }

    private async Task<MinecraftXboxAuthenticationResponse> AuthenticateWithMinecraftAsync(XBoxLiveAuthenticationResponse xstsResponse)
    {
        var response = await _httpClient.PostAsJsonAsync(new Uri(MinecraftAuthUri), new MinecraftXboxAuthenticationRequest
        {
            IdentityToken = $"XBL3.0 x={xstsResponse.DisplayClaims.Xui[0].UserHash};{xstsResponse.Token}"
        });

        response.EnsureSuccessStatusCode();
        
        using var sr = new StreamReader(await response.Content.ReadAsStreamAsync());
        return MinecraftXboxAuthenticationResponse.FromJson(await sr.ReadToEndAsync());
    }

    private async Task<bool> DoesUserOwnGameAsync(MinecraftXboxAuthenticationResponse mcAuth)
    {
        //TODO: Check against MinecraftEntitlementsUri;
        return true;
    }

    private async Task<MinecraftProfileResponse> GetUserProfileAsync(MinecraftXboxAuthenticationResponse mcAuth)
    {
        _logger.D($"GET {MinecraftProfileUri} Headers = {{ Authorization: Bearer {mcAuth.AccessToken} }}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, MinecraftProfileUri);
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", mcAuth.AccessToken);
    
        var response = await _httpClient.SendAsync(requestMessage);
        using var sr = new StreamReader(await response.Content.ReadAsStreamAsync());
        var responseString = await sr.ReadToEndAsync();
        
        if (!response.IsSuccessStatusCode)
            _logger.E(responseString);
        response.EnsureSuccessStatusCode();
        _logger.D($"Got a success response! {responseString}");

        return MinecraftProfileResponse.FromJson(responseString);
    }
}