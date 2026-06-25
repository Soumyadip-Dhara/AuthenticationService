using UserManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("level_relationships", Schema = "master")]
[Index("AccessLevelId", Name = "fki_level_relationships_access_id_fkey")]
[Index("LevelId", Name = "fki_level_relationships_level_id_fkey")]
public partial class LevelRelationship
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("access_level_id")]
    public int AccessLevelId { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [ForeignKey("AccessLevelId")]
    [InverseProperty("LevelRelationshipAccessLevels")]
    public virtual ApplicationLevel AccessLevel { get; set; } = null!;

    [ForeignKey("LevelId")]
    [InverseProperty("LevelRelationshipLevels")]
    public virtual ApplicationLevel Level { get; set; } = null!;
}
