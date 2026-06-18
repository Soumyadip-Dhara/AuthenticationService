using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[PrimaryKey("Id", "LevelId")]
[Table("tr_28", Schema = "master")]
public partial class Tr28
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name", TypeName = "character varying")]
    public string? Name { get; set; }

    [Column("value", TypeName = "character varying")]
    public string? Value { get; set; }

    [Key]
    [Column("level_id")]
    public int LevelId { get; set; }
}
