using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.DAL.Entities
{
    [PrimaryKey("Id", "LevelId")]
    [Table("scopes", Schema = "master")]
    public class Scope
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

        [Column("updated_by")]
        public long? UpdatedBy { get; set; }

        [Column("updated_at", TypeName = "timestamp without time zone")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_active", TypeName = "bool")]
        public bool IsActive { get; set; }

    }
}
