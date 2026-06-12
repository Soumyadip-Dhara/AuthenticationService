using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Index("ApplicationId", "Status", "Subject", "Type", Name = "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type")]
[Index("AuthorizationId", Name = "IX_OpenIddictTokens_AuthorizationId")]
[Index("ReferenceId", Name = "IX_OpenIddictTokens_ReferenceId", IsUnique = true)]
public partial class OpenIddictToken
{
    [Key]
    public string Id { get; set; } = null!;

    public string? ApplicationId { get; set; }

    public string? AuthorizationId { get; set; }

    [StringLength(50)]
    public string? ConcurrencyToken { get; set; }

    public DateTime? CreationDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string? Payload { get; set; }

    public string? Properties { get; set; }

    public DateTime? RedemptionDate { get; set; }

    [StringLength(100)]
    public string? ReferenceId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    [StringLength(400)]
    public string? Subject { get; set; }

    [StringLength(50)]
    public string? Type { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("OpenIddictTokens")]
    public virtual OpenIddictApplication? Application { get; set; }

    [ForeignKey("AuthorizationId")]
    [InverseProperty("OpenIddictTokens")]
    public virtual OpenIddictAuthorization? Authorization { get; set; }
}
