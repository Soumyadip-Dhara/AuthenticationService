using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("scope_master", Schema = "master")]
[Index("Value", Name = "scope_master_value_key", IsUnique = true)]
public partial class ScopeMaster
{
    [Key]
    [Column("scope_id")]
    public int ScopeId { get; set; }

    [Column("scope_name")]
    [StringLength(200)]
    public string ScopeName { get; set; } = null!;

    [Column("value", TypeName = "character varying")]
    public string Value { get; set; } = null!;

    [Column("is_global")]
    public bool IsGlobal { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [Column("level_id")]
    public int? LevelId { get; set; }

    [InverseProperty("Scope")]
    public virtual ICollection<ApplicationScope> ApplicationScopes { get; set; } = new List<ApplicationScope>();
}
