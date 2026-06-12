using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("role_has_permission", Schema = "master")]
[Index("PermissionId", Name = "fki_role_has_permission_permission_fkey")]
[Index("RoleId", Name = "fki_role_has_permission_role_fkey")]
public partial class RoleHasPermission
{
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("PermissionId")]
    [InverseProperty("RoleHasPermissions")]
    public virtual Permission Permission { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("RoleHasPermissions")]
    public virtual Role Role { get; set; } = null!;
}
