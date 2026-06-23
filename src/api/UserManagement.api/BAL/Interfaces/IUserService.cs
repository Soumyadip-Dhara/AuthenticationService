using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface IUserService
    {
        public Task<bool> CheckExistingUserByLoginId(string userName);
        public Task<bool> DeleteUser(long userId);
        public Task<bool> UpdateUser(UserUpdateDTO user);
        Task<bool> ResetPassword(long user);
        Task<(bool, string)> NewUserRegistration(UserRegistrationNewDTO user);
        Task<(bool, string)> NewUserRegistrationBySuperAdmin(UserRegistrationNewDTO user, bool isSuperAdminCreation);
        Task<UserDetailsForDisplay> LockedUserDetailsForDisplay();
        Task<UserDetailsForDisplayWithCountDTO> UserDetailsForDisplay(FilterData paylod);
        Task<List<UserAccessDTO>> GetUserPrivilegeByUserId(long userId);
        Task<bool> UnlockedUserByUserIds(List<long> userId);
        Task<APIResponseClass<bool>> ChangePassword(ChangePasswordDTO passwordDetails);
        Task<APIResponseClass<bool>> ChangeUserBasicDetails(ChangeUserBasicDetailsDTO userDetails);
        Task<(string, string, bool)> GetUserPhoneEmailDueLogin(long userId);
        Task<(string MobileNumber, string? Email, string UserName)> GetUserDetailForLogin(long userId);
        Task<(bool, string)> UpdateUserPrivilege(UserPrivilegeUpdateDTO userPrivilegeUpdate);
        Task<string> GetKeyofApplicationOfUserByRoleId(int roleId);
        Task<object> GetDataForAdminManagement(SearchDataForAdminManagementDTO searchDataForAdminManagementDTO);
        Task<(bool, string)> ManageAdmin(AdminManagementDTO data);
        Task<bool> AddApplicationToActivityLog(long userId, int applicationId);
        Task<UserProfileDTO> GetUserProfileData(string userName);
        Task<APIResponseClass<OTPResponseDTO>> SendOTPForEmailChange(string username);
        Task<bool> UpadteUserEmail(UpdateEmailDTO updateEmailDTO);
        Task<bool> UpdateSignerIdAsync(string userName, string signerId);
    }
}
