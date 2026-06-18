using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Models.MQueue
{
    public class BaseLogFilterDTO
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
        public string? Search { get; set; }
        public string? QueueName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int Size { get; set; }
        public int Total { get; set; }
    }
    public class PublishedLogDTO
    {
        public Guid UniqueId { get; set; }
        public string? QueueName { get; set; }
        public string? ExchangeName { get; set; }
        public string? MessageBody { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishAt { get; set; }
    }
    public  class PublishedLogWithCount : PublishedLogDTO
    {
        public int TotalCount { get; set; }   // 👈 IMPORTANT
    }
    public class ConsumedLogDTO
    {
        public string MessageId { get; set; }
        public string? QueueName { get; set; }
        public string? ExchangeName { get; set; }
        public string? MessageBody { get; set; }
        public string RoutingKey { get; set; }
        public string Status { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public string? ErrorMessages { get; set; }
    }
    public class FailedConsumeLogDTO
    {
        public long Id { get; set; }
        public Guid MessageId { get; set; }
        public string? QueueName { get; set; }
        public string? ExchangeName { get; set; }
        public string? RoutingKey { get; set; }
        public string MessageBody { get; set; }
        public string FailedType { get; set; }
        public string FailedMessage { get; set; }
        public DateTime FailedAt { get; set; }
        public string ActionStatus { get; set; }
    }
    public class PublishAckLogDTO
    {
        public Guid UniqueId { get; set; }
        public Guid? ConsumeMessageId { get; set; }   
        public string? QueueName { get; set; }
        public string MessageBody {get; set;}
        public string? ExchangeName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishAt { get; set; }
    }
    public class ConsumeAckLogDTO
    {
        public Guid UniqueId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? PublishedMessageId { get; set; }
        public string? QueueName { get; set; }
        public string Status { get; set; } = "";   
        public DateTime? ConsumedAt { get; set; }
        public string? ErrorMessage { get; set; }    
        public string? FailedType { get; set; }     
        public string? MessageBody { get; set; } 
    }

    public class AckSummaryDTO
    {
        public string? Producer { get; set; }
        public string? Consumer { get; set; }
        public string? QueueName { get; set; }
        public long TotalMessages { get; set; }
        public long AckReceived { get; set; }
        public long AckPending { get; set; }
        public decimal AckPercentage { get; set; }
        public string? AckStatus { get; set; }
    }
    public class AckSummaryFilterDTO
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? QueueName { get; set; } = "ALL";
        public string? Mode { get; set; } = "CONSUMER"; // or PRODUCER
    }
}

