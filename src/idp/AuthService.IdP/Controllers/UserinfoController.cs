using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.IdP.Controllers;

/// <summary>
/// Handles the OIDC /connect/userinfo endpoint.
/// Returns user profile claims from the access token.
/// </summary>
[ApiController]
public class UserinfoController : ControllerBase
{
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public IActionResult Userinfo()
    {
        var claims = new Dictionary<string, object?>();

        var sub = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (sub != null) claims["sub"] = sub;

        var name = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value;
        if (name != null) claims["name"] = name;

        var email = User.FindFirst(OpenIddictConstants.Claims.Email)?.Value;
        if (email != null) claims["email"] = email;

        return Ok(claims);
    }
}
