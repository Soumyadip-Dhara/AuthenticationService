using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL.Entities;

[Table("app_users")]
[Index("Email", Name = "IX_app_users_Email", IsUnique = true)]
public partial class AppUser
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(256)]
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    [StringLength(256)]
    public string DisplayName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [StringLength(256)]
    public string? Role { get; set; }

    public string? PermissionsJson { get; set; }
}
