using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("level_has_allowed_roles", Schema = "master")]
public partial class LevelHasAllowedRole
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("is_parent_or_admin_role")]
    public bool IsParentOrAdminRole { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [ForeignKey("LevelId")]
    [InverseProperty("LevelHasAllowedRoles")]
    public virtual ApplicationLevel Level { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("LevelHasAllowedRoles")]
    public virtual Role Role { get; set; } = null!;
}
