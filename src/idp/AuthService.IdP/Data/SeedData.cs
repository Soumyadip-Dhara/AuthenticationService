using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthService.IdP.DAL;
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

    private static async Task SeedClientsAsync(IServiceProvider provider, CancellationToken ct)
    {
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();

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
                new Uri("https://localhost:5004/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:4300/"),
                new Uri("https://localhost:5004/signout-callback-oidc")
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

        var existingMdmBff = await manager.FindByClientIdAsync("mdm-bff", ct);
        if (existingMdmBff is not null)
        {
            await manager.DeleteAsync(existingMdmBff, ct);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "mdm-bff",
            ClientSecret = "mdm-bff-secret",
            DisplayName = "MDM BFF Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,

            RedirectUris =
            {
                new Uri("https://localhost:5005/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:4200/"),
                new Uri("https://localhost:5005/signout-callback-oidc")
            },

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
                OpenIddictConstants.Permissions.Prefixes.Scope + "api:mdm"
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

        // --- MDM API (resource server for introspection) ---
        var existingMdmApi = await manager.FindByClientIdAsync("mdm-api", ct);
        if (existingMdmApi is not null)
        {
            await manager.DeleteAsync(existingMdmApi, ct);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "mdm-api",
            ClientSecret = "mdm-api-secret",
            DisplayName = "MDM API Resource Server",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,

            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Introspection
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

        if (await manager.FindByNameAsync("api:mdm", ct) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api:mdm",
                DisplayName = "MDM API Access",
                Resources =
                {
                    "mdm-api"
                }
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


