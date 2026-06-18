using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Models.MQueue;
using UserManagement.DAL;
using UserManagement.DAL.Entities;

namespace UserManagement.DAL.Repositories.MQueue;

public class PublishedAcknowledgementLogRepository : Repository<PublishedAcknowledgementLog, UserManagementDBContext>, IPublishedAcknowledgementLogRepository
{
    private readonly UserManagementDBContext _dbContext;
    private readonly ILogger _logger;
    public PublishedAcknowledgementLogRepository(UserManagementDBContext context, ILogger<PublishedAcknowledgementLogRepository> logger) : base(context)
    {
        _dbContext = context;
        _logger = logger;
    }

    public async Task<bool> InsertNewLog(PublishedAcknowledgementLogModel publishedAcknowledgementLog)
    {
        try
        {
            var log = new PublishedAcknowledgementLog()
            {
                UniqueId = publishedAcknowledgementLog.UniqueId,
                ConsumeMessageId = publishedAcknowledgementLog.MessageId,
                QueueName = publishedAcknowledgementLog.QueueName,
                ExchangeName = publishedAcknowledgementLog.ExchangeName,
                MessageBody = publishedAcknowledgementLog.MessageBody,
                QueueOptions = publishedAcknowledgementLog.QueueOptions,
                PublishAt = publishedAcknowledgementLog.PublishAt,
            };
            await _dbContext.AddAsync(log);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting published acknowledgement log");
            throw;
        }
    }
}