using UserManagement.DAL.Entities;

namespace UserManagement.DAL.Interfaces
{
    public interface INoticeRepository : IRepository<Notice>
    {
        
        Task<List<Notice>> GetNoticesAsync(
                int pageNumber,
                int pageSize,
                DateTime? fromDate,
                DateTime? toDate,
                bool? isactive);
        Task<long> InsertNoticeAsync(Notice notice);
        Task<Notice?> GetNoticeByIdAsync(long id);
        Task UpdateNoticeAsync(Notice notice);


    }
}
