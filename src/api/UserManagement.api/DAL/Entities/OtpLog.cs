using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("otp_log", Schema = "log")]
public partial class OtpLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("otp_hash", TypeName = "character varying")]
    public string OtpHash { get; set; } = null!;

    [Column("salt", TypeName = "character varying")]
    public string Salt { get; set; } = null!;

    [Column("otp_type")]
    public short? OtpType { get; set; }

    [Column("username", TypeName = "character varying")]
    public string Username { get; set; } = null!;

    [Column("phone_number", TypeName = "character varying")]
    public string PhoneNumber { get; set; } = null!;

    [Column("timestamp", TypeName = "timestamp without time zone")]
    public DateTime Timestamp { get; set; }

    [Column("sms_response_status")]
    public short? SmsResponseStatus { get; set; }

    [Column("sms_response_message", TypeName = "character varying")]
    public string? SmsResponseMessage { get; set; }
}
