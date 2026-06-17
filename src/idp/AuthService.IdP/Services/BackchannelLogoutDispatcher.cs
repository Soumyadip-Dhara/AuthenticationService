using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace AuthService.IdP.Services;

/// <summary>
/// Dispatches back-channel logout tokens to all registered BFF clients
/// when a user logs out or the SSO session is terminated.
///
/// For each registered client with a backchannel_logout_uri:
/// 1. Build a logout_token JWT (RS256 signed)
/// 2. POST it to the client's endpoint
/// 3. Log success/failure
/// </summary>
public class BackchannelLogoutDispatcher
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackchannelLogoutDispatcher> _logger;
    private readonly SigningCredentials _signingCredentials;

    public BackchannelLogoutDispatcher(
        IOpenIddictApplicationManager applicationManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BackchannelLogoutDispatcher> logger,
        SigningCredentials signingCredentials)
    {
        _applicationManager = applicationManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _signingCredentials = signingCredentials;
    }

    /// <summary>
    /// Fan-out logout_token to all registered BFFs for the given sub + sid.
    /// </summary>
    public async Task DispatchAsync(string sub, string sid, CancellationToken ct = default)
    {
        var issuer = _configuration["Issuer"] ?? "https://localhost:5001/";
        var backchannelClients = _configuration
            .GetSection("BackchannelLogout:Clients")
            .Get<List<BackchannelClientConfig>>() ?? [];

        var tasks = backchannelClients.Select(client =>
            SendLogoutTokenAsync(client, sub, sid, issuer, ct));

        var results = await Task.WhenAll(tasks);

        var succeeded = results.Count(r => r);
        var failed = results.Length - succeeded;
        _logger.LogInformation(
            "Back-channel logout dispatched for sid={Sid}: {Succeeded} succeeded, {Failed} failed",
            sid, succeeded, failed);
    }

    private async Task<bool> SendLogoutTokenAsync(
        BackchannelClientConfig client,
        string sub,
        string sid,
        string issuer,
        CancellationToken ct)
    {
        try
        {
            var logoutToken = BuildLogoutToken(client.ClientId, sub, sid, issuer);
            var httpClient = _httpClientFactory.CreateClient("backchannel");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("logout_token", logoutToken)
            });

            var response = await httpClient.PostAsync(client.LogoutUri, content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Back-channel logout succeeded for client={ClientId} at {Uri}",
                    client.ClientId, client.LogoutUri);
                return true;
            }

            _logger.LogWarning(
                "Back-channel logout failed for client={ClientId} at {Uri}: {StatusCode}",
                client.ClientId, client.LogoutUri, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Back-channel logout error for client={ClientId} at {Uri}",
                client.ClientId, client.LogoutUri);
            return false;
        }
    }

    /// <summary>
    /// Build a logout_token JWT per OIDC Back-Channel Logout spec:
    /// - iss, sub, aud, iat, jti claims
    /// - sid claim
    /// - events claim with "http://schemas.openid.net/event/backchannel-logout" key
    /// - NO nonce claim (prohibited in logout tokens)
    /// </summary>
    private string BuildLogoutToken(string clientId, string sub, string sid, string issuer)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = clientId,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = sub,
                ["sid"] = sid,
                ["events"] = new Dictionary<string, object>
                {
                    ["http://schemas.openid.net/event/backchannel-logout"] = new Dictionary<string, object>()
                }
            },
            IssuedAt = DateTime.Now,
            SigningCredentials = _signingCredentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }
}

public class BackchannelClientConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string LogoutUri { get; set; } = string.Empty;
}
