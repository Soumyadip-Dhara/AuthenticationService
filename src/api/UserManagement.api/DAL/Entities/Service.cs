using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("services", Schema = "master")]
public partial class Service
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("service_name", TypeName = "character varying")]
    public string ServiceName { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }
}
