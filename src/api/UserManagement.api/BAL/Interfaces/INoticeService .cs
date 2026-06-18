using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface INoticeService
    {
        
        Task<List<NoticeDTO>> GetAllNoticesAsync(NoticeFilterDTO filter);
        Task<long> CreateNoticeAsync(CreateNoticeDTO dto, long createdBy);

        Task UpdateNoticeAsync(UpdateNoticeDTO dto, long updatedBy);



    }
}
