using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class PermissionGetDTO
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public List<string> Roles { get; set; } = null!;

        public string? ApplicationName { get; set; }

        public string CreatedBy { get; set; } = null!;
    }
    public class PermissionSetDTO
    {
        [Required]
        [RegularExpression(
          @"[a-zA-Z_-]*",
            ErrorMessage = "{0} must be alphabetical characters only, can include hyphens(-), can include underscores(_)."
        )]
        public string Name { get; set; } = null!;
        public int ApplicationId { get; set; }
    }
    public class PermissionByRoleDTO
    {
        public List<int> roleIds { get; set; } = null!;
    }
    public class PermissionUpdateDTO
    {
        public int Id { get; set; }
        [Required]
        [RegularExpression(
            @"[a-z-_]*",
            ErrorMessage = "{0} must be alphabetical characters only, can include hyphens(-), can include underscores(_)."
        )]
        public string? Name { get; set; }
    }
}
