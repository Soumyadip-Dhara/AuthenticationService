using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IRoleService
    {
        Task<PaginatedResult<RoleGetDTO>> GetRoleListAsync(QueryParameters payload);
        Task<List<RoleGetDTO>> GetRolesByApplicationIdForSuperAdminAndUserAdmin(int applicationId);
        Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, short isOwnOffice, int levelId);
    }
}
