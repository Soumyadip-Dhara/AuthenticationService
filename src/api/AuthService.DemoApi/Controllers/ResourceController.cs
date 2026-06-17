using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace AuthService.DemoApi.Controllers;

/// <summary>
/// Sample protected API endpoints.
/// These are accessed through the BFF reverse proxy — the browser never calls them directly.
/// Token validation is via introspection against the IdP.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class ResourceController : ControllerBase
{
    /// <summary>
    /// Sample protected resource endpoint.
    /// Returns data only if the access token is valid (via introspection).
    /// </summary>
    [HttpGet("resource")]
    public IActionResult GetResource()
    {
        return Ok(new
        {
            message = "This is a protected resource from Demo.Api",
            timestamp = DateTime.Now,
            data = new[]
            {
                new { id = 1, name = "Project Alpha", status = "active" },
                new { id = 2, name = "Project Beta", status = "planning" },
                new { id = 3, name = "Project Gamma", status = "completed" }
            }
        });
    }

    /// <summary>
    /// Returns the authenticated user's claims as seen by the API.
    /// Useful for debugging — shows what the introspection result contains.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var claims = User.Claims.Select(c => new
        {
            type = c.Type,
            value = c.Value
        });

        return Ok(new
        {
            sub = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value,
            name = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value,
            email = User.FindFirst(OpenIddictConstants.Claims.Email)?.Value,
            claims
        });
    }
}
