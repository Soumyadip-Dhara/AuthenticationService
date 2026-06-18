using UserManagement.DAL.Entities;

namespace UserManagement.DAL.Interfaces.MQueue
{
    public interface IMessageQueueRepository:IRepository<MessageQueue>
    {
        Task<IEnumerable<MessageQueue>> GetRecordsForQueueAsync(string queueName);
        Task InsertLogAsync(MessageQueue record, string queueName);
        Task RemoveRecordAsync(Guid uniqueId);
    }
}
