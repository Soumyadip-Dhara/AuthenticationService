using System.Threading.Tasks;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IApplicationService
    {
        Task<FetchApplicationResponse> GetApplicationListAsync(QueryParameters payload);
    }
}
