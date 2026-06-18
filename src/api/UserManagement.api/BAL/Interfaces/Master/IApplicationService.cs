using UserManagement.Models.DTO;
namespace UserManagement.BAL.Interfaces.Master
{
    public interface IApplicationService
    {
        public Task<List<ApplicationGetDTO>> GetAllApplications(); //Modified
        public Task<List<ApplicationGetDTO>> GetApplicationByRoleName(string roleName);
        public Task<List<ApplicationGetDTO>> GetApplicationsByNames(string[] applicationNames);
        public Task<string> GetApplicationNameById(int applictionId);
        public Task<string> GetApplicationKey(int applicationId);
        public Task<string> GetApplicationUrl(int applicationId);

        // new
        public Task<(bool, string)> CreateApplication(ApplicationCreateDTO applicationSetDTO, string photoPath);
        public Task<(bool, string, string)> UpdateApplication(ApplicationUpdateDTO applicationSetDTO, string photoPath);
        public Task<(string, bool)> DeleteApplication(int AppId);
        Task<List<ApplicationFetchDTO>> GetApplicationsByUserId(long userId);
        Task<List<ApplicationGetDTO>> GetApplicationsByUserIdForUM(long userId);
        public Task<List<ApplicationGetDTO>> GetAllApplicationsForSuperAdmin();
        Task<List<ApplicationGetDTO>> GetApplicationByUserIdForMM();
        Task<bool> GetApplicationStatusById(int appId);
        Task<string?> GetApplicationMaintenanceMsgById(int appId);


        //login page application list for user
        Task<List<ApplicationFetchDTO>> GetApplicationsForAppAccessByUserId(long userId);
        Task<List<ApplicationForServiceDTO>> GetAllApplicationsForService();
        Task<ModuleDetails> FetchModuleDetails(long appId);
    }
}