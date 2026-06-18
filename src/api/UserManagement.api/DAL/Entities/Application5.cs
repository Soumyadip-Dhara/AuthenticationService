using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[PrimaryKey("Id", "LevelId")]
[Table("APPLICATION_5", Schema = "master")]
[Index("Value", "LevelId", Name = "APPLICATION_5_value_level_id_key", IsUnique = true)]
public partial class Application5
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name", TypeName = "character varying")]
    public string Name { get; set; } = null!;

    [Column("value", TypeName = "character varying")]
    public string Value { get; set; } = null!;

    [Key]
    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("is_admin_created")]
    public bool IsAdminCreated { get; set; }
}
