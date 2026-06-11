using Microsoft.AspNetCore.Authentication;

namespace AuthService.DemoBff.Auth;

/// <summary>
/// Middleware that checks if the current access token is expired
/// and silently refreshes it using the stored refresh token.
/// 
/// This implements Flow 5 from the architecture docs:
/// - Detect access_token expiry from ticket metadata
/// - POST /connect/token with grant_type=refresh_token
/// - Update ticket in TicketStore with new tokens
/// - Next API call uses new access_token transparently
/// 
/// The browser is never involved in this process.
/// </summary>
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRefreshMiddleware> _logger;

    public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var authenticateResult = await context.AuthenticateAsync("cookie");
            if (authenticateResult.Succeeded && authenticateResult.Properties != null)
            {
                var expiresAt = authenticateResult.Properties.GetTokenValue("expires_at");
                if (expiresAt != null &&
                    DateTimeOffset.TryParse(expiresAt, out var expiresAtDate) &&
                    expiresAtDate < DateTimeOffset.UtcNow.AddMinutes(1)) // Refresh 1 min before expiry
                {
                    var refreshToken = authenticateResult.Properties.GetTokenValue("refresh_token");
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await TryRefreshTokenAsync(context, authenticateResult, refreshToken);
                    }
                }
            }
        }

        await _next(context);
    }

    private async Task TryRefreshTokenAsync(
        HttpContext context,
        AuthenticateResult authenticateResult,
        string refreshToken)
    {
        try
        {
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var authority = configuration["Oidc:Authority"] ?? "https://localhost:5001";
            var clientId = configuration["Oidc:ClientId"] ?? "demo-login-bff";
            var clientSecret = configuration["Oidc:ClientSecret"] ?? "demo-login-bff-secret";

            var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("token-refresh");

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await httpClient.PostAsync($"{authority}/connect/token", tokenRequest);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenRefreshResponse>();
                if (tokenResponse != null)
                {
                    // Update the authentication ticket with new tokens
                    var properties = authenticateResult.Properties!;
                    properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                    properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken ?? refreshToken);

                    if (tokenResponse.ExpiresIn > 0)
                    {
                        var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                        properties.UpdateTokenValue("expires_at", newExpiresAt.ToString("o"));
                    }

                    // Re-sign in to persist the updated ticket
                    await context.SignInAsync("cookie", authenticateResult.Principal!, properties);

                    _logger.LogInformation("Token refreshed successfully for user {Sub}",
                        authenticateResult.Principal?.FindFirst("sub")?.Value);
                }
            }
            else
            {
                _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
        }
    }
}

internal class TokenRefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}
