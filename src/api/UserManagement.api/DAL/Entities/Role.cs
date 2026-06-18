using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("roles", Schema = "master")]
[Index("ApplicationId", Name = "fki_roles_application_fkey")]
public partial class Role
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(50)]
    public string Title { get; set; } = null!;

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("is_operational")]
    public bool? IsOperational { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("Roles")]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("Roles")]
    public virtual UserMaster CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Role")]
    public virtual ICollection<LevelHasAllowedRole> LevelHasAllowedRoles { get; set; } = new List<LevelHasAllowedRole>();

    [InverseProperty("Role")]
    public virtual ICollection<RoleHasPermission> RoleHasPermissions { get; set; } = new List<RoleHasPermission>();

    [InverseProperty("AccessRole")]
    public virtual ICollection<RoleRelationship> RoleRelationshipAccessRoles { get; set; } = new List<RoleRelationship>();

    [InverseProperty("Role")]
    public virtual ICollection<RoleRelationship> RoleRelationshipRoles { get; set; } = new List<RoleRelationship>();

    [InverseProperty("Role")]
    public virtual ICollection<UserApplicationHasUserRole> UserApplicationHasUserRoles { get; set; } = new List<UserApplicationHasUserRole>();

    [InverseProperty("AssignedMmRole")]
    public virtual ICollection<UserHasModuleManagement> UserHasModuleManagements { get; set; } = new List<UserHasModuleManagement>();

    [InverseProperty("AssignedUmRole")]
    public virtual ICollection<UserHasUserManagement> UserHasUserManagements { get; set; } = new List<UserHasUserManagement>();

    [InverseProperty("OwnAppRole")]
    public virtual ICollection<UserRoleHasOwnApp> UserRoleHasOwnApps { get; set; } = new List<UserRoleHasOwnApp>();
}
