using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.RbbitMQ;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.BAL.Services.MQueue
{
    public class MQueueProcessingService : IMQueueProcessingService
    {
        private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly IConfiguration _configuration;
        private readonly IRabbitMqService _rabbitMqService;
        public MQueueProcessingService(IMessageQueueRepository messageQueueRepository, IRabbitMqService rabbitMqService, IConfiguration configuration)
        {
            _messageQueueRepository = messageQueueRepository;
            _configuration = configuration;
            _rabbitMqService = rabbitMqService;
        }
        public async Task ProcessQueueAsync(string queueName, string? correlationId = "")
        {
            var strategy = _messageQueueRepository.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                var transaction = await _messageQueueRepository.BeginTransactionAsync(); // Begin transaction

                try
                {
                    // Fetch records from the database based on queue name
                    var records = await _messageQueueRepository.GetRecordsForQueueAsync(queueName);

                    if (records == null || !records.Any())
                    {
                        await _messageQueueRepository.CommitTransactionAsync(transaction);
                        //_logger.LogInformation($"No records found for queue: {queueName}");
                        return;
                    }

                    foreach (var record in records)
                    {
                        // Insert log into message_queue_logs table with publish time
                        await _messageQueueRepository.InsertLogAsync(record, queueName);

                        // Remove the record from message_queues
                        await _messageQueueRepository.RemoveRecordAsync(record.UniqueId);

                        // Publish each record to RabbitMQ
                        await _rabbitMqService.PushMessageAsync(record.QueueName, record.MessageBody, record.UniqueId.ToString(),"", correlationId);
                        //RabitMQProducer producer = new RabitMQProducer();

                        //await producer.PushMessage(_configuration["RabbitMQConnection:Host"],
                        //     int.Parse(_configuration["RabbitMQConnection:Port"]),
                        //     _configuration["RabbitMQConnection:UserName"],
                        //     _configuration["RabbitMQConnection:Password"], queueName, 5, record.MessageBody);

                    }

                    // Commit the transaction after all records are successfully processed
                    await _messageQueueRepository.CommitTransactionAsync(transaction);

                    //_logger.LogInformation("Successfully processed all records.");
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of any error
                    await _messageQueueRepository.RollbackTransactionAsync(transaction);

                    //_logger.LogError($"An error occurred while processing the queue {queueName}: {ex.Message}", ex);
                    throw; // Re-throw the exception to be handled by the caller
                }
            });
        }
    }
}
