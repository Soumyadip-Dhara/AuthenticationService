using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("published_acknowledgement_logs", Schema = "message_queue")]
[Index("CreatedAt", "QueueName", Name = "idx_pub_ack_logs_date_queue")]
[Index("ConsumeMessageId", Name = "idx_pub_ack_logs_message_id")]
public partial class PublishedAcknowledgementLog
{
    [Key]
    [Column("unique_id")]
    public Guid UniqueId { get; set; }

    [Column("consume_message_id")]
    public Guid? ConsumeMessageId { get; set; }

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
