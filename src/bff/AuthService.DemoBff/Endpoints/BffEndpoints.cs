using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.DemoBff.Endpoints;

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
        app.MapGet("/bff/logout", (Delegate)Logout).RequireAuthorization();
    }

    /// <summary>
    /// Triggers OIDC authorization code flow with PKCE.
    /// The browser is redirected to the IdP's /connect/authorize endpoint.
    /// After successful auth, the callback lands on /signin-oidc.
    /// </summary>
    private static IResult Login(HttpContext context)
    {
        // If already authenticated, redirect to frontend
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return Results.Redirect("https://localhost:4300/dashboard");
        }

        return Results.Challenge(
            properties: new AuthenticationProperties
            {
                RedirectUri = "https://localhost:4300/dashboard"
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
        var claims = user.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(
                g => g.Key,
                g => g.Count() == 1 ? (object)g.First().Value : g.Select(c => c.Value).ToArray()
            );

        return Results.Ok(claims);
    }

    /// <summary>
    /// Signs out of the BFF session and redirects to IdP logout.
    /// 
    /// Flow:
    /// 1. SignOut("cookie") → removes server-side ticket, expires .demo.session cookie
    /// 2. SignOut("oidc") → redirects to IdP /connect/logout with id_token_hint
    /// 3. IdP dispatches back-channel logout to all BFFs
    /// </summary>
    private static async Task<IResult> Logout(HttpContext context)
    {
        // Sign out of the local cookie session
        await context.SignOutAsync("cookie");

        // Sign out via OIDC → redirects to IdP logout endpoint
        await context.SignOutAsync("oidc");

        // The OIDC sign-out handler will redirect to IdP, so we return empty
        return Results.Empty;
    }
}
