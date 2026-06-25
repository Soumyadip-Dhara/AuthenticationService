using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("message_queue_logs", Schema = "message_queue")]
[Index("CreatedAt", "QueueName", Name = "idx_message_queue_logs_date_queue")]
public partial class MessageQueueLog
{
    [Key]
    [Column("unique_id")]
    public Guid UniqueId { get; set; }

    [Column("exchange_name")]
    [StringLength(100)]
    public string? ExchangeName { get; set; }

    [Column("queue_name")]
    [StringLength(100)]
    public string QueueName { get; set; } = null!;

    [Column("message_body", TypeName = "jsonb")]
    public string MessageBody { get; set; } = null!;

    [Column("queue_options", TypeName = "jsonb")]
    public string? QueueOptions { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("publish_at", TypeName = "timestamp without time zone")]
    public DateTime? PublishAt { get; set; }
}
