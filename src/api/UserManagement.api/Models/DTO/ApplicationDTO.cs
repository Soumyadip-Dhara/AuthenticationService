using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class ApplicationGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public bool IsMaintenance { get; set; }
        public bool IsActive { get; set; }

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
