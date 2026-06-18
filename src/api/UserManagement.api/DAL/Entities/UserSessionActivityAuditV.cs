using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Keyless]
public partial class UserSessionActivityAuditV
{
    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("session_id")]
    public Guid? SessionId { get; set; }

    [Column("request_id")]
    public Guid? RequestId { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("criticality")]
    public string? Criticality { get; set; }

    [Column("activity", TypeName = "jsonb")]
    public string? Activity { get; set; }


}
