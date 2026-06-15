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
    private readonly AuthService.IdP.DAL.IDPDBContext1 _idpDbContext1;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        AuthService.IdP.DAL.IDPDBContext1 idpDbContext1,
        ILogger<AccountController> logger)
    {
        _idpDbContext1 = idpDbContext1;
        _logger = logger;
    }

    /// <summary>
    /// Redirect to the login UI. The IdP serves the Angular app's static files.
    /// The returnUrl is passed as a query parameter so the UI can redirect back after login.
    /// </summary>
    [HttpGet("Login")]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl)
    {
        // If already logged in, redirect directly to dashboard
        var result = await HttpContext.AuthenticateAsync("idp-session");
        if (result.Succeeded && result.Principal != null)
        {
            return Redirect($"/Dashboard?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }

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

        var inputEmailOrUser = request.Email.Trim().ToLowerInvariant();

        // Find user by email or username in IDPDBContext1
        var user = await _idpDbContext1.UserMasters
            .FirstOrDefaultAsync(u => (u.Email != null && u.Email.ToLower() == inputEmailOrUser) || u.UserName.ToLower() == inputEmailOrUser);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for {Email}: User is inactive", request.Email);
            return Unauthorized(new { error = "Your account is inactive." });
        }

        if (user.IsBlocked)
        {
            _logger.LogWarning("Failed login attempt for {Email}: User is blocked", request.Email);
            return Unauthorized(new { error = "Your account is blocked." });
        }

        // Mint a new session ID — this is the SSO session identifier
        var sid = Guid.NewGuid().ToString();

        // Build the IdP session principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email ?? user.UserName),
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

        _logger.LogInformation("User {Email} logged in, sid={Sid}", user.Email ?? user.UserName, sid);

        var dashboardUrl = $"/Dashboard?returnUrl={Uri.EscapeDataString(request.ReturnUrl ?? "/")}";
        return Ok(new
        {
            returnUrl = dashboardUrl
        });
    }

    //private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    //{
    //    if (storedHash == null || storedSalt == null) return false;
    //    using var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt);
    //    var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    //    return computedHash.SequenceEqual(storedHash);
    //}
    private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
        {
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i]) return false;
            }
        }
        return true;
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
