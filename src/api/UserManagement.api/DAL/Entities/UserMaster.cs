using UserManagement.api.DAL.Entities;
using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.api.DAL.Entities;

[Table("user_master", Schema = "user")]
[Index("UserName", Name = "user_master_user_name_user_name1_key", IsUnique = true)]
public partial class UserMaster
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_name")]
    [StringLength(100)]
    public string UserName { get; set; } = null!;

    [Column("hrms_id")]
    [StringLength(50)]
    public string? HrmsId { get; set; }

    [Column("name")]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Column("password_hash")]
    public byte[] PasswordHash { get; set; } = null!;

    [Column("password_salt")]
    public byte[] PasswordSalt { get; set; } = null!;

    [Column("designation", TypeName = "character varying")]
    public string Designation { get; set; } = null!;

    [Column("mobile_number")]
    [StringLength(10)]
    public string MobileNumber { get; set; } = null!;

    [Column("email")]
    [StringLength(50)]
    public string? Email { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("effective_from")]
    public DateOnly EffectiveFrom { get; set; }

    [Column("expires_on")]
    public DateOnly ExpiresOn { get; set; }

    [Column("unsuccessful_login_attempt")]
    public short UnsuccessfulLoginAttempt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_an_admin")]
    public bool IsAnAdmin { get; set; }

    [Column("is_blocked")]
    public bool IsBlocked { get; set; }

    [Column("old_id")]
    public long OldId { get; set; }

    [Column("due_first_login")]
    public bool DueFirstLogin { get; set; }

    [Column("block_time")]
    public TimeOnly? BlockTime { get; set; }

    [Column("signer_id")]
    [StringLength(100)]
    public string SignerId { get; set; } = null!;

    [Column("totp_secret")]
    [StringLength(500)]
    public string? TotpSecret { get; set; }

    [Column("totp_enabled")]
    public bool? TotpEnabled { get; set; }

    [Column("totp_verified_at")]
    public DateTime? TotpVerifiedAt { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual UserMaster CreatedByNavigation { get; set; } = null!;

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<UserMaster> InverseCreatedByNavigation { get; set; } = new List<UserMaster>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    [InverseProperty("User")]
    public virtual ICollection<PasswordChangeLog> PasswordChangeLogs { get; set; } = new List<PasswordChangeLog>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    [InverseProperty("User")]
    public virtual ICollection<UserActivityLog> UserActivityLogs { get; set; } = new List<UserActivityLog>();

    [InverseProperty("User")]
    public virtual ICollection<UserHasApplication> UserHasApplications { get; set; } = new List<UserHasApplication>();

    [InverseProperty("User")]
    public virtual ICollection<UserHasModuleManagement> UserHasModuleManagements { get; set; } = new List<UserHasModuleManagement>();

    [InverseProperty("User")]
    public virtual ICollection<UserHasUserManagement> UserHasUserManagements { get; set; } = new List<UserHasUserManagement>();
}

