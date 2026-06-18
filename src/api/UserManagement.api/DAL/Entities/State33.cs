using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities;

[PrimaryKey("Id", "LevelId")]
[Table("STATE_33", Schema = "master")]
public partial class State33
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

    [Column("parent_scope_id")]
    public long? ParentScopeId { get; set; }

    [Column("parent_scope_value", TypeName = "character varying")]
    public string? ParentScopeValue { get; set; }

    [Column("parent_scope_level_id")]
    public int? ParentScopeLevelId { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("is_admin_created")]
    public bool IsAdminCreated { get; set; }
}
