using System.Threading.Tasks;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResponse<PaginatedResult<UserDetailsDTO>>> FetchUserList(QueryParameters payload);
    }
}
