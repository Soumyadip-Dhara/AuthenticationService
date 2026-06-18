using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_has_user_management", Schema = "user")]
[Index("UserId", "AssignedAppId", Name = "user_has_user_management_user_id_assigned_app_id_user_id1_a_key", IsUnique = true)]
public partial class UserHasUserManagement
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("assigned_app_id")]
    public int AssignedAppId { get; set; }

    [Column("assigned_um_role_id")]
    public int AssignedUmRoleId { get; set; }

    [ForeignKey("AssignedAppId")]
    [InverseProperty("UserHasUserManagements")]
    public virtual Application AssignedApp { get; set; } = null!;

    [ForeignKey("AssignedUmRoleId")]
    [InverseProperty("UserHasUserManagements")]
    public virtual Role AssignedUmRole { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserHasUserManagements")]
    public virtual UserMaster User { get; set; } = null!;
}
