using System.Security.Cryptography;
using AuthService.IdP.Data;
using AuthService.IdP.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// 1. Database — PostgreSQL via EF Core
// ──────────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services.AddDbContext<AuthService.IdP.DAL.IDPDBContext1>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ──────────────────────────────────────────────
// 2. OpenIddict — OIDC Server
// ──────────────────────────────────────────────

// Load or generate dev RSA keys (persisted to file to survive restarts)
var signingKey = GetOrCreateRsaKey("temp-signing-key.pem");
var encryptionKey = GetOrCreateRsaKey("temp-encryption-key.pem");

var signingCredentials = new SigningCredentials(
    new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256);

builder.Services.AddSingleton(signingCredentials);

builder.Services.AddOpenIddict()
    // Core: EF Core stores for applications, authorizations, scopes, tokens
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthDbContext>();
    })

    // Server: OIDC endpoints and token handling
    .AddServer(options =>
    {
        // Set public issuer domain
        options.SetIssuer(new Uri("https://wbifms.gov.in/"));

        // Disable access token encryption to issue readable signed JWT access tokens
        options.DisableAccessTokenEncryption();

        // Enable endpoints
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout")
            .SetIntrospectionEndpointUris("connect/introspect")
            .SetUserInfoEndpointUris("connect/userinfo");

        // Enable flows
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();

        // Require PKCE for authorization code flow
        options.RequireProofKeyForCodeExchange();

        // Register signing and encryption credentials
        // Signing: RS256 — BFF and others can verify id_token/logout_token via JWKS
        // Encryption: access tokens are JWE (encrypted) → APIs MUST introspect
        options.AddSigningKey(new RsaSecurityKey(signingKey))
            .AddEncryptionKey(new RsaSecurityKey(encryptionKey));

        // Register scopes
        options.RegisterScopes(
            "openid", "profile", "email", "offline_access",
            "api:demo", "api:mdm", "api:modulemanagement", "api:usermanagement",
            "api:ifms3cts", "api:ifms3ebantan", "api:ifms3ebilling",
            "api:cts", "api:wbjit", "api:wbjitbilling");

        // ASP.NET Core integration
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();

        // Custom event handler to print issued tokens in debugger/console
        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                var accessToken = context.Response.AccessToken;
                var idToken = context.Response.IdToken;

                if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(idToken))
                {
                    System.Diagnostics.Debug.WriteLine("=== OIDC TOKENS ISSUED ===");
                    Console.WriteLine("=== OIDC TOKENS ISSUED ===");
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        System.Diagnostics.Debug.WriteLine($"Access Token: {accessToken}");
                        Console.WriteLine($"Access Token: {accessToken}");
                    }
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        System.Diagnostics.Debug.WriteLine($"ID Token: {idToken}");
                        Console.WriteLine($"ID Token: {idToken}");
                    }
                    System.Diagnostics.Debug.WriteLine("==========================");
                    Console.WriteLine("==========================");
                }
                return default;
            });
        });
    })

    // Validation: for protecting the userinfo endpoint
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ──────────────────────────────────────────────
// 3. Authentication — IdP session cookie
// ──────────────────────────────────────────────
builder.Services.AddAuthentication()
    .AddCookie("idp-session", options =>
    {
        options.Cookie.Name = ".idp.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/Account/Login";
    });

// ──────────────────────────────────────────────
// 4. Services
// ──────────────────────────────────────────────
builder.Services.AddSingleton<TotpService>();
builder.Services.AddScoped<ClaimsBuilder>();
builder.Services.AddScoped<BackchannelLogoutDispatcher>();
builder.Services.AddHttpClient("backchannel")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Allow self-signed certs in development
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddControllers();
builder.Services.AddRazorPages();

// ──────────────────────────────────────────────
// 5. CORS — allow BFF and frontend origins
// ──────────────────────────────────────────────



builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
    //options.AddDefaultPolicy(policy =>
    //{
    //    policy.WithOrigins(
    //            "https://localhost:4300",  // Demo UI
    //            "https://localhost:5004",  // Demo BFF
    //            "https://10.176.100.10:4200",
    //            "https://10.176.100.10:5005")  
    //        .AllowAnyHeader()
    //        .AllowAnyMethod()
    //        .AllowCredentials();
    //});
});

// ──────────────────────────────────────────────
// 6. Seed data
// ──────────────────────────────────────────────
builder.Services.AddHostedService<SeedData>();

var app = builder.Build();

// ──────────────────────────────────────────────
// Middleware pipeline
// ──────────────────────────────────────────────
app.UseCors();
app.UseStaticFiles(); // Serve Angular login UI from wwwroot

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuthService.IdP" }));

app.Run();

RSA GetOrCreateRsaKey(string filename)
{
    var path = Path.Combine(AppContext.BaseDirectory, filename);
    var rsa = RSA.Create(2048);
    if (File.Exists(path))
    {
        rsa.ImportFromPem(File.ReadAllText(path));
    }
    else
    {
        File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
    }
    return rsa;
}
