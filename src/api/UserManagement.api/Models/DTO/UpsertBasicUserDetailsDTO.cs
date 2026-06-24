using System.Text.Json.Serialization;

namespace UserManagement.Models.DTO
{
    public class UpsertBasicUserDetailsRequest
    {
        public long? userId { get; set; }
        public string? userName { get; set; }
        public string? hrmsId { get; set; }
        public string? name { get; set; }
        public string? designation { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public bool? active { get; set; }
        public bool? blocked { get; set; }
    }

    public class UpsertBasicUserDetailsResponse
    {
        public int apiResponseStatus { get; set; }
        public string message { get; set; } = string.Empty;
        public string? validationResults { get; set; }
    }

    public class FetchBasicUserDetailsResponse
    {
        public UserDetailsDTO? result { get; set; }
        public int apiResponseStatus { get; set; }
        public string message { get; set; } = string.Empty;
        public string? validationResults { get; set; }
    }
}
