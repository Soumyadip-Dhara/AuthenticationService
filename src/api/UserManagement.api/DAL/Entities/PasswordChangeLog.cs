using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("password_change_log", Schema = "log")]
public partial class PasswordChangeLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("password_change_log", TypeName = "timestamp without time zone")]
    public DateTime PasswordChangeLog1 { get; set; }

    [Column("new_password_hash")]
    public byte[] NewPasswordHash { get; set; } = null!;

    [Column("salt")]
    public byte[] Salt { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PasswordChangeLogs")]
    public virtual UserMaster User { get; set; } = null!;
}
