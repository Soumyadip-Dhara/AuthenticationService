using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("otp")]
public partial class Otp
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("otp_value")]
    [StringLength(6)]
    public string OtpValue { get; set; } = null!;

    [Column("user_name", TypeName = "character varying")]
    public string UserName { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("otp_type")]
    public short OtpType { get; set; }
}
