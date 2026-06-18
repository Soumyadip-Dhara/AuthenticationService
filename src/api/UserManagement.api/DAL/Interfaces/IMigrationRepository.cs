using UserManagement.DAL.Entities;
namespace UserManagement.DAL.Interfaces
{
    public interface IMigrationRepository : IRepository<TempHrm>
    {
        Task<Guid> CreateJobAsync(string entityType);
        //Task BulkInsertCsvAsync(Guid jobId, IFormFile file);
        Task ProcessBatchAsync(Guid jobId, int batchSize);
        Task RetryFailedAsync(Guid jobId);
        Task CopyCsvToStaging(IFormFile file, Guid jobId);
        Task CreateJob(Guid jobId, string entityType, int totalRows);
    }
}