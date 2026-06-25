using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_has_application", Schema = "user")]
[Index("UserId", "AppId", Name = "user_has_application_user_id_app_id_user_id1_app_id1_key", IsUnique = true)]
public partial class UserHasApplication
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("app_id")]
    public int AppId { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserHasApplications")]
    public virtual UserMaster User { get; set; } = null!;

    [InverseProperty("UserHasApp")]
    public virtual ICollection<UserApplicationHasUserRole> UserApplicationHasUserRoles { get; set; } = new List<UserApplicationHasUserRole>();

    [InverseProperty("UserHasApp")]
    public virtual ICollection<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; } = new List<UserRoleHasUserLevel>();
}
