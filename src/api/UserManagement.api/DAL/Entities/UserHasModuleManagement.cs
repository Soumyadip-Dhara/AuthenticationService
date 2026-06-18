using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_has_module_management", Schema = "user")]
[Index("UserId", "AssignedAppId", Name = "user_has_module_management_user_id_assigned_app_id_user_id1_key", IsUnique = true)]
public partial class UserHasModuleManagement
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("assigned_app_id")]
    public int AssignedAppId { get; set; }

    [Column("assigned_mm_role_id")]
    public int AssignedMmRoleId { get; set; }

    [ForeignKey("AssignedAppId")]
    [InverseProperty("UserHasModuleManagements")]
    public virtual Application AssignedApp { get; set; } = null!;

    [ForeignKey("AssignedMmRoleId")]
    [InverseProperty("UserHasModuleManagements")]
    public virtual Role AssignedMmRole { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserHasModuleManagements")]
    public virtual UserMaster User { get; set; } = null!;
}
