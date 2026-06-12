using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("scope_relationships", Schema = "master")]
public partial class ScopeRelationship
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("scope_id")]
    public long? ScopeId { get; set; }

    [Column("parent_scope_id")]
    public long? ParentScopeId { get; set; }

    [Column("own_scope_level_id")]
    public int? OwnScopeLevelId { get; set; }

    [Column("parent_scope_level_id")]
    public int? ParentScopeLevelId { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [ForeignKey("ParentScopeId")]
    [InverseProperty("ScopeRelationshipParentScopes")]
    public virtual ApplicationScope? ParentScope { get; set; }

    [ForeignKey("ScopeId")]
    [InverseProperty("ScopeRelationshipScopes")]
    public virtual ApplicationScope? Scope { get; set; }
}
