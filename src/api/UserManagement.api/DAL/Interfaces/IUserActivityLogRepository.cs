using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces
{
    public interface IUserActivityLogRepository : IRepository<UserActivityLog>
    {
        //Task<List<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter);
        Task<PaginatedResult<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter);
        Task<List<UserLoginCountDTO>> GetCurrentLoggedInUserCount();
        Task<List<DeviceLoginCount>> GetDeviceLoginsAsync();
        Task<List<AgentLoginCount>> GetAgentLoginsAsync();

        Task<ActivityPageResponse> GetPagedActivities(ActivityLogRequestDto request);
    }
}
