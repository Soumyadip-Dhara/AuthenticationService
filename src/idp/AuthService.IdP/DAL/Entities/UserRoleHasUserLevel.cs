using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("user_role_has_user_level", Schema = "user")]
[Index("ApplicationHasRoleId", "RoleHasLevelId", "UserHasAppId", Name = "user_role_has_user_level_application_has_role_id_role_has_l_key", IsUnique = true)]
public partial class UserRoleHasUserLevel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_has_role_id")]
    public long ApplicationHasRoleId { get; set; }

    [Column("role_has_level_id")]
    public int RoleHasLevelId { get; set; }

    [Column("user_has_app_id")]
    public long? UserHasAppId { get; set; }

    [ForeignKey("ApplicationHasRoleId")]
    [InverseProperty("UserRoleHasUserLevels")]
    public virtual UserApplicationHasUserRole ApplicationHasRole { get; set; } = null!;

    [ForeignKey("RoleHasLevelId")]
    [InverseProperty("UserRoleHasUserLevels")]
    public virtual ApplicationLevel RoleHasLevel { get; set; } = null!;

    [ForeignKey("UserHasAppId")]
    [InverseProperty("UserRoleHasUserLevels")]
    public virtual UserHasApplication? UserHasApp { get; set; }

    [InverseProperty("UserRoleHasLevel")]
    public virtual ICollection<UserLevelHasUserScope> UserLevelHasUserScopes { get; set; } = new List<UserLevelHasUserScope>();
}
