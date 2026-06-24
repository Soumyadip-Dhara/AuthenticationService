using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("consumed_acknowledgement_logs", Schema = "message_queue")]
[Index("CreatedAt", "QueueName", Name = "idx_cons_ack_logs_date_queue")]
[Index("PublishedMessageId", Name = "idx_cons_ack_logs_message_id")]
public partial class ConsumedAcknowledgementLog
{
    [Key]
    [Column("unique_id")]
    public Guid UniqueId { get; set; }

    [Column("message_id")]
    public Guid? MessageId { get; set; }

    [Column("published_message_id")]
    public Guid? PublishedMessageId { get; set; }

    [Column("exchange_name")]
    [StringLength(100)]
    public string? ExchangeName { get; set; }

    [Column("queue_name")]
    [StringLength(100)]
    public string QueueName { get; set; } = null!;

    [Column("raouting_key")]
    [StringLength(255)]
    public string? RaoutingKey { get; set; }

    [Column("message_body", TypeName = "jsonb")]
    public string MessageBody { get; set; } = null!;

    [Column("queue_options", TypeName = "jsonb")]
    public string? QueueOptions { get; set; }

    [Column("consumed_at", TypeName = "timestamp without time zone")]
    public DateTime? ConsumedAt { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [Column("error_messages")]
    public string? ErrorMessages { get; set; }

    [Column("error_type")]
    [StringLength(50)]
    public string? ErrorType { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }
}
