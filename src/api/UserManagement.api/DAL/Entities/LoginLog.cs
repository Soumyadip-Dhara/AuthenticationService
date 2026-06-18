using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("login_log", Schema = "log")]
public partial class LoginLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("login_time", TypeName = "timestamp without time zone")]
    public DateTime LoginTime { get; set; }
    [Column("session_id")]
    public Guid? SessionId { get; set; }


}
