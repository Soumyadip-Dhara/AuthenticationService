using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IPermissionService
    {
        public Task<List<PermissionGetDTO>> GetPermissionByApplicationIdForViewOnly(int applicationId);
        public Task<List<PermissionGetDTO>> GetPermissionByApplicationId(int applicationId);
        public Task<bool> InsertPermission(List<PermissionSetDTO> permissionSetDTO);
        public Task<List<PermissionGetDTO>> GetPermissionsNotAssignedToARoleByRoleId(int roleId, int applicationId);
        Task<(string, bool)> UpdatePermission(PermissionUpdateDTO permissionUpdateDTO);
        Task<(string, bool)> DeletePermission(int Id);
    }
}