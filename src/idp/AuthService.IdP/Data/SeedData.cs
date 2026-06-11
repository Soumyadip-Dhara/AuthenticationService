using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthService.IdP.Data;

/// <summary>
/// Hosted service that seeds:
/// 1. OpenIddict client application registrations (demo-login-bff, demo-api)
/// 2. A default admin user for development
///
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

        await SeedClientsAsync(scope.ServiceProvider, cancellationToken);
        await SeedScopesAsync(scope.ServiceProvider, cancellationToken);
        await SeedUsersAsync(context, cancellationToken);
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
                // Back-channel logout URI — IdP will POST logout_token here
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
    }

    private static async Task SeedUsersAsync(AuthDbContext context, CancellationToken ct)
    {
        if (!await context.Users.AnyAsync(ct))
        {
            context.Users.Add(new ApplicationUser
            {
                Email = "admin@demo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                DisplayName = "Demo Admin"
            });
            await context.SaveChangesAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
