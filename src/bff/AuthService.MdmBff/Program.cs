using System.IdentityModel.Tokens.Jwt;
using AuthService.MdmBff.Auth;
using AuthService.MdmBff.Endpoints;
using AuthService.Shared.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// 1. Shared services — ticket store + sid index
var ticketStore = new InMemoryTicketStore();
var sidIndex = new SidIndex();

builder.Services.AddSingleton(ticketStore);
builder.Services.AddSingleton(sidIndex);

// 2. Authentication — Cookie + OpenID Connect
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "cookie";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("cookie", options =>
{
    var config = builder.Configuration.GetSection("Session");
    options.Cookie.Name = config["CookieName"] ?? ".mdm.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    // Server-side session storage — the cookie only contains the session key
    options.SessionStore = ticketStore;

    // Return 401 for AJAX requests instead of redirecting to login
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect("oidc", options =>
{
    var config = builder.Configuration.GetSection("Oidc");

    options.Authority = config["Authority"] ?? "https://localhost:5001";
    options.ClientId = config["ClientId"] ?? "mdm-bff";
    options.ClientSecret = config["ClientSecret"] ?? "mdm-bff-secret";

    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true;

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false
    };

    // Request scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("api:mdm");
    options.Scope.Add("offline_access"); // for refresh tokens

    options.GetClaimsFromUserInfoEndpoint = true;

    // Callback paths
    options.CallbackPath = config["CallbackPath"] ?? "/signin-oidc";
    options.SignedOutCallbackPath = config["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
    options.RemoteSignOutPath = "/signout-oidc";

    // Where to redirect after OIDC sign-out completes
    var frontendUrl = builder.Configuration["FrontendUrl"] ?? "https://localhost:4200/";
    options.SignedOutRedirectUri = frontendUrl.EndsWith("/") ? frontendUrl : frontendUrl + "/";

    // Accept self-signed certs in development
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    options.Events = new OpenIdConnectEvents
    {
        // After tokens are validated, register the sid in our index
        OnTokenValidated = context =>
        {
            var sid = context.Principal?.FindFirst("sid")?.Value;
            if (!string.IsNullOrEmpty(sid) && context.Properties != null)
            {
                context.Properties.Items["sid"] = sid;
            }
            return Task.CompletedTask;
        },

        // Handle sign-out redirect
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            // Ensure id_token_hint is included for back-channel logout
            var idToken = context.Properties?.GetTokenValue("id_token");
            if (!string.IsNullOrEmpty(idToken))
            {
                context.ProtocolMessage.IdTokenHint = idToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 3. Override ticket store to capture sid→key mapping
builder.Services.PostConfigure<CookieAuthenticationOptions>("cookie", options =>
{
    var innerStore = ticketStore;
    options.SessionStore = new SidTrackingTicketStore(innerStore, sidIndex);
});

// 4. YARP Reverse Proxy — forwards /api/** to MDM.Api
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpClient("token-refresh")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

// 5. CORS — allow MDM UI origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for cookies
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseAuthentication();

// Token refresh middleware — silently refreshes expired tokens
app.UseMiddleware<TokenRefreshMiddleware>();

app.UseAuthorization();

// BFF endpoints
app.MapBffEndpoints();
app.MapBackchannelLogout();

// YARP reverse proxy for /api/** routes
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        // Get the access token from the server-side session
        var accessToken = await context.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Inject the token as an Authorization header to the upstream API
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        // Strip browser cookies — the API should not see them
        context.Request.Headers.Remove("Cookie");

        await next();
    });
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuthService.MdmBff" }));

app.Run();

internal class SidTrackingTicketStore : ITicketStore
{
    private readonly InMemoryTicketStore _inner;
    private readonly SidIndex _sidIndex;

    public SidTrackingTicketStore(InMemoryTicketStore inner, SidIndex sidIndex)
    {
        _inner = inner;
        _sidIndex = sidIndex;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = await _inner.StoreAsync(ticket);

        // Register the sid→sessionKey mapping
        string? sid = null;
        ticket.Properties?.Items.TryGetValue("sid", out sid);
        sid ??= ticket.Principal?.FindFirst("sid")?.Value;

        if (!string.IsNullOrEmpty(sid))
        {
            _sidIndex.AddSession(sid, key);
        }

        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => _inner.RenewAsync(key, ticket);
    public Task<AuthenticationTicket?> RetrieveAsync(string key) => _inner.RetrieveAsync(key);
    public Task RemoveAsync(string key) => _inner.RemoveAsync(key);
}
