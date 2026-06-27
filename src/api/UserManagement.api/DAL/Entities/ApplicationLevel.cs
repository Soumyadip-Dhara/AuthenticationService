using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("application_level", Schema = "master")]
[Index("AppId", "LevelId", Name = "application_level_app_id_level_id_key", IsUnique = true)]
public partial class ApplicationLevel
{
    [Key]
    [Column("app_level_id")]
    public int AppLevelId { get; set; }

    [Column("app_id")]
    public int? AppId { get; set; }

    [Column("level_id")]
    public int? LevelId { get; set; }

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

    [Column("referenced_global_id")]
    public int? ReferencedGlobalId { get; set; }

    [ForeignKey("AppId")]
    [InverseProperty("ApplicationLevels")]
    public virtual Application? App { get; set; }

    [InverseProperty("Level")]
    public virtual ICollection<ApplicationScope> ApplicationScopes { get; set; } = new List<ApplicationScope>();

    [ForeignKey("LevelId")]
    [InverseProperty("ApplicationLevels")]
    public virtual LevelMaster? Level { get; set; }

    [InverseProperty("Level")]
    public virtual ICollection<LevelHasAllowedRole> LevelHasAllowedRoles { get; set; } = new List<LevelHasAllowedRole>();

    [InverseProperty("AccessLevel")]
    public virtual ICollection<LevelRelationship> LevelRelationshipAccessLevels { get; set; } = new List<LevelRelationship>();

    [InverseProperty("Level")]
    public virtual ICollection<LevelRelationship> LevelRelationshipLevels { get; set; } = new List<LevelRelationship>();

    [InverseProperty("Level")]
    public virtual ICollection<UserLevelHasUserScope> UserLevelHasUserScopes { get; set; } = new List<UserLevelHasUserScope>();

    [InverseProperty("RoleHasLevel")]
    public virtual ICollection<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; } = new List<UserRoleHasUserLevel>();
}
