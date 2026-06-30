using System.Threading.Tasks;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IPermissionService
    {
        Task<FetchPermissionResponse> GetPermissionListAsync(QueryParameters payload);
    }
}
