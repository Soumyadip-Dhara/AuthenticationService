using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using UserManagement.DAL.Entities;

namespace UserManagement.Models.DTO
{
    public class DailyUserLoginDTO
    {
        public DateTime LoginDay { get; set; }
        public int UserLoginCount { get; set; }
    }
    public class DashboardSummaryDTO
    {
        [JsonPropertyName("total_user")]
        public int TotalUser { get; set; }

        [JsonPropertyName("active_total_user")]
        public int ActiveTotalUser { get; set; }

        [JsonPropertyName("new_users_added_in_month")]
        public int NewUsersAddedInMonth { get; set; }

        [JsonPropertyName("average_engagement_time_in_minute")]
        public double? AverageEngagementTimeInMinute { get; set; }

        [JsonPropertyName("total_modules")]
        public int TotalModules { get; set; }

        [JsonPropertyName("active_modules")]
        public int ActiveModules { get; set; }

        [JsonPropertyName("umder maintainance_modules")]
        public int UnderMaintenanceModules { get; set; }
        [JsonPropertyName("total_admins_in_the_system")]
        public int TotalAdmins { get; set; }

    }
    public class MostLoggedInUsersDTO : RecentlyUsersCreatedDTO
    {
        public int login_count { get; set; }

    }
    public class RecentlyUsersCreatedDTO
    {
        public long id { get; set; }
        public string? user_name { get; set; }
        public string? name { get; set; }
        public string? designation { get; set; }
        public List<string?> application { get; set; }
    }
    public class MonthlyUserLoginCountDTO
    {
        public int month { get; set; }
        public int UserLoginCount { get; set; }
    }
    public class ActivityLogFilterDTO
    {
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Username { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

    }

    public class UserActivityLogDTO
    {
        public DateTime LoginTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string[] Application { get; set; }
        public bool? SystemLogout { get; set; }

        public Guid? SessionId { get; set; }
    }
    public class UserLoginCountDTO
    {
        public string LoginTime { get; set; }
        public int UserLoginCount { get; set; }
    }

    public class ActivityPageResponse
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<string> Activities { get; set; }
    }


    public class ActivityLogRequestDto
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public long? UserId { get; set; }
        public Guid? SessionId { get; set; }
    }

    //public class UserSessionActivityAuditView
    //{

    //    public string? UserId { get; set; }
    //    public Guid? SessionId { get; set; } 
    //    public Guid? RequestId { get; set; }    
    //    public DateTime? Timestamp { get; set; }
    //    public string? Activity { get; set; }
    //}

    public class HashCheckRequest
    {
        public string Hash { get; set; }
        public int Depth { get; set; }
    }
    //public class HashChainValidationResultDTO
    //{
    //    public bool IsTampered { get; set; }
    //    public string Message { get; set; }
    //    public string FirstTamperedHash { get; set; }
    //    public int TotalNodesChecked { get; set; }
    //}

    public class PaginatedResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalRecords { get; set; }
    }








}
