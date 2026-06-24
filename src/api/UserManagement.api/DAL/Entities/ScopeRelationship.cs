using UserManagement.api.DAL.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[Table("scope_relationships", Schema = "master")]
public partial class ScopeRelationship
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("scope_id")]
    public int ScopeId { get; set; }

    [Column("parent_scope_id")]
    public int ParentScopeId { get; set; }    

    [Column("own_scope_level_id")]
    public int? OwnScopeLevelId { get; set; }

    [Column("parent_scope_level_id")]
    public int? ParentScopeLevelId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }


    [ForeignKey("OwnScopeLevelId")]
    [InverseProperty("ScopeRelationshipOwnScopeLevels")]
    public virtual ApplicationLevel? OwnScopeLevel { get; set; }

    [ForeignKey("ParentScopeLevelId")]
    [InverseProperty("ScopeRelationshipParentScopeLevels")]
    public virtual ApplicationLevel? ParentScopeLevel { get; set; }

    [ForeignKey("ScopeId")]
    [InverseProperty("ScopeRelationships")]
    public virtual ApplicationScope? AppScope { get; set; }

    [ForeignKey("ParentScopeId")]
    [InverseProperty("ParentScopeRelationships")]
    public virtual ApplicationScope? ParentAppScope { get; set; }
}
