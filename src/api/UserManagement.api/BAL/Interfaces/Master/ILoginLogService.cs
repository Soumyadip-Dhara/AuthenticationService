using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface ILoginLogService
    {
        Task<(bool, string)> IsMultipleLoggedIn(long userId, int appId);
    }
}