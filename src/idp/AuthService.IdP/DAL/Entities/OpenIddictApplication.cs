using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Index("ClientId", Name = "IX_OpenIddictApplications_ClientId", IsUnique = true)]
public partial class OpenIddictApplication
{
    [Key]
    public string Id { get; set; } = null!;

    [StringLength(50)]
    public string? ApplicationType { get; set; }

    [StringLength(100)]
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    [StringLength(50)]
    public string? ClientType { get; set; }

    [StringLength(50)]
    public string? ConcurrencyToken { get; set; }

    [StringLength(50)]
    public string? ConsentType { get; set; }

    public string? DisplayName { get; set; }

    public string? DisplayNames { get; set; }

    public string? JsonWebKeySet { get; set; }

    public string? Permissions { get; set; }

    public string? PostLogoutRedirectUris { get; set; }

    public string? Properties { get; set; }

    public string? RedirectUris { get; set; }

    public string? Requirements { get; set; }

    public string? Settings { get; set; }

    [InverseProperty("Application")]
    public virtual ICollection<OpenIddictAuthorization> OpenIddictAuthorizations { get; set; } = new List<OpenIddictAuthorization>();

    [InverseProperty("Application")]
    public virtual ICollection<OpenIddictToken> OpenIddictTokens { get; set; } = new List<OpenIddictToken>();
}
