using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Models.MQueue
{
    public class ConsumedAcknowledgementLogModel
    {
        public Guid? MessageId { get; set; }
        public Guid? PublishedMessageId { get; set; }
        public string? ExchangeName { get; set; }
        public string QueueName { get; set; } = null!;
        public string? RaoutingKey { get; set; }
        public string MessageBody { get; set; } = null!;
        public string? QueueOptions { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessages { get; set; }
        public string? ErrorType { get; set; }
    }
}

