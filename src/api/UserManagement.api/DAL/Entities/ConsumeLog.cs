using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("consume_logs", Schema = "message_queue")]
[Index("CreatedAt", "QueueName", Name = "idx_consume_logs_date_queue")]
public partial class ConsumeLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("message_id")]
    [StringLength(255)]
    public string? MessageId { get; set; }

    [Column("queue_name")]
    [StringLength(255)]
    public string? QueueName { get; set; }

    [Column("exchange_name")]
    [StringLength(255)]
    public string? ExchangeName { get; set; }

    [Column("raouting_key")]
    [StringLength(255)]
    public string? RaoutingKey { get; set; }

    [Column("message_body")]
    public string? MessageBody { get; set; }

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
