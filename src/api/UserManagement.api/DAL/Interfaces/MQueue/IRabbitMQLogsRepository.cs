using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Interfaces.MQueue;


public interface IRabbitMQLogsRepository
{
    Task<PagedResponse<PublishedLogDTO>> GetPublishedLogsAsync(BaseLogFilterDTO filter);
    Task<PagedResponse<ConsumedLogDTO>> GetConsumedLogsAsync(BaseLogFilterDTO filter);
    Task<PagedResponse<FailedConsumeLogDTO>> GetFailedConsumeLogsAsync(BaseLogFilterDTO filter);
    Task<PagedResponse<PublishAckLogDTO>> GetPublishAckLogsAsync(BaseLogFilterDTO filter);
    Task<PagedResponse<ConsumeAckLogDTO>> GetConsumeAckLogsAsync(BaseLogFilterDTO filter);
    Task<List<AckSummaryDTO>> GetAckSummaryAsync(DateTime from, DateTime to, string queue, string mode);
}