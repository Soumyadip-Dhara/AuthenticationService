using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IRoleRelationshipService
    {
        public Task<bool> Insert(RoleRelationshipModel roleRelationshipSetDTO);
        public Task<List<GroupItemDTO>> RolesByAccessRoleIds(List<int> accessRoleIds);
    }
}