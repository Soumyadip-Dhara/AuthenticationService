using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface IMigrationService
    {
        Task UploadCsvAsync(IFormFile file, string entityType, Guid jobId);
    }
}