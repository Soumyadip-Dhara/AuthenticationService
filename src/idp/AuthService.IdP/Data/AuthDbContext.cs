using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.Data;

/// <summary>
/// EF Core context for the Identity Provider.
/// Includes OpenIddict entity sets.
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("public");
    }
}
