using UserManagement.api.DAL.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[Table("application_scope", Schema = "master")]
public partial class ApplicationScope
{
    [Key]
    [Column("app_scope_id")]
    public int AppScopeId { get; set; }

    [Column("app_id")]
    public int AppId { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("scope_id")]
    public int ScopeId { get; set; }

    [Column("is_admin_created")]
    public bool IsAdminCreated { get; set; }

    [Column("created_at", TypeName = "time without time zone")]
    public TimeSpan CreatedAt { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("status")]
    public short Status { get; set; }

    [ForeignKey("AppId")]
    [InverseProperty("ApplicationScopes")]
    public virtual Application? App { get; set; }

    [ForeignKey("LevelId")]
    [InverseProperty("ApplicationScopes")]
    public virtual ApplicationLevel? AppLevel { get; set; }

    [ForeignKey("ScopeId")]
    [InverseProperty("ApplicationScopes")]
    public virtual ScopeMaster? Scope { get; set; }

    [InverseProperty("AppScope")]
    public virtual ICollection<ScopeRelationship> ScopeRelationships { get; set; } = new List<ScopeRelationship>();

    [InverseProperty("ParentAppScope")]
    public virtual ICollection<ScopeRelationship> ParentScopeRelationships { get; set; } = new List<ScopeRelationship>();
}
