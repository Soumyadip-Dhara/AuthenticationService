using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<List<RoleFetchDTO>> GetRolesOfAuthenticatedUserByApplicationId(long userId, int applicationId);
        Task<(bool, string, int)> CreateRole(InsertRoleDTO insertRoleDTO);
        Task<(string, bool)> UpdateRole(RoleUpdateDTO roleUpdateDTO);
    }
}