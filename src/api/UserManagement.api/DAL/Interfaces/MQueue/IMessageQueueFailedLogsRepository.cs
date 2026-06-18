using UserManagement.DAL.Entities;
using UserManagement.RbbitMQ;

namespace UserManagement.DAL.Interfaces.MQueue
{
    public interface IMessageQueueFailedLogsRepository : IRepository<MessageQueueFailedLog>
    {
        Task InsertLogAsync(AckPayloadModel record);
    }
}
