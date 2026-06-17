using System;
using System.Security.Claims;
using AuthService.IdP.Data;
using AuthService.IdP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.DataProtection;

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
    private readonly IDataProtector _protector;
    private readonly IConfiguration _configuration;
    private readonly TotpService _totpService;
    private readonly INotificationService _notificationService;

    public AccountController(
        AuthService.IdP.DAL.IDPDBContext1 idpDbContext1,
        ILogger<AccountController> logger,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        TotpService totpService,
        INotificationService notificationService)
    {
        _idpDbContext1 = idpDbContext1;
        _logger = logger;
        _protector = dataProtectionProvider.CreateProtector("AuthService.IdP.AccountController");
        _configuration = configuration;
        _totpService = totpService;
        _notificationService = notificationService;
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
    /// Serves a dynamically generated secure SVG CAPTCHA image and saves its encrypted answer in a cookie.
    /// </summary>
    [HttpGet("Captcha")]
    public IActionResult GetCaptcha()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // clear distinct alphanumeric characters
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 5)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        var expiry = DateTimeOffset.Now.AddMinutes(2).ToUnixTimeSeconds();
        var payload = $"{code}:{expiry}";
        var encrypted = _protector.Protect(payload);

        Response.Cookies.Append(".idp.captcha", encrypted, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.Now.AddMinutes(5)
        });

        var svg = GenerateCaptchaSvg(code);
        return Content(svg, "image/svg+xml");
    }

    private static string GenerateCaptchaSvg(string code)
    {
        var random = new Random();
        var width = 150;
        var height = 50;
        var svgBuilder = new System.Text.StringBuilder();

        svgBuilder.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}' style='background: rgba(255, 255, 255, 0.05); border-radius: 8px;'>");

        for (int i = 0; i < 5; i++)
        {
            var x1 = random.Next(width);
            var y1 = random.Next(height);
            var x2 = random.Next(width);
            var y2 = random.Next(height);
            var color = $"rgba({random.Next(100, 255)}, {random.Next(100, 255)}, {random.Next(100, 255)}, 0.2)";
            svgBuilder.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='1.5' />");
        }

        for (int i = 0; i < 30; i++)
        {
            var cx = random.Next(width);
            var cy = random.Next(height);
            var r = random.Next(1, 3);
            var color = $"rgba({random.Next(100, 255)}, {random.Next(100, 255)}, {random.Next(100, 255)}, 0.15)";
            svgBuilder.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='{color}' />");
        }

        for (int i = 0; i < code.Length; i++)
        {
            var character = code[i];
            var fontSize = random.Next(26, 32);
            var rotation = random.Next(-20, 20);
            var x = 15 + (i * 25) + random.Next(-3, 3);
            var y = 35 + random.Next(-4, 4);
            var fill = $"rgb({random.Next(150, 255)}, {random.Next(150, 255)}, {random.Next(200, 255)})";
            svgBuilder.Append($"<text x='{x}' y='{y}' fill='{fill}' font-size='{fontSize}' font-family='Courier New, monospace' font-weight='bold' transform='rotate({rotation} {x} {y})'>{character}</text>");
        }

        svgBuilder.Append("</svg>");
        return svgBuilder.ToString();
    }

    /// <summary>
    /// API endpoint called by the Angular login form.
    /// Validates credentials, verifies CAPTCHA, and handles configurable 2FA (OTP) flow.
    /// </summary>
    [HttpPost("LoginApi")]
    public async Task<IActionResult> LoginApi([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email and password are required." });
        }

        // --- CAPTCHA verification ---
        if (string.IsNullOrWhiteSpace(request.Captcha))
        {
            return BadRequest(new { error = "CAPTCHA code is required." });
        }

        var captchaCookie = Request.Cookies[".idp.captcha"];
        if (string.IsNullOrEmpty(captchaCookie))
        {
            return BadRequest(new { error = "CAPTCHA session expired. Please reload CAPTCHA." });
        }

        string decCode;
        long decExpiry;
        try
        {
            var decrypted = _protector.Unprotect(captchaCookie);
            var parts = decrypted.Split(':');
            decCode = parts[0];
            decExpiry = long.Parse(parts[1]);
        }
        catch
        {
            return BadRequest(new { error = "Invalid CAPTCHA session." });
        }

        if (DateTimeOffset.Now.ToUnixTimeSeconds() > decExpiry)
        {
            return BadRequest(new { error = "CAPTCHA expired. Please reload." });
        }

        if (!string.Equals(request.Captcha.Trim(), decCode, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Incorrect CAPTCHA code." });
        }

        Response.Cookies.Delete(".idp.captcha");

        // --- Credential check ---
        var inputEmailOrUser = request.Email.Trim().ToLowerInvariant();
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

        var isTwoFactorEnabled = _configuration.GetValue<bool>("TwoFactor:Enabled");
        if (isTwoFactorEnabled)
        {
            var sid = Guid.NewGuid().ToString();
            if (string.Equals(request.AuthMethod, "Totp", StringComparison.OrdinalIgnoreCase))
            {
                if (user.TotpEnabled == true)
                {
                    var expiry = DateTimeOffset.Now.AddMinutes(5).ToUnixTimeSeconds();
                    var pendingPayload = $"{user.Id}:totp:{expiry}:{sid}:{request.ReturnUrl ?? "/"}";
                    var encryptedPending = _protector.Protect(pendingPayload);

                    Response.Cookies.Append(".idp.pending-login", encryptedPending, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.Now.AddMinutes(10)
                    });

                    return Ok(new { requiresOtp = true, authMethod = "Totp", setupTotp = false });
                }
                else
                {
                    return BadRequest(new { error = "TOTP is not enabled for this user. Please log in using standard OTP." });
                }
            }
            else
            {
                string otp;
                var disableOtp = _configuration.GetValue<bool>("NotificationService:DisableOtp");
                if (disableOtp)
                {
                    otp = _configuration.GetValue<string>("NotificationService:DefaultOtp") ?? "123456";
                }
                else
                {
                    otp = new Random().Next(100000, 999999).ToString();
                }

                // Call external notification service
                try
                {
                    var payload = new SmsPayload
                    {
                        MobileNumber = user.MobileNumber,
                        Message = $"Your OTP for login is {otp}."
                    };
                    var (result, message, status) = await _notificationService.SendSmsUsingQueue(payload);
                    if (!result)
                    {
                        _logger.LogWarning("NotificationService returned failure result. Status: {Status}, Message: {Message}", status, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while calling NotificationService to send SMS OTP.");
                }

                _logger.LogInformation("\n==================================================");
                _logger.LogInformation("[2FA OTP FOR USER {Email}]: {Otp}", user.Email ?? user.UserName, otp);
                _logger.LogInformation("==================================================\n");

                var otpExpiry = DateTimeOffset.Now.AddMinutes(5).ToUnixTimeSeconds();
                var pendingPayload = $"{user.Id}:{otp}:{otpExpiry}:{sid}:{request.ReturnUrl ?? "/"}";
                var encryptedPending = _protector.Protect(pendingPayload);

                Response.Cookies.Append(".idp.pending-login", encryptedPending, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.Now.AddMinutes(10)
                });

                return Ok(new { requiresOtp = true, authMethod = "Normal" });
            }
        }

        // Complete login immediately
        var newSid = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email ?? user.UserName),
            new("sid", newSid)
        };

        var identity = new ClaimsIdentity(claims, "idp-session");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("idp-session", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.Now.AddHours(8)
        });

        _logger.LogInformation("User {Email} logged in directly, sid={Sid}", user.Email ?? user.UserName, newSid);

        var dashboardUrl = $"/Dashboard?returnUrl={Uri.EscapeDataString(request.ReturnUrl ?? "/")}";
        return Ok(new
        {
            returnUrl = dashboardUrl
        });
    }

    /// <summary>
    /// Verifies 2FA OTP code and completes the sign in.
    /// </summary>
    [HttpPost("VerifyOtpApi")]
    public async Task<IActionResult> VerifyOtpApi([FromBody] OtpVerificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Otp))
        {
            return BadRequest(new { error = "Verification code is required." });
        }

        var pendingCookie = Request.Cookies[".idp.pending-login"];
        if (string.IsNullOrEmpty(pendingCookie))
        {
            return BadRequest(new { error = "Session expired. Please sign in again." });
        }

        long userId;
        string expectedOtpOrMethod;
        long expiry;
        string sid;
        string returnUrl;

        try
        {
            var decrypted = _protector.Unprotect(pendingCookie);
            var parts = decrypted.Split(':');
            userId = long.Parse(parts[0]);
            expectedOtpOrMethod = parts[1];
            expiry = long.Parse(parts[2]);
            sid = parts[3];
            returnUrl = parts[4];
        }
        catch
        {
            return BadRequest(new { error = "Invalid verification session." });
        }

        if (DateTimeOffset.Now.ToUnixTimeSeconds() > expiry)
        {
            return BadRequest(new { error = "Verification session expired. Please try signing in again." });
        }

        var user = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || user.IsBlocked)
        {
            return BadRequest(new { error = "User account is inactive or blocked." });
        }

        if (string.Equals(expectedOtpOrMethod, "totp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(user.TotpSecret) || user.TotpEnabled != true)
            {
                return BadRequest(new { error = "TOTP is not configured for this user." });
            }

            var isTotpValid = _totpService.VerifyTotp(user.TotpSecret, request.Otp, out var totpError);
            if (!isTotpValid)
            {
                return BadRequest(new { error = totpError ?? "Incorrect verification code." });
            }
        }
        else
        {
            if (request.Otp.Trim() != expectedOtpOrMethod)
            {
                return BadRequest(new { error = "Incorrect OTP code." });
            }
        }

        Response.Cookies.Delete(".idp.pending-login");

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

        await HttpContext.SignInAsync("idp-session", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.Now.AddHours(8)
        });

        _logger.LogInformation("User {Email} logged in via 2FA, sid={Sid}", user.Email ?? user.UserName, sid);

        var dashboardUrl = $"/Dashboard?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Ok(new
        {
            returnUrl = dashboardUrl
        });
    }

    /// <summary>
    /// Generates a new TOTP setup secret and OIDC URL for the currently authenticated user.
    /// </summary>
    [HttpGet("GetTotpSetupDetailsApi")]
    public async Task<IActionResult> GetTotpSetupDetailsApi()
    {
        var result = await HttpContext.AuthenticateAsync("idp-session");
        if (!result.Succeeded || result.Principal == null)
        {
            return Unauthorized(new { error = "Please log in first." });
        }

        var userIdStr = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid user session." });
        }

        var user = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || user.IsBlocked)
        {
            return BadRequest(new { error = "User account is inactive or blocked." });
        }

        var secretKey = _totpService.GenerateSecretKey();
        var expiry = DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds();
        var sid = result.Principal.FindFirstValue("sid") ?? Guid.NewGuid().ToString();
        var pendingPayload = $"{user.Id}:{secretKey}:{expiry}:{sid}:/Dashboard";
        var encryptedPending = _protector.Protect(pendingPayload);

        Response.Cookies.Append(".idp.pending-totp-setup", encryptedPending, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.Now.AddMinutes(15)
        });

        var qrCodeUrl = $"otpauth://totp/AuthService:{Uri.EscapeDataString(user.UserName)}?secret={secretKey}&issuer=AuthService";
        return Ok(new
        {
            secret = secretKey,
            qrCodeUrl = qrCodeUrl
        });
    }

    /// <summary>
    /// Verifies the TOTP code entered during setup/registration and enables TOTP for the user.
    /// </summary>
    [HttpPost("VerifyTotpSetupApi")]
    public async Task<IActionResult> VerifyTotpSetupApi([FromBody] VerifyTotpSetupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { error = "Verification code is required." });
        }

        var pendingCookie = Request.Cookies[".idp.pending-totp-setup"];
        if (string.IsNullOrEmpty(pendingCookie))
        {
            return BadRequest(new { error = "Session expired or invalid. Please login again." });
        }

        long userId;
        string secretKey;
        long expiry;
        string sid;
        string returnUrl;

        try
        {
            var decrypted = _protector.Unprotect(pendingCookie);
            var parts = decrypted.Split(':');
            userId = long.Parse(parts[0]);
            secretKey = parts[1];
            expiry = long.Parse(parts[2]);
            sid = parts[3];
            returnUrl = parts[4];
        }
        catch
        {
            return BadRequest(new { error = "Invalid TOTP setup session." });
        }

        if (DateTimeOffset.Now.ToUnixTimeSeconds() > expiry)
        {
            return BadRequest(new { error = "Setup session expired. Please start over." });
        }

        var isTotpValid = _totpService.VerifyTotp(secretKey, request.Code, out var totpError);
        if (!isTotpValid)
        {
            return BadRequest(new { error = totpError ?? "Incorrect verification code. Please try again." });
        }

        var user = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || user.IsBlocked)
        {
            return BadRequest(new { error = "User account is inactive or blocked." });
        }

        user.TotpSecret = secretKey;
        user.TotpEnabled = true;
        user.TotpVerifiedAt = DateTime.Now;

        await _idpDbContext1.SaveChangesAsync();

        Response.Cookies.Delete(".idp.pending-totp-setup");

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

        await HttpContext.SignInAsync("idp-session", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.Now.AddHours(8)
        });

        _logger.LogInformation("User {Email} completed TOTP setup, enabled TOTP, and logged in, sid={Sid}", user.Email ?? user.UserName, sid);

        var dashboardUrl = $"/Dashboard?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Ok(new
        {
            returnUrl = dashboardUrl
        });
    }

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
    public string Captcha { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = "Normal";
}

public class OtpVerificationRequest
{
    public string Otp { get; set; } = string.Empty;
}

public class VerifyTotpSetupRequest
{
    public string Code { get; set; } = string.Empty;
}
