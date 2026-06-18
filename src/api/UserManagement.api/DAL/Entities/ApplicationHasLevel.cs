using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[Keyless]
[Table("application_has_level", Schema = "master")]
[Index("ApplicationId", Name = "fki_application_has_level_application_id_fkey")]
[Index("LevelId", Name = "fki_application_has_level_level_id_fkey")]
public partial class ApplicationHasLevel
{
    [Column("application_id")]
    public int? ApplicationId { get; set; }

    [Column("level_id")]
    public int? LevelId { get; set; }

    [ForeignKey("ApplicationId")]
    public virtual Application? Application { get; set; }

    [ForeignKey("LevelId")]
    public virtual Level? Level { get; set; }
}
