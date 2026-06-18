using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IServiceManagementService
    {
        Task<List<MasterServicesDTO>> GetServicesWithPermissions();
        Task<bool> EnableDisableServiceAsync(EnableServiceRequestDTO request);
        //Task<Service> GetServiceByIdAsync(long serviceId);
        Task<ServiceDetails?> GetServiceByIdAsync(long serviceId);
        Task<bool> AddServiceAsync(AddServiceRequestDTO request);
    }
}