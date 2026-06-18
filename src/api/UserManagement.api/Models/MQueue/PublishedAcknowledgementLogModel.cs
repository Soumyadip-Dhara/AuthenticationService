namespace UserManagement.Models.MQueue
{
    public class PublishedAcknowledgementLogModel
    {
        public Guid UniqueId { get; set; }
        public Guid MessageId { get; set; }
        public string? ExchangeName { get; set; }
        public string QueueName { get; set; } = null!;
        public string MessageBody { get; set; } = null!;
        public string? QueueOptions { get; set; }
        public DateTime? PublishAt { get; set; }
    }
}

