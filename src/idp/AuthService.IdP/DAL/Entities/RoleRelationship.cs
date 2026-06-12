using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("role_relationships", Schema = "master")]
[Index("AccessRoleId", Name = "fki_role_relationships_access_id_fkey")]
[Index("RoleId", Name = "fki_role_relationships_role_id_key")]
public partial class RoleRelationship
{
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("access_role_id")]
    public int AccessRoleId { get; set; }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("AccessRoleId")]
    [InverseProperty("RoleRelationshipAccessRoles")]
    public virtual Role AccessRole { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("RoleRelationshipRoles")]
    public virtual Role Role { get; set; } = null!;
}
