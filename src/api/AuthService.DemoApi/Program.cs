using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// 1. OpenIddict Validation — Token introspection
// ──────────────────────────────────────────────
// Access tokens are JWE (encrypted) — this API cannot decode them locally.
// Instead, it sends them to the IdP's /connect/introspect endpoint
// using its own client credentials (demo-api / demo-api-secret).
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://10.176.100.17:5001/");
        options.UseIntrospection()
            .SetClientId("demo-api")
            .SetClientSecret("demo-api-secret");

        options.UseSystemNetHttp()
            .ConfigureHttpClientHandler(handler =>
            {
                // Accept self-signed certs in development
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            });

        options.UseAspNetCore();
    });

// ──────────────────────────────────────────────
// 2. Authentication & Authorization
// ──────────────────────────────────────────────
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// ──────────────────────────────────────────────
// 3. CORS — allow BFF origin
// ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5004") // Only BFF should call this API
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuthService.DemoApi" }));

app.Run();
