using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Keyless]
[Table("user_master_200525", Schema = "user")]
public partial class UserMaster200525
{
    [Column("id")]
    public long? Id { get; set; }

    [Column("user_name")]
    [StringLength(100)]
    public string? UserName { get; set; }

    [Column("hrms_id")]
    [StringLength(50)]
    public string? HrmsId { get; set; }

    [Column("name")]
    [StringLength(200)]
    public string? Name { get; set; }

    [Column("password_hash")]
    public byte[]? PasswordHash { get; set; }

    [Column("password_salt")]
    public byte[]? PasswordSalt { get; set; }

    [Column("designation", TypeName = "character varying")]
    public string? Designation { get; set; }

    [Column("mobile_number")]
    [StringLength(10)]
    public string? MobileNumber { get; set; }

    [Column("email")]
    [StringLength(50)]
    public string? Email { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("effective_from")]
    public DateOnly? EffectiveFrom { get; set; }

    [Column("expires_on")]
    public DateOnly? ExpiresOn { get; set; }

    [Column("unsuccessful_login_attempt")]
    public short? UnsuccessfulLoginAttempt { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_only_usermanagement")]
    public bool? IsOnlyUsermanagement { get; set; }

    [Column("is_an_admin")]
    public bool? IsAnAdmin { get; set; }

    [Column("is_blocked")]
    public bool? IsBlocked { get; set; }

    [Column("old_id")]
    public long? OldId { get; set; }

    [Column("due_first_login")]
    public bool? DueFirstLogin { get; set; }

    [Column("block_time")]
    public TimeOnly? BlockTime { get; set; }
}
