using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;
using UserManagement.DAL.Interfaces;


namespace UserManagement.DAL.Interfaces.MQueue
{
    public interface IConsumeFailedLogRepository : IRepository<ConsumeFailedLog>
    {
        Task InsertNewLog(NewConsumeLogModel newConsumeLog);
    }
}
