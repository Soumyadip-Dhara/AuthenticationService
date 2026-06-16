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

        // Custom Profile Claims
        identity.AddClaim(new Claim("userid", user.UserName));
        identity.AddClaim(new Claim("nameid", user.Id.ToString()));
        identity.AddClaim(new Claim("phoneNumber", user.MobileNumber ?? ""));
        identity.AddClaim(new Claim("designation", user.Designation ?? ""));
        identity.AddClaim(new Claim("typ", "acc"));

        // Creator Username
        var creatorUser = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == user.CreatedBy);
        identity.AddClaim(new Claim("created_by", creatorUser?.UserName ?? ""));

        // FinYear Calculation (e.g. April 2026 -> 2627)
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;
        string finyear;
        if (month >= 4)
        {
            var yy = year % 100;
            var yyPlus1 = (year + 1) % 100;
            finyear = $"{yy:D2}{yyPlus1:D2}";
        }
        else
        {
            var yyMinus1 = (year - 1) % 100;
            var yy = year % 100;
            finyear = $"{yyMinus1:D2}{yy:D2}";
        }
        identity.AddClaim(new Claim("finyear", finyear));

        // Initialize Level/Scope Context Claims
        string levelName = "";
        string roleName = "";
        string scopeValue = "";
        string parentScopeValue = "";
        string optionalJson = "";

        string districtCode = "";
        string slsCode = "";
        string ddoCode = "";
        string treasCode = "";

        // Map client_id to business application Id
        var appId = MapClientIdToAppId(clientId);
        if (appId.HasValue)
        {
            var contextInfo = await (
                from uha in _idpDbContext1.UserHasApplications
                where uha.UserId == userId && uha.AppId == appId.Value
                from uahur in _idpDbContext1.UserApplicationHasUserRoles
                where uahur.UserHasAppId == uha.Id
                join r in _idpDbContext1.Roles on uahur.RoleId equals r.Id
                from urhul in _idpDbContext1.UserRoleHasUserLevels
                where urhul.ApplicationHasRoleId == uahur.Id
                join al in _idpDbContext1.ApplicationLevels on urhul.RoleHasLevelId equals al.AppLevelId
                join lm in _idpDbContext1.LevelMasters on al.LevelId equals lm.LevelId
                from ulhus in _idpDbContext1.UserLevelHasUserScopes
                where ulhus.UserRoleHasLevelId == urhul.Id
                join as_child in _idpDbContext1.ApplicationScopes on ulhus.UserLevelHasScopeId equals as_child.AppScopeId
                join sm in _idpDbContext1.ScopeMasters on as_child.ScopeId equals sm.ScopeId
                select new {
                    RoleName = r.Title,
                    LevelName = lm.LevelName,
                    ScopeValue = sm.Value,
                    AppScopeId = as_child.AppScopeId,
                    UserLevelHasUserScopeId = ulhus.Id
                }
            ).FirstOrDefaultAsync();

            if (contextInfo != null)
            {
                roleName = contextInfo.RoleName;
                levelName = contextInfo.LevelName;
                scopeValue = contextInfo.ScopeValue;

                // Parent scope
                var parentScopeVal = await (
                    from sr in _idpDbContext1.ScopeRelationships
                    where sr.ScopeId == contextInfo.AppScopeId && sr.ScopeId != sr.ParentScopeId
                    join as_parent in _idpDbContext1.ApplicationScopes on sr.ParentScopeId equals as_parent.AppScopeId
                    join parent_sm in _idpDbContext1.ScopeMasters on as_parent.ScopeId equals parent_sm.ScopeId
                    select parent_sm.Value
                ).FirstOrDefaultAsync();

                parentScopeValue = parentScopeVal ?? "";

                // Fetch optional json from app context
                var appCtx = await _idpDbContext1.UserRoleScopeAppContexts
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == appId.Value && ac.UserLevelHasScopeId == contextInfo.UserLevelHasUserScopeId);
                optionalJson = appCtx?.OptionalJson ?? "";

                // Populate code based on LevelName
                if (levelName.Equals("DDO", StringComparison.OrdinalIgnoreCase))
                {
                    ddoCode = scopeValue;
                }
                else if (levelName.Equals("TREASURY", StringComparison.OrdinalIgnoreCase))
                {
                    treasCode = scopeValue;
                }
                else if (levelName.Equals("District", StringComparison.OrdinalIgnoreCase) || levelName.Equals("DISTRICT", StringComparison.OrdinalIgnoreCase))
                {
                    districtCode = scopeValue;
                }
                else if (levelName.Equals("Scheme", StringComparison.OrdinalIgnoreCase))
                {
                    slsCode = scopeValue;
                }
            }

            // Fetch roles and permissions for this user and application
            var userHasApp = await _idpDbContext1.UserHasApplications
                .FirstOrDefaultAsync(u => u.UserId == userId && u.AppId == appId.Value);

            if (userHasApp != null)
            {
                var userRoles = await _idpDbContext1.UserApplicationHasUserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserHasAppId == userHasApp.Id)
                    .ToListAsync();

                var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

                var rolePermissions = await _idpDbContext1.RoleHasPermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                var userAppRoleIds = userRoles.Select(ur => ur.Id).ToList();
                var userSpecificPermissions = await _idpDbContext1.UserRoleHasUserPermissions
                    .Include(up => up.RoleHasPermission)
                    .Where(up => userAppRoleIds.Contains(up.ApplicationHasRoleId))
                    .Select(up => up.RoleHasPermission.Name)
                    .ToListAsync();

                var permissions = rolePermissions.Union(userSpecificPermissions).Distinct().ToList();
                var permissionsJson = System.Text.Json.JsonSerializer.Serialize(permissions);
                identity.AddClaim(new Claim("permissions", permissionsJson));
            }
        }

        // Parent Agency Code logic (taking first part of role name and appending scope value)
        string parentAgencyCode = "";
        if (!string.IsNullOrEmpty(roleName) && !string.IsNullOrEmpty(scopeValue))
        {
            var parts = roleName.Split('_');
            if (parts.Length > 0)
            {
                parentAgencyCode = parts[0] + scopeValue;
            }
        }

        // Add Context Claims
        identity.AddClaim(new Claim("role", roleName));
        identity.AddClaim(new Claim("level", levelName));
        identity.AddClaim(new Claim("scope", scopeValue));
        identity.AddClaim(new Claim("parent_scope", parentScopeValue));
        identity.AddClaim(new Claim("parentagencycode", parentAgencyCode));
        identity.AddClaim(new Claim("districtcode", districtCode));
        identity.AddClaim(new Claim("sls_code", slsCode));
        identity.AddClaim(new Claim("ddo_code", ddoCode));
        identity.AddClaim(new Claim("treas_code", treasCode));
        identity.AddClaim(new Claim("pti", ""));
        identity.AddClaim(new Claim("aid", ""));
        identity.AddClaim(new Claim("optional", optionalJson));

        var principal = new ClaimsPrincipal(identity);

        // Set scopes
        principal.SetScopes(scopes);

        // Set resource (audience) exactly to public domain
        principal.SetResources("https://wbifms.gov.in/");

        // Set destinations: which claims go into which tokens
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        // Log claims for debugger/console visibility
        System.Diagnostics.Debug.WriteLine("=== OIDC Claims Principal built ===");
        Console.WriteLine("=== OIDC Claims Principal built ===");
        foreach (var claim in principal.Claims)
        {
            var msg = $"Claim Type: {claim.Type}, Value: {claim.Value}";
            System.Diagnostics.Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }
        System.Diagnostics.Debug.WriteLine("===================================");
        Console.WriteLine("===================================");

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
            return 66; // Fallback to IFMS3-eBilling for testing PARTHA1978
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
            case "userid":
            case "nameid":
            case "typ":
            case "phoneNumber":
            case "designation":
            case "created_by":
            case "level":
            case "parent_scope":
            case "scope":
            case "parentagencycode":
            case "districtcode":
            case "sls_code":
            case "ddo_code":
            case "treas_code":
            case "pti":
            case "aid":
            case "finyear":
            case "optional":
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
