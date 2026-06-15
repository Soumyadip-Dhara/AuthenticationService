using System.Security.Claims;
using AuthService.IdP.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.IdP.Services;

/// <summary>
/// Builds the claims principal for OIDC token issuance.
/// Copies the sid from the IdP session into the OIDC claims,
/// which is the critical thread that makes back-channel logout work.
/// </summary>
public class ClaimsBuilder
{
    private readonly AuthService.IdP.DAL.IDPDBContext1 _idpDbContext1;

    public ClaimsBuilder(AuthService.IdP.DAL.IDPDBContext1 idpDbContext1)
    {
        _idpDbContext1 = idpDbContext1;
    }

    /// <summary>
    /// Build a ClaimsPrincipal for OIDC sign-in from the IdP session principal.
    /// The sid claim is copied from the IdP session → OIDC tokens,
    /// establishing the link used for back-channel logout fan-out.
    /// </summary>
    public async Task<ClaimsPrincipal> BuildAsync(
        ClaimsPrincipal sessionPrincipal,
        string clientId,
        IEnumerable<string> scopes)
    {
        var sub = sessionPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? sessionPrincipal.FindFirstValue(OpenIddictConstants.Claims.Subject)
            ?? throw new InvalidOperationException("No subject in session principal");

        var sid = sessionPrincipal.FindFirstValue("sid")
            ?? throw new InvalidOperationException("No sid in session principal");

        if (!long.TryParse(sub, out var userId))
        {
            throw new InvalidOperationException($"Invalid user subject format: {sub}");
        }

        var user = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException($"User {sub} not found");

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Core claims
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, sub));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.Name));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email ?? user.UserName));

        // Session ID — the thread that ties SSO logout together
        identity.AddClaim(new Claim("sid", sid));

        // Map client_id to business application Id
        var appId = MapClientIdToAppId(clientId);
        if (appId.HasValue)
        {
            // Fetch roles and permissions for this user and application
            var userHasApp = await _idpDbContext1.UserHasApplications
                .FirstOrDefaultAsync(u => u.UserId == userId && u.AppId == appId.Value);

            if (userHasApp != null)
            {
                var userRoles = await _idpDbContext1.UserApplicationHasUserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserHasAppId == userHasApp.Id)
                    .ToListAsync();

                var roles = userRoles.Select(ur => ur.Role.Title).Distinct().ToList();
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
                }

                // Retrieve permissions
                var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

                // 1. Role-based permissions
                var rolePermissions = await _idpDbContext1.RoleHasPermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                // 2. Specific assigned user-role permissions
                var userAppRoleIds = userRoles.Select(ur => ur.Id).ToList();
                var userSpecificPermissions = await _idpDbContext1.UserRoleHasUserPermissions
                    .Include(up => up.RoleHasPermission)
                    .Where(up => userAppRoleIds.Contains(up.ApplicationHasRoleId))
                    .Select(up => up.RoleHasPermission.Name)
                    .ToListAsync();

                var permissions = rolePermissions.Union(userSpecificPermissions).Distinct().ToList();

                // Add permissions claim as JSON string
                var permissionsJson = System.Text.Json.JsonSerializer.Serialize(permissions);
                identity.AddClaim(new Claim("permissions", permissionsJson));
            }
        }

        var principal = new ClaimsPrincipal(identity);

        // Set scopes
        principal.SetScopes(scopes);

        // Set resources (audiences) based on scopes
        var scopesList = scopes.ToList();
        if (scopesList.Contains("api:demo"))
        {
            principal.SetResources("demo-api");
        }
        if (scopesList.Contains("api:mdm"))
        {
            principal.SetResources("mdm-api");
        }

        // Set destinations: which claims go into which tokens
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    private static int? MapClientIdToAppId(string clientId)
    {
        if (string.IsNullOrEmpty(clientId)) return null;

        if (clientId.Contains("mdm", StringComparison.OrdinalIgnoreCase))
        {
            return 68; // MasterDataManagement
        }
        if (clientId.Contains("cts", StringComparison.OrdinalIgnoreCase))
        {
            return 64; // CTS
        }
        if (clientId.Contains("ebilling", StringComparison.OrdinalIgnoreCase))
        {
            return 66; // IFMS3-eBilling
        }
        if (clientId.Contains("ebantan", StringComparison.OrdinalIgnoreCase))
        {
            return 67; // IFMS3-eBantan
        }
        if (clientId.Contains("demo", StringComparison.OrdinalIgnoreCase))
        {
            return 64; // Fallback to CTS for demo
        }

        return null;
    }

    /// <summary>
    /// Determines which token types each claim should be included in.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.Role:
            case "permissions":
            case "sid":
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Scopes.OpenId))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                break;
        }
    }
}
