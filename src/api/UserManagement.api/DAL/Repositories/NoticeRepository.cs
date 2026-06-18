using Microsoft.EntityFrameworkCore;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories
{
    public class NoticeRepository : Repository<Notice, UserManagementDBContext>, INoticeRepository
    {
        private readonly UserManagementDBContext _context;
        public NoticeRepository(UserManagementDBContext userManagementDBContext) : base(userManagementDBContext)
        {
            _context = userManagementDBContext;
        }

        public async Task<List<Notice>> GetNoticesAsync(
        int pageNumber,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isactive)
        {
            var query = _context.Notices
                .AsNoTracking()
                .AsQueryable();
            if (isactive.HasValue)
            {
                query = query.Where(n => n.IsActive == isactive.Value);
            }
            if (fromDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= toDate.Value);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<long> InsertNoticeAsync(Notice notice)
        {
            _context.Notices.Add(notice);
            await _context.SaveChangesAsync();
            return notice.Id;
        }

        public async Task<Notice?> GetNoticeByIdAsync(long id)
        {
            return await _context.Notices.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task UpdateNoticeAsync(Notice notice)
        {
            _context.Notices.Update(notice);
            await _context.SaveChangesAsync();
        }
    }
}
