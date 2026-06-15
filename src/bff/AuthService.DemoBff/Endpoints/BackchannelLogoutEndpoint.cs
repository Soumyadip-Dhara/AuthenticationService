using System.IdentityModel.Tokens.Jwt;
using AuthService.Shared.Sessions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.DemoBff.Endpoints;

/// <summary>
/// Handles POST /backchannel-logout from the IdP.
///
/// When the IdP terminates an SSO session, it sends a logout_token JWT
/// to each registered BFF. This endpoint:
/// 1. Validates the logout_token signature against the IdP's JWKS
/// 2. Validates iss, aud, and events claims
/// 3. Extracts the sid claim
/// 4. Uses SidIndex to find all local sessions for that sid
/// 5. Removes all those sessions from the TicketStore
/// </summary>
public static class BackchannelLogoutEndpoint
{
    public static void MapBackchannelLogout(this WebApplication app)
    {
        app.MapPost("/backchannel-logout", HandleBackchannelLogout);
    }

    private static async Task<IResult> HandleBackchannelLogout(
        HttpContext context,
        InMemoryTicketStore ticketStore,
        SidIndex sidIndex,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        // The logout_token comes as a form-encoded POST
        var form = await context.Request.ReadFormAsync();
        var logoutToken = form["logout_token"].ToString();

        if (string.IsNullOrEmpty(logoutToken))
        {
            logger.LogWarning("Back-channel logout: no logout_token in request");
            return Results.BadRequest(new { error = "logout_token is required" });
        }

        try
        {
            // Validate the logout_token JWT
            var authority = configuration["Oidc:Authority"] ?? "https://localhost:5001";
            var clientId = configuration["Oidc:ClientId"] ?? "demo-login-bff";

            // Fetch the IdP's JWKS for signature validation
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                })));

            var oidcConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

            var validationParams = new TokenValidationParameters
            {
                ValidIssuer = authority.TrimEnd('/') + "/",
                ValidAudience = clientId,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // logout_token has no exp requirement
                RequireSignedTokens = true
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(logoutToken, validationParams, out var validatedToken);

            // Validate the events claim (required by OIDC Back-Channel Logout spec)
            var eventsClaim = principal.FindFirst("events")?.Value;
            if (eventsClaim == null || !eventsClaim.Contains("backchannel-logout"))
            {
                logger.LogWarning("Back-channel logout: missing or invalid events claim");
                return Results.BadRequest(new { error = "Invalid logout_token: missing events claim" });
            }

            // Ensure no nonce claim (prohibited in logout tokens)
            if (principal.FindFirst("nonce") != null)
            {
                logger.LogWarning("Back-channel logout: logout_token contains prohibited nonce claim");
                return Results.BadRequest(new { error = "Invalid logout_token: nonce claim is prohibited" });
            }

            // Extract sid
            var sid = principal.FindFirst("sid")?.Value;
            if (string.IsNullOrEmpty(sid))
            {
                logger.LogWarning("Back-channel logout: no sid in logout_token");
                return Results.BadRequest(new { error = "Invalid logout_token: missing sid claim" });
            }

            // Revoke all sessions associated with this sid
            var sessionKeys = sidIndex.GetSessions(sid);
            logger.LogInformation(
                "Back-channel logout: revoking {Count} sessions for sid={Sid}",
                sessionKeys.Count, sid);

            foreach (var key in sessionKeys)
            {
                await ticketStore.RemoveAsync(key);
            }

            sidIndex.ClearSid(sid);

            return Results.Ok();
        }
        catch (SecurityTokenValidationException ex)
        {
            logger.LogWarning(ex, "Back-channel logout: token validation failed");
            return Results.BadRequest(new { error = "Invalid logout_token", details = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Back-channel logout: unexpected error");
            return Results.StatusCode(500);
        }
    }
}
