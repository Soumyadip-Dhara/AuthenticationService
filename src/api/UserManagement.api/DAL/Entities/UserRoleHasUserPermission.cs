using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_role_has_user_permission", Schema = "user")]
[Index("ApplicationHasRoleId", "RoleHasPermissionId", Name = "user_role_has_user_permission_application_has_role_id_role__key", IsUnique = true)]
public partial class UserRoleHasUserPermission
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_has_role_id")]
    public long ApplicationHasRoleId { get; set; }

    [Column("role_has_permission_id")]
    public int RoleHasPermissionId { get; set; }

    [ForeignKey("ApplicationHasRoleId")]
    [InverseProperty("UserRoleHasUserPermissions")]
    public virtual UserApplicationHasUserRole ApplicationHasRole { get; set; } = null!;

    [ForeignKey("RoleHasPermissionId")]
    [InverseProperty("UserRoleHasUserPermissions")]
    public virtual Permission RoleHasPermission { get; set; } = null!;
}
