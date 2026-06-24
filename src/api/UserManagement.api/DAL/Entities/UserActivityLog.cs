using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_activity_log", Schema = "log")]
public partial class UserActivityLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("public_ip", TypeName = "character varying")]
    public string PublicIp { get; set; } = null!;

    [Column("private_ip", TypeName = "character varying")]
    public string PrivateIp { get; set; } = null!;

    [Column("is_login")]
    public bool IsLogin { get; set; }

    [Column("activity_time", TypeName = "timestamp without time zone")]
    public DateTime ActivityTime { get; set; }

    [Column("device")]
    [StringLength(50)]
    public string? Device { get; set; }

    [Column("agent")]
    [StringLength(50)]
    public string? Agent { get; set; }

    [Column("applications")]
    public long[]? Applications { get; set; }

    [Column("is_system_logout")]
    public bool? IsSystemLogout { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserActivityLogs")]
    public virtual UserMaster User { get; set; } = null!;

    [Column("session_id")]
    public Guid? SessionId { get; set; }
}
