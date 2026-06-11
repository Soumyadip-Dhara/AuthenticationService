using System.Security.Claims;
using AuthService.IdP.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthService.IdP.Controllers;

/// <summary>
/// Handles user login for the IdP.
///
/// GET  /Account/Login    → redirect to the Angular login UI
/// POST /Account/LoginApi → validate credentials, create IdP session, return JSON
///
/// The IdP session cookie (.idp.session) is set here.
/// The sid (session ID) is minted here — this is the root of the SSO chain.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AuthDbContext dbContext, ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Redirect to the login UI. The IdP serves the Angular app's static files.
    /// The returnUrl is passed as a query parameter so the UI can redirect back after login.
    /// </summary>
    [HttpGet("Login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        // Serve the Angular login app with the returnUrl embedded
        // The Angular app is served as static files from wwwroot
        var redirectTo = $"/index.html?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
        return Redirect(redirectTo);
    }

    /// <summary>
    /// API endpoint called by the Angular login form.
    /// Validates credentials and creates the IdP SSO session.
    /// </summary>
    [HttpPost("LoginApi")]
    public async Task<IActionResult> LoginApi([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email and password are required." });
        }

        // Find user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        // Mint a new session ID — this is the SSO session identifier
        var sid = Guid.NewGuid().ToString();

        // Build the IdP session principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new("sid", sid)
        };

        var identity = new ClaimsIdentity(claims, "idp-session");
        var principal = new ClaimsPrincipal(identity);

        // Sign in → sets .idp.session cookie
        await HttpContext.SignInAsync("idp-session", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        _logger.LogInformation("User {Email} logged in, sid={Sid}", user.Email, sid);

        return Ok(new
        {
            returnUrl = request.ReturnUrl ?? "/"
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
