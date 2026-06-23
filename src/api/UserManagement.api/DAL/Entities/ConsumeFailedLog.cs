using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("consume_failed_logs", Schema = "message_queue")]
public partial class ConsumeFailedLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("message_id")]
    public Guid? MessageId { get; set; }

    [Column("queue_name")]
    [StringLength(255)]
    public string? QueueName { get; set; }

    [Column("exchange_name")]
    [StringLength(255)]
    public string? ExchangeName { get; set; }

    [Column("message_body")]
    public string? MessageBody { get; set; }

    [Column("consumed_at", TypeName = "timestamp without time zone")]
    public DateTime? ConsumedAt { get; set; }

    [Column("failed_type")]
    [StringLength(20)]
    public string? FailedType { get; set; }

    [Column("failed_message")]
    public string? FailedMessage { get; set; }

    [Column("failed_at", TypeName = "timestamp without time zone")]
    public DateTime FailedAt { get; set; }

    [Column("action_status")]
    [StringLength(10)]
    public string ActionStatus { get; set; } = null!;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_redelivered")]
    public bool IsRedelivered { get; set; }

    [Column("routing_key")]
    [StringLength(255)]
    public string? RoutingKey { get; set; }
}
