using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthService.IdP.DAL;
using AuthService.IdP.DAL.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.IdP.Data;

/// <summary>
/// Hosted service that ensures the database and OpenIddict tables exist,
/// and seeds client/scope configurations.
/// Runs once at startup.
/// </summary>
public class SeedData : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public SeedData(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
        try
        {
            // If the database already existed (due to business tables), EnsureCreatedAsync won't create OpenIddict tables.
            // We force create them using the Database Creator if they do not exist.
            var creator = (IRelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();
            await creator.CreateTablesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateTablesAsync threw an exception (possibly because some tables already exist)");
        }

        await SeedClientsAsync(scope.ServiceProvider, cancellationToken);
        await SeedScopesAsync(scope.ServiceProvider, cancellationToken);
    }

    private static string GetAppCode(string title)
    {
        if (title.Equals("MasterDataManagement", StringComparison.OrdinalIgnoreCase))
            return "mdm";

        var code = title.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");
        return code;
    }

    private static int GetBffPort(int appId)
    {
        return appId switch
        {
            1 => 5003, // User Management
            5 => 5006, // Module Management
            63 => 5012, // WBJIT Billing
            64 => 5007, // CTS
            65 => 5008, // IFMS3-CTS
            66 => 5010, // IFMS3-eBilling
            67 => 5009, // IFMS3-eBantan
            68 => 5005, // MasterDataManagement (mdm)
            69 => 5011, // WBJIT
            _ => 5000 + appId
        };
    }

    private static int GetApiPort(int appId)
    {
        return appId switch
        {
            1 => 6003, // User Management
            5 => 6006, // Module Management
            63 => 6012, // WBJIT Billing
            64 => 6007, // CTS
            65 => 6008, // IFMS3-CTS
            66 => 6010, // IFMS3-eBilling
            67 => 6009, // IFMS3-eBantan
            68 => 6005, // MasterDataManagement (mdm)
            69 => 6011, // WBJIT
            _ => 6000 + appId
        };
    }

    private static int GetUiPort(int appId)
    {
        return appId switch
        {
            1 => 4100, // User Management
            5 => 4500, // Module Management
            63 => 5300, // WBJIT Billing
            64 => 4700, // CTS
            65 => 4800, // IFMS3-CTS
            66 => 5100, // IFMS3-eBilling
            67 => 4900, // IFMS3-eBantan
            68 => 4200, // MasterDataManagement (mdm)
            69 => 5200, // WBJIT
            _ => 4000 + appId
        };
    }

    private static async Task SeedClientsAsync(IServiceProvider provider, CancellationToken ct)
    {
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        var config = provider.GetRequiredService<IConfiguration>();
        var bffHost = config["Deployment:BffHost"]?.TrimEnd('/') ?? "https://10.176.100.10";
        var uiHost = config["Deployment:UiHost"]?.TrimEnd('/') ?? "https://10.176.100.10";

        // --- Demo Login BFF (confidential client) ---
        var existingBff = await manager.FindByClientIdAsync("demo-login-bff", ct);
        if (existingBff is not null)
        {
            await manager.DeleteAsync(existingBff, ct);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "demo-login-bff",
            ClientSecret = "demo-login-bff-secret",
            DisplayName = "Demo Login BFF",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,

            RedirectUris =
            {
                new Uri("https://localhost:5004/signin-oidc") // Demo BFF redirect URI (dev)
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:4300/"),            // Demo UI post-logout redirect (dev)
                new Uri("https://localhost:5004/signout-callback-oidc") // Demo BFF signout callback (dev)
            },

            Permissions =
            {
                // Endpoints
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,

                // Grant types
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                // Response type
                OpenIddictConstants.Permissions.ResponseTypes.Code,

                // Scopes
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api:demo"
            },

            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            },

            Settings =
            {
                [OpenIddictConstants.Settings.TokenLifetimes.AccessToken] = "00:15:00",
                [OpenIddictConstants.Settings.TokenLifetimes.RefreshToken] = "14.00:00:00"
            }
        }, ct);

        // --- Demo API (resource server for introspection) ---
        var existingApi = await manager.FindByClientIdAsync("demo-api", ct);
        if (existingApi is not null)
        {
            await manager.DeleteAsync(existingApi, ct);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "demo-api",
            ClientSecret = "demo-api-secret",
            DisplayName = "Demo API Resource Server",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,

            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Introspection
            }
        }, ct);

        // --- Load and dynamically register all applications from master.applications ---
        var idpContext = provider.GetRequiredService<IDPDBContext1>();
        var dbApps = await idpContext.Applications.Where(a => a.IsActive).ToListAsync(ct);

        foreach (var app in dbApps)
        {
            var code = GetAppCode(app.Title);
            
            // Register/Update BFF Client
            var bffClientId = $"{code}-bff";
            var bffClientSecret = $"{code}-bff-secret";
            var bffDisplayName = $"{app.Title} BFF Client";

            var existingDbBff = await manager.FindByClientIdAsync(bffClientId, ct);
            if (existingDbBff is not null)
            {
                await manager.DeleteAsync(existingDbBff, ct);
            }

            var bffDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = bffClientId,
                ClientSecret = bffClientSecret,
                DisplayName = bffDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,

                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,

                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                    OpenIddictConstants.Permissions.ResponseTypes.Code,

                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + $"api:{code}"
                },

                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                },

                Settings =
                {
                    [OpenIddictConstants.Settings.TokenLifetimes.AccessToken] = "00:15:00",
                    [OpenIddictConstants.Settings.TokenLifetimes.RefreshToken] = "14.00:00:00"
                }
            };

            var bffPort = GetBffPort(app.Id);
            // var bffPort = 5145;
            var uiPort = GetUiPort(app.Id);

            // Add local dev redirect & logout URIs
            bffDescriptor.RedirectUris.Add(new Uri($"{bffHost}:{bffPort}/signin-oidc"));   // {app.Title} BFF redirect URI (dev)
            bffDescriptor.RedirectUris.Add(new Uri($"{uiHost}:{uiPort}/signin-oidc"));    // {app.Title} UI redirect URI (dev)

            bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{uiHost}:{uiPort}/"));                          // {app.Title} UI post-logout (dev)
            bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{bffHost}:{bffPort}/signout-callback-oidc"));    // {app.Title} BFF signout callback (dev)
            bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{uiHost}:{uiPort}/signout-callback-oidc"));     // {app.Title} UI signout callback (dev)

            // Parse URL from database to add production/UAT redirect & logout URIs
            if (!string.IsNullOrEmpty(app.Url) && Uri.TryCreate(app.Url, UriKind.Absolute, out var uri))
            {
                var schemeAndServer = $"{uri.Scheme}://{uri.Authority}";
                var segments = uri.Segments;
                var firstSegment = segments.Length > 1 ? segments[1].TrimEnd('/') : "";
                
                var uiBaseUrl = string.IsNullOrEmpty(firstSegment) 
                    ? schemeAndServer 
                    : $"{schemeAndServer}/{firstSegment}";
                    
                var bffBaseUrl = $"{uiBaseUrl}-bff";

                bffDescriptor.RedirectUris.Add(new Uri($"{bffBaseUrl}/signin-oidc"));
                bffDescriptor.RedirectUris.Add(new Uri($"{uiBaseUrl}/signin-oidc"));

                bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{uiBaseUrl}/"));
                bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{bffBaseUrl}/signout-callback-oidc"));
                bffDescriptor.PostLogoutRedirectUris.Add(new Uri($"{uiBaseUrl}/signout-callback-oidc"));
            }

            await manager.CreateAsync(bffDescriptor, ct);

            // Register/Update API Resource Server
            var apiClientId = $"{code}-api";
            var apiClientSecret = $"{code}-api-secret";
            var apiDisplayName = $"{app.Title} API Resource Server";

            var existingDbApi = await manager.FindByClientIdAsync(apiClientId, ct);
            if (existingDbApi is not null)
            {
                await manager.DeleteAsync(existingDbApi, ct);
            }

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = apiClientId,
                ClientSecret = apiClientSecret,
                DisplayName = apiDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Confidential,

                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Introspection
                }
            }, ct);

            // Register/Update Swagger Client
            var swaggerClientId = $"{code}-swagger";
            var swaggerDisplayName = $"{app.Title} Swagger UI";

            var existingDbSwagger = await manager.FindByClientIdAsync(swaggerClientId, ct);
            if (existingDbSwagger is not null)
            {
                await manager.DeleteAsync(existingDbSwagger, ct);
            }

            var apiPort = GetApiPort(app.Id);

            var swaggerDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = swaggerClientId,
                DisplayName = swaggerDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,

                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + $"api:{code}"
                },

                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                },
                
                RedirectUris = 
                {
                    new Uri($"https://localhost:{apiPort}/swagger/oauth2-redirect.html"),
                    new Uri($"http://localhost:{apiPort}/swagger/oauth2-redirect.html"),
                    new Uri($"https://10.176.100.90:{apiPort}/swagger/oauth2-redirect.html"),
                    new Uri($"http://10.176.100.90:{apiPort}/swagger/oauth2-redirect.html")
                }
            };

            await manager.CreateAsync(swaggerDescriptor, ct);
        }
    }

    private static async Task SeedScopesAsync(IServiceProvider provider, CancellationToken ct)
    {
        var manager = provider.GetRequiredService<IOpenIddictScopeManager>();

        if (await manager.FindByNameAsync("api:demo", ct) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api:demo",
                DisplayName = "Demo API Access",
                Resources =
                {
                    "demo-api"
                }
            }, ct);
        }

        var idpContext = provider.GetRequiredService<IDPDBContext1>();
        var dbApps = await idpContext.Applications.Where(a => a.IsActive).ToListAsync(ct);

        foreach (var app in dbApps)
        {
            var code = GetAppCode(app.Title);
            var scopeName = $"api:{code}";
            var resourceServerName = $"{code}-api";

            var existingScope = await manager.FindByNameAsync(scopeName, ct);
            if (existingScope is not null)
            {
                await manager.DeleteAsync(existingScope, ct);
            }

            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = scopeName,
                DisplayName = $"{app.Title} API Access",
                Resources =
                {
                    resourceServerName
                }
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


