using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Interfaces.MQueue
{
    public interface IConsumeLogRepository: IRepository<ConsumeLog>
    {
        Task InsertNewLog(NewConsumeLogModel newConsumeLog);
    }
}
