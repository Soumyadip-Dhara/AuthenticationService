using UserManagement.Helper;
using UserManagement.Models.MQueue;

namespace UserMangement.BAL.Interfaces.MQueue
{
    public interface ILogsService
    {
        Task<PagedResponse<PublishedLogDTO>> GetPublishedLogsAsync(BaseLogFilterDTO filter);
        Task<PagedResponse<ConsumedLogDTO>> GetConsumedLogsAsync(BaseLogFilterDTO filter);
        Task<PagedResponse<FailedConsumeLogDTO>> GetFailedConsumeLogsAsync(BaseLogFilterDTO filter);
        Task<PagedResponse<PublishAckLogDTO>> GetPublishAckLogsAsync(BaseLogFilterDTO filter);
        Task<PagedResponse<ConsumeAckLogDTO>> GetConsumeAckLogsAsync(BaseLogFilterDTO filter);
        Task<List<AckSummaryDTO>> GetAckSummaryAsync(AckSummaryFilterDTO filter);

        //Task<APIResponseClass<string>> RetryFailedConsumeAsync(long id);


    }
}
