using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Index("ApplicationId", "Status", "Subject", "Type", Name = "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type")]
public partial class OpenIddictAuthorization
{
    [Key]
    public string Id { get; set; } = null!;

    public string? ApplicationId { get; set; }

    [StringLength(50)]
    public string? ConcurrencyToken { get; set; }

    public DateTime? CreationDate { get; set; }

    public string? Properties { get; set; }

    public string? Scopes { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    [StringLength(400)]
    public string? Subject { get; set; }

    [StringLength(50)]
    public string? Type { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("OpenIddictAuthorizations")]
    public virtual OpenIddictApplication? Application { get; set; }

    [InverseProperty("Authorization")]
    public virtual ICollection<OpenIddictToken> OpenIddictTokens { get; set; } = new List<OpenIddictToken>();
}
