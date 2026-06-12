using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("user_application_has_user_role", Schema = "user")]
[Index("UserHasAppId", "RoleId", Name = "user_application_has_user_rol_user_has_app_id_role_id_user__key", IsUnique = true)]
public partial class UserApplicationHasUserRole
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_has_app_id")]
    public long UserHasAppId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("app_id")]
    public int AppId { get; set; }

    [ForeignKey("AppId")]
    [InverseProperty("UserApplicationHasUserRoles")]
    public virtual Application App { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("UserApplicationHasUserRoles")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("UserHasAppId")]
    [InverseProperty("UserApplicationHasUserRoles")]
    public virtual UserHasApplication UserHasApp { get; set; } = null!;

    [InverseProperty("UserAppHasRole")]
    public virtual ICollection<UserRoleHasOwnApp> UserRoleHasOwnApps { get; set; } = new List<UserRoleHasOwnApp>();

    [InverseProperty("ApplicationHasRole")]
    public virtual ICollection<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; } = new List<UserRoleHasUserLevel>();

    [InverseProperty("ApplicationHasRole")]
    public virtual ICollection<UserRoleHasUserPermission> UserRoleHasUserPermissions { get; set; } = new List<UserRoleHasUserPermission>();
}
