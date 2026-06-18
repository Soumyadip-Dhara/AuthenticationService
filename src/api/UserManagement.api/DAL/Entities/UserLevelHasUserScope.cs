using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_level_has_user_scope", Schema = "user")]
[Index("UserRoleHasLevelId", "UserLevelHasScopeId", "LevelId", Name = "user_level_has_user_scope_user_role_has_level_id_user_level_key", IsUnique = true)]
public partial class UserLevelHasUserScope
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_role_has_level_id")]
    public long UserRoleHasLevelId { get; set; }

    [Column("user_level_has_scope_id")]
    public long UserLevelHasScopeId { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [ForeignKey("LevelId")]
    [InverseProperty("UserLevelHasUserScopes")]
    public virtual ApplicationLevel Level { get; set; } = null!;

    [ForeignKey("UserRoleHasLevelId")]
    [InverseProperty("UserLevelHasUserScopes")]
    public virtual UserRoleHasUserLevel UserRoleHasLevel { get; set; } = null!;
}
