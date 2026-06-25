using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("user_role_scope_app_context", Schema = "user")]
[Index("ApplicationId", "UserLevelHasScopeId", Name = "uq_user_scope_app_context", IsUnique = true)]
public partial class UserRoleScopeAppContext
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("user_level_has_scope_id")]
    public long UserLevelHasScopeId { get; set; }

    [Column("optional_json", TypeName = "jsonb")]
    public string OptionalJson { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserLevelHasScopeId")]
    [InverseProperty("UserRoleScopeAppContexts")]
    public virtual UserLevelHasUserScope UserLevelHasScope { get; set; } = null!;
}
