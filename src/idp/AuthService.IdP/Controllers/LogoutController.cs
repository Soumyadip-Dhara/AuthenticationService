using System.Security.Claims;
using AuthService.IdP.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.IdP.Controllers;

/// <summary>
/// Handles the OIDC /connect/logout endpoint.
/// 
/// Flow:
/// 1. Parse id_token_hint to extract sub + sid
/// 2. Dispatch back-channel logout tokens to all registered BFFs
/// 3. Sign out the IdP session (clear .idp.session cookie)
/// 4. Redirect to post_logout_redirect_uri
/// </summary>
[ApiController]
public class LogoutController : ControllerBase
{
    private readonly BackchannelLogoutDispatcher _dispatcher;
    private readonly ILogger<LogoutController> _logger;

    public LogoutController(
        BackchannelLogoutDispatcher dispatcher,
        ILogger<LogoutController> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to get sub + sid from the id_token_hint
        string? sub = null;
        string? sid = null;

        // Try from the OpenIddict server authentication
        var serverResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (serverResult.Succeeded && serverResult.Principal != null)
        {
            sub = serverResult.Principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            sid = serverResult.Principal.FindFirstValue("sid");
        }

        // Fallback: try from the IdP session
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(sid))
        {
            var sessionResult = await HttpContext.AuthenticateAsync("idp-session");
            if (sessionResult.Succeeded && sessionResult.Principal != null)
            {
                sub ??= sessionResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? sessionResult.Principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
                sid ??= sessionResult.Principal.FindFirstValue("sid");
            }
        }

        // Dispatch back-channel logout to all BFFs
        if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(sid))
        {
            _logger.LogInformation("Dispatching back-channel logout for sub={Sub}, sid={Sid}", sub, sid);
            await _dispatcher.DispatchAsync(sub, sid);
        }
        else
        {
            _logger.LogWarning("Could not extract sub/sid for back-channel logout dispatch");
        }

        // Sign out the IdP session
        await HttpContext.SignOutAsync("idp-session");

        // Sign out via OpenIddict server (clears server-side state)
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    [HttpGet("~/connect/applogout")]
    [HttpPost("~/connect/applogout")]
    public async Task<IActionResult> AppLogout()
    {
        // Try to get sub + sid from the id_token_hint or server authentication or idp session
        string? sub = null;
        string? sid = null;

        var serverResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (serverResult.Succeeded && serverResult.Principal != null)
        {
            sub = serverResult.Principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
            sid = serverResult.Principal.FindFirstValue("sid");
        }

        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(sid))
        {
            var sessionResult = await HttpContext.AuthenticateAsync("idp-session");
            if (sessionResult.Succeeded && sessionResult.Principal != null)
            {
                sub ??= sessionResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? sessionResult.Principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
                sid ??= sessionResult.Principal.FindFirstValue("sid");
            }
        }

        if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(sid))
        {
            _logger.LogInformation("Dispatching back-channel logout for sub={Sub}, sid={Sid} without signing out of IdP", sub, sid);
            await _dispatcher.DispatchAsync(sub, sid);
        }
        else
        {
            _logger.LogWarning("Could not extract sub/sid for back-channel logout dispatch in AppLogout");
        }

        return Redirect("/Dashboard");
    }
}
