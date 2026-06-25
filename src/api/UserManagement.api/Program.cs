using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Services.MQueue;
using UserManagement.DAL;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.DAL.Repositories.MQueue;
using UserManagement.Extensions;
using UserManagement.Jwt.Auth;
using UserManagement.Models;
using UserManagement.RbbitMQ;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;
using UserMangement.BAL.Interfaces.MQueue;

var builder = WebApplication.CreateBuilder(args);
// Removed hardcoded Kestrel port to allow Docker dynamic port mapping

// Database Connection
builder.Services.AddDbContext<UserManagementDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserManagementDBConnection"),
    options => options.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
), ServiceLifetime.Transient);

// Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddAutoMapper(typeof(Program));

// Repositories
builder.Services.AddTransient<IMessageQueueRepository, MessageQueueRepository>();
builder.Services.AddTransient<IConsumeFailedLogRepository, ConsumeFailedLogRepository>();
builder.Services.AddTransient<IConsumeLogRepository, ConsumeLogRepository>();
builder.Services.AddTransient<IMessageQueueFailedLogsRepository, MessageQueueFailedLogsRepository>();
builder.Services.AddTransient<IConsumedAcknowledgementLogRepository, ConsumedAcknowledgementLogRepository>();
builder.Services.AddTransient<IPublishedAcknowledgementLogRepository, PublishedAcknowledgementLogRepository>();
builder.Services.AddTransient<IRabbitMQLogsRepository, RabbitMQLogsRepository>();
builder.Services.AddScoped<UserManagement.DAL.Interfaces.IUserRepository, UserManagement.DAL.Repositories.UserRepository>();
builder.Services.AddScoped<UserManagement.DAL.Interfaces.Master.IRoleRepository, UserManagement.DAL.Repositories.Master.RoleRepository>();
builder.Services.AddScoped<UserManagement.DAL.Interfaces.Master.IUserApplicationHasUserRoleRepository, UserManagement.DAL.Repositories.Master.UserApplicationHasUserRoleRepository>();
builder.Services.AddScoped<UserManagement.DAL.Interfaces.Master.ILevelHasAllowedRoleRepository, UserManagement.DAL.Repositories.Master.LevelHasAllowedRoleRepository>();

// RabbitMQ Registration
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddMessageProcessing();

// Services
builder.Services.AddTransient<IRabbitMQPublisherService, RabbitMQPublisherService>();
builder.Services.AddTransient<IMQueueProcessingService, MQueueProcessingService>();
builder.Services.AddTransient<IRabbitMqService, RabbitMqService>();
builder.Services.AddTransient<ILogsService, LogsService>();
builder.Services.AddScoped<UserManagement.BAL.Interfaces.IClaimService, UserManagement.BAL.Services.ClaimService>();
builder.Services.AddScoped<UserManagement.BAL.Interfaces.IUserService, UserManagement.BAL.Services.UserService>();
builder.Services.AddScoped<UserManagement.BAL.Interfaces.Master.IRoleService, UserManagement.BAL.Services.Master.RoleService>();





builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// ===========================
// Swagger
// ===========================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1"
    });
    
    // Resolve duplicate schema name errors for classes with the same name in different namespaces
    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            AuthorizationCode = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(builder.Configuration["Swagger:AuthorizationUrl"]),
                TokenUrl = new Uri(builder.Configuration["Swagger:TokenUrl"]),
                Scopes = new Dictionary<string, string>
                {
                    { "api:usermanagement", "User Management API Access" },
                    { "openid", "OpenID" },
                    { "profile", "Profile" },
                    { "roles", "Roles" },
                    {"email", "Email" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "api:usermanagement" }
        }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

// Add Memory Cache for Introspection
builder.Services.AddMemoryCache();

// Register the custom validation caching handlers in DI
builder.Services.AddSingleton<UserManagement.api.Authentication.IntrospectionCachingHandler>();
builder.Services.AddSingleton<UserManagement.api.Authentication.IntrospectionCacheSaver>();

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        var oidcConfig = builder.Configuration.GetSection("OpenIddict");
        options.SetIssuer(oidcConfig["Issuer"] ?? "https://10.176.100.17:5001/");
        options.AddAudiences(oidcConfig["ClientId"] ?? "usermanagement-api");

        // Configure Introspection (Disabled - using Local JWT Validation instead)
        options.UseIntrospection()
               .SetClientId(oidcConfig["ClientId"] ?? "usermanagement-api")
               .SetClientSecret(oidcConfig["ClientSecret"] ?? "usermanagement-api-secret");

        // Custom validation caching handlers
        options.AddEventHandler<OpenIddict.Validation.OpenIddictValidationEvents.ProcessAuthenticationContext>(builder =>
        {
            builder.UseSingletonHandler<UserManagement.api.Authentication.IntrospectionCachingHandler>()
                   .SetOrder(OpenIddict.Validation.OpenIddictValidationHandlers.ValidateAccessToken.Descriptor.Order - 1000);
        });

        options.AddEventHandler<OpenIddict.Validation.OpenIddictValidationEvents.HandleIntrospectionResponseContext>(builder =>
        {
            builder.UseSingletonHandler<UserManagement.api.Authentication.IntrospectionCacheSaver>()
                   .SetOrder(OpenIddict.Validation.OpenIddictValidationHandlers.Introspection.PopulateClaims.Descriptor.Order + 1000);
        });

        // Register the System.Net.Http integration (required for introspection).
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly)
               .ConfigureHttpClientHandler(handler =>
               {
                   handler.ServerCertificateCustomValidationCallback =
                       HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
               });

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Register OR-based policy provider (allows comma-separated policies)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AnyPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, AnyPolicyHandler>();

// Register custom HTTP message handler filter to redirect public issuer traffic locally
builder.Services.AddSingleton<Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter, RoutingHandlerFilter>();

// HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.OAuthClientId(builder.Configuration["Swagger:OAuthClientId"] ?? "usermanagement-swagger");
        c.OAuthUsePkce();
    });
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");
        await next();
    });
}

string frontEndUrl = builder.Configuration.GetSection("AllowedOrigins").Value ?? "https://ifms.wb.gov.in";

if (app.Environment.IsDevelopment())
{
    // In development, allow all origins
    app.UseCors(builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
}
else
{
    // In production, restrict to specific origin
    app.UseCors(builder => builder
        .WithOrigins(frontEndUrl)  // Allow only this origin
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}

app.UseRouting();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    Console.WriteLine($"\n[Diagnostic] Incoming Request: {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"[Diagnostic] Authorization Header: {(string.IsNullOrEmpty(authHeader) ? "MISSING" : authHeader.Substring(0, Math.Min(authHeader.Length, 30)) + "...")}");

    var user = context.User;
    Console.WriteLine($"[Diagnostic] IsAuthenticated: {user?.Identity?.IsAuthenticated}");
    if (user?.Identity?.IsAuthenticated == true)
    {
        Console.WriteLine($"[Diagnostic] Claims count: {user.Claims.Count()}");
        foreach (var claim in user.Claims)
        {
            Console.WriteLine($"[Diagnostic]   Claim: {claim.Type} = {claim.Value}");
        }
    }
    else
    {
        Console.WriteLine($"[Diagnostic] User is not authenticated.");
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();

public class LocalIssuerRoutingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host.Equals("wbifms.gov.in", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Host = "10.176.100.17",
                Port = 5001
            };
            request.RequestUri = builder.Uri;
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

public class RoutingHandlerFilter : Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter
{
    public Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> Configure(Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            builder.AdditionalHandlers.Add(new LocalIssuerRoutingHandler());
        };
    }
}
