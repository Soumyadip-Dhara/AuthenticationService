using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("message_queue_failed_logs", Schema = "message_queue")]
public partial class MessageQueueFailedLog
{
    [Key]
    [Column("unique_id")]
    public Guid UniqueId { get; set; }

    [Column("action_status")]
    [StringLength(10)]
    public string ActionStatus { get; set; } = null!;

    [Column("failed_type")]
    [StringLength(20)]
    public string? FailedType { get; set; }

    [Column("failed_message")]
    public string? FailedMessage { get; set; }

    [Column("failed_at", TypeName = "timestamp without time zone")]
    public DateTime FailedAt { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("message_id")]
    public Guid? MessageId { get; set; }
}
