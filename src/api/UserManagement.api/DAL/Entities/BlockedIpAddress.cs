using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("blocked_ip_addresses")]
public partial class BlockedIpAddress
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("ip_address")]
    [StringLength(50)]
    public string IpAddress { get; set; } = null!;

    [Column("blocked_at", TypeName = "timestamp without time zone")]
    public DateTime BlockedAt { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = null!;

    [Column("blocked_until", TypeName = "timestamp without time zone")]
    public DateTime BlockedUntil { get; set; }
}
