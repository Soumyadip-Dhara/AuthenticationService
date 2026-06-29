using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class ApplicationGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public bool IsMaintenance { get; set; }
        public bool IsActive { get; set; }
        public string? url { get; set; }
        public string? LogoUrl { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool? IsConsumingData { get; set; }
        public string? BaseUrl { get; set; }
        public bool? IsMultiAdminDisallowed { get; set; }
        public bool IsUseUserManagement { get; set; }

    }
    public class FetchApplicationResponse
    {
        public ApplicationPaginatedResult? result { get; set; }
        public int apiResponseStatus { get; set; }
        public string message { get; set; } = string.Empty;
        public string? validationResults { get; set; }
    }

    public class ApplicationPaginatedResult
    {
        public int? totalCount { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
        public List<ApplicationGetDTO>? data { get; set; }
    }
}
