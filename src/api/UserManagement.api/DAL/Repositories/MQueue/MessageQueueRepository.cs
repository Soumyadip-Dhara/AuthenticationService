using Microsoft.EntityFrameworkCore;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;
namespace UserManagement.DAL.Repositories.MQueue
{
    public class MessageQueueRepository :Repository<MessageQueue, UserManagementDBContext>,IMessageQueueRepository
    {
        private readonly UserManagementDBContext _dbContext;

        public MessageQueueRepository(UserManagementDBContext context) : base(context) 
        {
            _dbContext = context;
        }
        public async Task<IEnumerable<MessageQueue>> GetRecordsForQueueAsync(string queueName)
        {
            try {
                return await _dbContext.MessageQueues
                .Where(r => r.QueueName == queueName)
                .ToListAsync();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task InsertLogAsync(MessageQueue record, string queueName)
        {
            var log = new MessageQueueLog
            {
                UniqueId = record.UniqueId,
                ExchangeName = record.ExchangeName,
                QueueName = queueName,
                MessageBody = record.MessageBody,
                QueueOptions = record.QueueOptions,
                PublishAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local)
            };

            await _dbContext.MessageQueueLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRecordAsync(Guid uniqueId)
        {
            var record = await _dbContext.MessageQueues.FindAsync(uniqueId);
            if (record != null)
            {
                _dbContext.MessageQueues.Remove(record);
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}
