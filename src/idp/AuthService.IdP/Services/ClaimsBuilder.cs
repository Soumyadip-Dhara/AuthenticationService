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
    private readonly AuthDbContext _dbContext;

    public ClaimsBuilder(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == sub)
            ?? throw new InvalidOperationException($"User {sub} not found");

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Core claims
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, sub));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.DisplayName));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));

        // Session ID — the thread that ties SSO logout together
        identity.AddClaim(new Claim("sid", sid));

        var principal = new ClaimsPrincipal(identity);

        // Set scopes
        principal.SetScopes(scopes);

        // Set resources (audiences) based on scopes
        var scopesList = scopes.ToList();
        if (scopesList.Contains("api:demo"))
        {
            principal.SetResources("demo-api");
        }

        // Set destinations: which claims go into which tokens
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    /// <summary>
    /// Determines which token types each claim should be included in.
    /// - sub, name, email, sid → both access_token and id_token
    /// - Other claims → access_token only
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
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
