using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface IDashboardService
    {
        public Task<List<DailyUserLoginDTO>> GetMonthlyLoggedInUser(string? period);
        public Task<DashboardSummaryDTO> GetDashboardSummaryAsync();
        public Task<List<MostLoggedInUsersDTO>> MostLoggedInUsers();
        public Task<List<RecentlyUsersCreatedDTO>> RecentlyUsersCreated();
        public Task<List<MonthlyUserLoginCountDTO>> GetMonthlyLoggedInUserCount();
        //public Task<List<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter);
        public Task<PaginatedResult<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter);
        public Task<List<DeviceLoginCount>> GetDeviceLoginsAsync();
        public Task<List<AgentLoginCount>> GetAgentLoginsAsync();
        public Task<List<UserLoginCountDTO>> GetCurrentLoggedInUserCount();
        public Task<ActivityPageResponse> GetPagedActivities(ActivityLogRequestDto request);
        public Task<bool> VerifyHashChain(string hash, int depth);
       



    }
}
