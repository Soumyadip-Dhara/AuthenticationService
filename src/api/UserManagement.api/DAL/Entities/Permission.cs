using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("permissions", Schema = "master")]
[Index("ApplicationId", Name = "fki_permission_application_fkey")]
public partial class Permission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name", TypeName = "character varying")]
    public string Name { get; set; } = null!;

    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("Permissions")]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual UserMaster CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Permission")]
    public virtual ICollection<RoleHasPermission> RoleHasPermissions { get; set; } = new List<RoleHasPermission>();

    [InverseProperty("RoleHasPermission")]
    public virtual ICollection<UserRoleHasUserPermission> UserRoleHasUserPermissions { get; set; } = new List<UserRoleHasUserPermission>();
}

