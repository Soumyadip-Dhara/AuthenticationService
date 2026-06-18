using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces
{
    public interface IUserMasterRepository : IRepository<UserMaster>
    {
        (string?, string?) GetJWTFromAccessToken(string accessToken);
        void SetJWTAccessToken(string accessToken, (string, string) data);
        Task<(bool, string, long)> UserRegistration(UserRegistrationNewDTO user, string password, byte[] passwordHash, byte[] passwordSalt);
        Task<(bool, string, long)> NewUserRegistrationBySuperAdmin(UserRegistrationNewDTO user, string password, byte[] passwordHash, byte[] passwordSalt, bool isSuperAdminCreation);
        Task<(bool, string)> ChangePassword(long userId, byte[] passwordHash, byte[] passwordSalt);
        Task<UserDetailsForDisplayWithCountDTO> UserDetailsForDisplay(FilterData payload, string userRoles, long userId);
        Task<List<UserAccessDTO>> GetUserPrivilegesAsync(long userId);
        Task<(bool, string)> ModifyUserPrivileges(
            UserPrivilegeUpdateDTO userPrivilegeUpdate);
        Task<object> GetDataForAdminManagement(string scope, int levelId);
        Task<(bool, string)> ManageAdmin(List<long> userIds, long scopeId, int appId, bool isSingleAdmin);

        Task<List<UserProfileQueryModel>> GetUserProfileData(string userName);

        Task<string> GetAdminsForScope(string scopeValue, int levelId, int appId);
        //-------------------ADMIN DASHBOARD SUMMARY-------------------
        Task<DashboardSummaryDTO> GetDashboardSummaryAsync();

        Task<bool> UpdateSignerIdByUserNameAsync(string userName, string signerId);


    }
}

