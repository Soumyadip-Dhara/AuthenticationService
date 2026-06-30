using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface IUserMasterRepository : IRepository<UserMaster>
    {
        Task<string> GetAdminsForScope(string scopeValue, int levelId, int appId);
        Task<List<UserAccessDTO>> GetUserPrivilegesAsync(long userId);
    }
}
