using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IRoleHasPermissionService
    {
        public Task<bool> Insert(RoleHasPermissionModel roleHasPermissionSetDTO);
        public Task<List<PermissionGetDTO>> PermissionByRoleIds(int roleId);
    }
}