using System.Security.Claims;
using AuthService.IdP.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.IdP.Controllers;

/// <summary>
/// Handles the OIDC /connect/authorize endpoint.
/// 
/// Flow:
/// 1. Check for idp-session cookie
/// 2. If no cookie → redirect to login page
/// 3. If cookie exists → build claims via ClaimsBuilder → issue authorization code
/// </summary>
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly ClaimsBuilder _claimsBuilder;

    public AuthorizationController(ClaimsBuilder claimsBuilder)
    {
        _claimsBuilder = claimsBuilder;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to authenticate using the IdP session cookie
        var result = await HttpContext.AuthenticateAsync("idp-session");

        if (!result.Succeeded || result.Principal == null)
        {
            // No valid session → redirect to login page
            // Preserve the full authorize URL as returnUrl so we can resume after login
            var returnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString;

            return Challenge(
                authenticationSchemes: "idp-session",
                properties: new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                });
        }

        // Session exists → build OIDC claims principal
        var scopes = request.GetScopes();
        var principal = await _claimsBuilder.BuildAsync(result.Principal, request.ClientId!, scopes);

        // Sign in via OpenIddict → issues authorization code
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
