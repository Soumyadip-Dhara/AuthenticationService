using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[Keyless]
[Table("application_has_role", Schema = "master")]
[Index("RoleId", Name = "fki_application_has_role_application_fkey")]
[Index("ApplicationId", Name = "fki_application_has_role_role_fkey")]
public partial class ApplicationHasRole
{
    [Column("application_id")]
    public int? ApplicationId { get; set; }

    [Column("role_id")]
    public int? RoleId { get; set; }

    [Column("visible_to")]
    public int? VisibleTo { get; set; }

    [ForeignKey("ApplicationId")]
    public virtual Application? Application { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
}
