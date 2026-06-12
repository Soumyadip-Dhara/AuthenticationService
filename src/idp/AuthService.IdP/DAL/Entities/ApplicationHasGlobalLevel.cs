using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("application_has_global_levels", Schema = "master")]
public partial class ApplicationHasGlobalLevel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("level_id")]
    public int LevelId { get; set; }

    [Column("app_id")]
    public int AppId { get; set; }
}
