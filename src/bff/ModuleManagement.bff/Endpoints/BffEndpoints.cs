using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Um.bff.Endpoints;

/// <summary>
/// BFF endpoint definitions:
///   GET /bff/login  → Challenge OIDC (triggers PKCE redirect to IdP)
///   GET /bff/user   → Returns user claims from server-side session (or 401)
///   GET /bff/logout  → Signs out locally + redirects to IdP logout
/// </summary>
public static class BffEndpoints
{
    public static void MapBffEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/login", Login);
        app.MapGet("/bff/user", GetUser).RequireAuthorization();
        app.MapGet("/bff/logout/local", LogoutLocal).RequireAuthorization();
        app.MapGet("/bff/logout/global", LogoutGlobal).RequireAuthorization();
    }

    /// <summary>
    /// Triggers OIDC authorization code flow with PKCE.
    /// The browser is redirected to the IdP's /connect/authorize endpoint.
    /// After successful auth, the callback lands on /signin-oidc.
    /// </summary>
    private static IResult Login(HttpContext context, IConfiguration config)
    {
        string frontendUrl = config["FrontendUrl"] ?? "https://localhost:4500";
        // If already authenticated, redirect to frontend
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return Results.Redirect($"{frontendUrl}/dashboard");
        }

        return Results.Challenge(
            properties: new AuthenticationProperties
            {
                RedirectUri = $"{frontendUrl}/dashboard"
            },
            authenticationSchemes: ["oidc"]);
    }

    /// <summary>
    /// Returns the authenticated user's claims from the server-side session.
    /// If no valid session exists, returns 401 (RequireAuthorization handles this).
    /// The frontend calls this to check authentication state.
    /// </summary>
    private static IResult GetUser(ClaimsPrincipal user)
    {
        var claims = user.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();
        return Results.Ok(claims);
    }

    /// <summary>
    /// Signs out of the local BFF session ONLY.
    /// Redirects to the IDP Dashboard.
    /// </summary>
    private static async Task<IResult> LogoutLocal(HttpContext context, IConfiguration config)
    {
        string idpUrl = config["Oidc:Authority"] ?? "https://10.176.100.90:5001";
        await context.SignOutAsync("cookie");
        return Results.Redirect($"{idpUrl.TrimEnd('/')}/dashboard");
    }

    /// <summary>
    /// Signs out of the BFF session AND the IDP.
    /// Redirects to IdP logout.
    /// </summary>
    private static async Task<IResult> LogoutGlobal(HttpContext context)
    {
        await context.SignOutAsync("cookie");
        await context.SignOutAsync("oidc");
        return Results.Empty;
    }
}
