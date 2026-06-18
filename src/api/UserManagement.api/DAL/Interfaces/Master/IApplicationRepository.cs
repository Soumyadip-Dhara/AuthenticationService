using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface IApplicationRepository : IRepository<Application>
    {
        public Task<(bool, string)> CreateApplication(ApplicationCreateDTO application, string photoPath, long createdBy, string key);
        public Task<(string, bool)> DeleteApplication(int AppId);
    }
}