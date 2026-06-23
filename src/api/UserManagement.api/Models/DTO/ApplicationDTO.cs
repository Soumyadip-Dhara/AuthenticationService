using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class ApplicationGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? url { get; set; }
        public string? LogoUrl { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMaintenance { get; set; }
        public bool IsActive { get; set; }
        public bool? IsConsumingData { get; set; }
        public string? BaseUrl { get; set; }
        public bool? IsMultiAdminDisallowed { get; set; }
        public bool IsUseUserManagement { get; set; }

    }
    public class ApplicationForServiceDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public bool IsActive { get; set; }
    }
    public class ApplicationCreateDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9- ]*",
            ErrorMessage = "{0} must be alphanumeric characters only, can include hyphens(-), can include space( )."
        )]  
        public string Title { get; set; } = null!;

        [Required]
        [RegularExpression(
            @"[.a-zA-Z0-9-:\/]*",
            ErrorMessage = "{0} must be alphanumeric characters only, can include hyphens(-), slashes(/) and colons(:), can include hyphens(.)."
        )]
        public string Url { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; } = null!;
    }
    public class ApplicationUpdateDTO
    {
        [Required]
        public string Id { get; set; } = null!;

        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9- ]*",
            ErrorMessage = "{0} must be alphanumeric characters only, can include hyphens(-), can include space( )."
        )]        
        public string Title { get; set; } = null!;

        [Required]
        [RegularExpression(
            @"[.a-zA-Z0-9-:\/]*",
            ErrorMessage = "{0} must be alphanumeric characters only, can include hyphens(-), slashes(/) and colons(:), can include hyphens(.)."
        )]     
        public string Url { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; } = null!;
        public bool IsMaintenance { get; set; }
        public bool? IsConsumingData { get; set; }
        public string? BaseUrl { get; set; }
        public bool? IsMultiAdminDisallowed { get; set; }
        public bool IsUseUserManagement { get; set; }

    }

    public class ApplicationFetchDTO
    {
        public int AppId { get; set; }
        public string ApplicationName { get; set; } = null!;
        public string LogoUrl { get; set; } = null!;
        public bool IsMaintenance { get; set; }
        public bool IsActive { get; set; }
        public bool IsUseUserManagement { get; set; }
    }
    public class ModuleDetails
    {
        public string Title { get; set; }
        public string Email { get; set; }
    }
}
