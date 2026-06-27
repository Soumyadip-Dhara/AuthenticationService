using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("application_scope", Schema = "master")]
[Index("AppId", "LevelId", "ScopeId", Name = "application_scope_app_id_level_id_scope_id_key", IsUnique = true)]
public partial class ApplicationScope
{
    [Key]
    [Column("app_scope_id")]
    public long AppScopeId { get; set; }

    [Column("app_id")]
    public int AppId { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("scope_id")]
    public int ScopeId { get; set; }

    [Column("is_admin_created")]
    public bool IsAdminCreated { get; set; }

    [Column("created_at")]
    public TimeOnly CreatedAt { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [Column("status")]
    public short Status { get; set; }

    [ForeignKey("AppId")]
    [InverseProperty("ApplicationScopes")]
    public virtual Application App { get; set; } = null!;

    [ForeignKey("LevelId")]
    [InverseProperty("ApplicationScopes")]
    public virtual ApplicationLevel Level { get; set; } = null!;

    [ForeignKey("ScopeId")]
    [InverseProperty("ApplicationScopes")]
    public virtual ScopeMaster Scope { get; set; } = null!;

    [InverseProperty("ParentScope")]
    public virtual ICollection<ScopeRelationship> ScopeRelationshipParentScopes { get; set; } = new List<ScopeRelationship>();

    [InverseProperty("Scope")]
    public virtual ICollection<ScopeRelationship> ScopeRelationshipScopes { get; set; } = new List<ScopeRelationship>();
}
