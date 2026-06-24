using UserManagement.api.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("levels", Schema = "master")]
[Index("ApplicationId", Name = "fki_level_application_fkey")]
public partial class Level
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(50)]
    public string Title { get; set; } = null!;

    [Column("rank")]
    public int Rank { get; set; }

    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("created_by")]
    public long CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_global_level")]
    public bool IsGlobalLevel { get; set; }

    [Column("same_level_other_office_admin_allowed")]
    public bool SameLevelOtherOfficeAdminAllowed { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("Levels")]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual UserMaster CreatedByNavigation { get; set; } = null!;

}

