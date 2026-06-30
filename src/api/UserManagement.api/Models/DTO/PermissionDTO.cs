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

    public class PermissionResultDTO
    {
        public int id { get; set; }
        public string name { get; set; } = null!;
    }

    public class PermissionPaginatedResult
    {
        public int? totalCount { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
        public List<PermissionResultDTO>? data { get; set; }
    }

    public class FetchPermissionResponse
    {
        public PermissionPaginatedResult? result { get; set; }
        public int apiResponseStatus { get; set; }
        public string message { get; set; } = string.Empty;
        public string? validationResults { get; set; }
    }
}
