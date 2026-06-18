using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.MdmBff.Endpoints;

public static class BffEndpoints
{
    public static void MapBffEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/login", Login);
        app.MapGet("/bff/user", GetUser).RequireAuthorization();
        app.MapGet("/bff/logout", (Delegate)Logout).RequireAuthorization();
        app.MapGet("/bff/applogout", (Delegate)AppLogout).RequireAuthorization();
    }

    private static IResult Login(HttpContext context)
    {
        var frontendUrl = context.RequestServices.GetRequiredService<IConfiguration>()["FrontendUrl"] ?? "https://localhost:4200/"; // MDM UI — fallback for dev
        var targetDashboardUrl = $"{frontendUrl.TrimEnd('/')}/dashboard";

        // If already authenticated, redirect to frontend
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return Results.Redirect(targetDashboardUrl);
        }

        return Results.Challenge(
            properties: new AuthenticationProperties
            {
                RedirectUri = targetDashboardUrl
            },
            authenticationSchemes: ["oidc"]);
    }

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

    private static async Task<IResult> Logout(HttpContext context)
    {
        // Sign out of the local cookie session
        await context.SignOutAsync("cookie");

        // Sign out via OIDC → redirects to IdP logout endpoint
        await context.SignOutAsync("oidc");

        return Results.Empty;
    }

    private static async Task<IResult> AppLogout(HttpContext context)
    {
        // 1. Sign out of the local cookie session
        await context.SignOutAsync("cookie");

        // 2. Redirect to the IdP's /connect/applogout endpoint
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var idpAuthority = configuration["Oidc:Authority"] ?? "https://10.176.100.17:5001"; // IDP (Identity Provider) — fallback for dev
        var idpAppLogoutUrl = $"{idpAuthority.TrimEnd('/')}/connect/applogout";

        return Results.Redirect(idpAppLogoutUrl);
    }
}
