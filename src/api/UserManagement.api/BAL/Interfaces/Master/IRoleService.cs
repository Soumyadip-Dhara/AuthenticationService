using UserManagement.Models;
using UserManagement.Models.DTO;
namespace UserManagement.BAL.Interfaces.Master
{
    public interface IRoleService
    {
        public Task<List<GroupItemDTO>> GetRolesByApplications(List<int> applicationIds);
        public Task<List<RoleGetDTO>> GetRolesByApplicationId(int applicationId);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdForSuperAdminAndUserAdmin(int applicationId);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleId(int applicationId, int viewRoleId);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, int viewRoleId, string appName);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, short isOwnOffice, int levelId);
        public Task<List<RoleGetDTO>> GetOtherOfficeRolesForUMByOwnRoleId(int applicationId, int umRoleId);
        public Task<int> GetRoleIdByRoleName(string roleName);
        public Task<RoleGetDTO> GetRolesByRoleId(int roleId);
        public Task<RoleGetDTO> InsertRole(RoleModel roleSetDTO);
        public Task<List<RoleFetchDTO>> GetRolesOfAuthenticatedUserByApplicationId(long userId, int applicationId);
        public Task<RoleSelectedDataFetchDTO> GetLevelAndPermissionsOfAuthenticatedUserByRoleId(long roleId);
        public Task<List<RoleGetDTO>> GetRespectiveApplicationRolesByApplicationIdForSuperAdmin(int applicationId);
        public Task<(bool, string)> CreateRole(InsertRoleDTO insertRoleDTO);
        public Task<RoleSelectedDataFetchDTO> GetLevelsAndPermissionsOfAuthenticatedUserByUserIdAndApplicationIdAndRoleId(long userId, int applicationId, long roleId);
        public Task<PermissionAllAndPreselectedDTO> GetAllAndSelectedPermission(RoleSelectionUsingAppIdDTO payload);
        public Task<AllAndSelectedRoles> GetAllRolesAndSelectedRoles(RoleSelectionUsingAppIdDTO payload);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdForApplicationUserAdmin(int applicationId);
        public Task<(string, bool)> UpdateRole(RoleUpdateDTO roleUpdateDTO);
        public Task<(string, bool)> DeleteRole(int Id);
        public Task<List<RoleGetDTO>> GetRolesByApplicationIdForDropdown(int applicationId);
    }
}
