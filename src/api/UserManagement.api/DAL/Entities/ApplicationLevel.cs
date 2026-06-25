using UserManagement.DAL.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[Table("application_level", Schema = "master")]
public partial class ApplicationLevel
{
    [Key]
    [Column("app_level_id")]
    public int AppLevelId { get; set; }

    [Column("app_id")]
    public int? AppId { get; set; }

    [Column("level_id")]
    public int? LevelId { get; set; }

    [Column("referenced_global_id")]
    public int? GlobalId { get; set; }

    [Column("rank")]
    public int? Rank { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("same_level_other_office_admin_allowed")]
    public bool? SameLevelOtherOfficeAdminAllowed { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [ForeignKey("AppId")]
    [InverseProperty("ApplicationLevels")]
    public virtual Application? App { get; set; }

    [ForeignKey("LevelId")]
    [InverseProperty("ApplicationLevels")]
    public virtual LevelMaster? Level { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual UserMaster? CreatedByNavigation { get; set; }

    [InverseProperty("Level")]
    public virtual ICollection<UserLevelHasUserScope> UserLevelHasUserScopes { get; set; } = new List<UserLevelHasUserScope>();

    [InverseProperty("RoleHasLevel")]
    public virtual ICollection<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; } = new List<UserRoleHasUserLevel>();

    [InverseProperty("Level")]
    public virtual ICollection<LevelHasAllowedRole> LevelHasAllowedRoles { get; set; } = new List<LevelHasAllowedRole>();

    [InverseProperty("Level")]
    public virtual ICollection<LevelRelationship> LevelRelationshipLevels { get; set; } = new List<LevelRelationship>();

    [InverseProperty("AccessLevel")]
    public virtual ICollection<LevelRelationship> LevelRelationshipAccessLevels { get; set; } = new List<LevelRelationship>();

    [InverseProperty("AppLevel")]
    public virtual ICollection<ApplicationScope> ApplicationScopes { get; set; } = new List<ApplicationScope>();

    [InverseProperty("OwnScopeLevel")]
    public virtual ICollection<ScopeRelationship> ScopeRelationshipOwnScopeLevels { get; set; } = new List<ScopeRelationship>();

    [InverseProperty("ParentScopeLevel")]
    public virtual ICollection<ScopeRelationship> ScopeRelationshipParentScopeLevels { get; set; } = new List<ScopeRelationship>();
}
