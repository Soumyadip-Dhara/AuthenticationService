using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Index("Name", Name = "IX_OpenIddictScopes_Name", IsUnique = true)]
public partial class OpenIddictScope
{
    [Key]
    public string Id { get; set; } = null!;

    [StringLength(50)]
    public string? ConcurrencyToken { get; set; }

    public string? Description { get; set; }

    public string? Descriptions { get; set; }

    public string? DisplayName { get; set; }

    public string? DisplayNames { get; set; }

    [StringLength(200)]
    public string? Name { get; set; }

    public string? Properties { get; set; }

    public string? Resources { get; set; }
}
