using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface ILevelRepository : IRepository<ApplicationLevel>
    {
        public Task<(bool, string, string)> CreateLevel(List<LevelPayloadDTO> level);
    }
}