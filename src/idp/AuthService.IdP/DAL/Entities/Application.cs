using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("applications", Schema = "master")]
public partial class Application
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(50)]
    public string Title { get; set; } = null!;

    [Column("url", TypeName = "character varying")]
    public string Url { get; set; } = null!;

    [Column("key")]
    [StringLength(300)]
    public string Key { get; set; } = null!;

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("logo_url", TypeName = "character varying")]
    public string? LogoUrl { get; set; }

    [Column("application_admin_mobile_number")]
    [StringLength(10)]
    public string? ApplicationAdminMobileNumber { get; set; }

    [Column("application_admin_email")]
    [StringLength(100)]
    public string? ApplicationAdminEmail { get; set; }

    [Column("is_under_maintenance")]
    public bool IsUnderMaintenance { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("maintenance_msg")]
    public string? MaintenanceMsg { get; set; }

    [Column("_is_multi_admin_disallowed")]
    public bool? IsMultiAdminDisallowed { get; set; }

    [Column("base_url")]
    [StringLength(200)]
    public string? BaseUrl { get; set; }

    [Column("is_consuming_data")]
    public bool IsConsumingData { get; set; }

    [Column("is_use_user_management")]
    public bool IsUseUserManagement { get; set; }

    [InverseProperty("App")]
    public virtual ICollection<ApplicationLevel> ApplicationLevels { get; set; } = new List<ApplicationLevel>();

    [InverseProperty("App")]
    public virtual ICollection<ApplicationScope> ApplicationScopes { get; set; } = new List<ApplicationScope>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Applications")]
    public virtual UserMaster? CreatedByNavigation { get; set; }

    [InverseProperty("Application")]
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    [InverseProperty("Application")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    [InverseProperty("App")]
    public virtual ICollection<UserApplicationHasUserRole> UserApplicationHasUserRoles { get; set; } = new List<UserApplicationHasUserRole>();

    [InverseProperty("App")]
    public virtual ICollection<UserHasApplication> UserHasApplications { get; set; } = new List<UserHasApplication>();

    [InverseProperty("AssignedApp")]
    public virtual ICollection<UserHasModuleManagement> UserHasModuleManagements { get; set; } = new List<UserHasModuleManagement>();

    [InverseProperty("AssignedApp")]
    public virtual ICollection<UserHasUserManagement> UserHasUserManagements { get; set; } = new List<UserHasUserManagement>();

    [InverseProperty("OwnApp")]
    public virtual ICollection<UserRoleHasOwnApp> UserRoleHasOwnApps { get; set; } = new List<UserRoleHasOwnApp>();

    [InverseProperty("Application")]
    public virtual ICollection<UserRoleScopeAppContext> UserRoleScopeAppContexts { get; set; } = new List<UserRoleScopeAppContext>();
}
