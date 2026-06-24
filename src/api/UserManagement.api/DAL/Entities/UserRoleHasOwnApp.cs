using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_role_has_own_app", Schema = "user")]
public partial class UserRoleHasOwnApp
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_app_has_role_id")]
    public long UserAppHasRoleId { get; set; }

    [Column("own_app_role_id")]
    public int OwnAppRoleId { get; set; }

    [Column("own_app_id")]
    public int? OwnAppId { get; set; }

    [ForeignKey("OwnAppId")]
    [InverseProperty("UserRoleHasOwnApps")]
    public virtual Application? OwnApp { get; set; }

    [ForeignKey("OwnAppRoleId")]
    [InverseProperty("UserRoleHasOwnApps")]
    public virtual Role OwnAppRole { get; set; } = null!;

    [ForeignKey("UserAppHasRoleId")]
    [InverseProperty("UserRoleHasOwnApps")]
    public virtual UserApplicationHasUserRole UserAppHasRole { get; set; } = null!;
}
