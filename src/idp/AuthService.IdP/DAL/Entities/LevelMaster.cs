using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("level_master", Schema = "master")]
[Index("LevelName", Name = "level_master_level_name_key", IsUnique = true)]
public partial class LevelMaster
{
    [Key]
    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("level_name")]
    [StringLength(50)]
    public string LevelName { get; set; } = null!;

    [Column("is_global")]
    public bool? IsGlobal { get; set; }

    [Column("parent_level_id")]
    public int? ParentLevelId { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [InverseProperty("Level")]
    public virtual ICollection<ApplicationLevel> ApplicationLevels { get; set; } = new List<ApplicationLevel>();

    [InverseProperty("ParentLevel")]
    public virtual ICollection<LevelMaster> InverseParentLevel { get; set; } = new List<LevelMaster>();

    [ForeignKey("ParentLevelId")]
    [InverseProperty("InverseParentLevel")]
    public virtual LevelMaster? ParentLevel { get; set; }
}
