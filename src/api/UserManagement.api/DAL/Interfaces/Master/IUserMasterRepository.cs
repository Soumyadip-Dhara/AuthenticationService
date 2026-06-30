using System.Threading.Tasks;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface IUserMasterRepository : IRepository<UserMaster>
    {
        Task<string> GetAdminsForScope(string scopeValue, int levelId, int appId);
    }
}
