using System.Threading.Tasks;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.DAL.Interfaces
{
    public interface IUserRepository
    {
        Task<PaginatedResult<UserDetailsDTO>> FetchUserList(QueryParameters payload, string userRoles, long userId);
    }
}
