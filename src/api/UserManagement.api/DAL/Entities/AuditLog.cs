using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[PrimaryKey("Id", "ChangeTimestamp")]
[Table("audit_log", Schema = "log")]
public partial class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("schema_name")]
    public string SchemaName { get; set; } = null!;

    [Column("table_name")]
    public string TableName { get; set; } = null!;

    [Column("operation_type")]
    public string OperationType { get; set; } = null!;

    [Column("changed_by")]
    public long? ChangedBy { get; set; }

    [Key]
    [Column("change_timestamp", TypeName = "timestamp without time zone")]
    public DateTime ChangeTimestamp { get; set; }

    [Column("old_data", TypeName = "jsonb")]
    public string? OldData { get; set; }

    [Column("new_data", TypeName = "jsonb")]
    public string? NewData { get; set; }
}
