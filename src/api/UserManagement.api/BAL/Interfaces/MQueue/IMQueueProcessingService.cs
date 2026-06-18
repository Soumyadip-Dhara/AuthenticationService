namespace UserMangement.BAL.Interfaces.MQueue
{
    public interface IMQueueProcessingService
    {
        Task ProcessQueueAsync(string queueName, string? correlationId = "");
    }
}
