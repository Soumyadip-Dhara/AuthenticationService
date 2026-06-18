using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
namespace UserManagement.DAL.Interfaces
{
    public interface IMasterServiceRepository : IRepository<Service>
    {
        Task<List<MasterServicesDTO>> GetServicesWithPermissionsAsync();
        Task<(bool, string)> EnableDisableServiceAsync(long serviceId, long appId, bool enabled, long userId);
        Task<Guid> GetClientSecretAsync(long serviceId, long appId);
    }
}