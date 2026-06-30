using System;

namespace UserManagement.Models.DTO
{
    // Matches the exact JSON structure returned by the stored procedure
    public class UserDetailsDTOForDeserialize
    {
        public long id { get; set; }
        public string? userName { get; set; }
        public string? hrmsId { get; set; }
        public string? name { get; set; }
        public string? designation { get; set; }
        public string? mobileNumber { get; set; }
        public string? email { get; set; }
        public bool isActive { get; set; }
        public bool isBlocked { get; set; }
        public DateTime? createdAt { get; set; }
        public string? scope { get; set; }
    }

    // Matches the exact JSON structure expected by the frontend
    public class UserDetailsDTO
    {
        public long userId { get; set; }
        public string? userName { get; set; }
        public string? hrmsId { get; set; }
        public string? name { get; set; }
        public string? designation { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public bool active { get; set; }
        public bool blocked { get; set; }
        public string? createdAt { get; set; }
        public bool hasAnyPrivilege { get; set; }

    }
}
