using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using FluentValidation;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using UserManagement.DAL;
using Microsoft.EntityFrameworkCore;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Helper;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;

namespace UserManagement.Consumer.ConsumeAck
{
    public class RabbitMQAckConsumer : BackgroundService, IDisposable
    {
        private readonly ILogger<RabbitMQAckConsumer> _logger;
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IValidator<AckPayloadModel> _validator;
        private readonly IConfiguration _configuration;
        private readonly string _queueName;
        private readonly string _virtualHostKey;

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQAckConsumer(
            ILogger<RabbitMQAckConsumer> logger,
            IRabbitMQConnectionFactory connectionFactory,
            IServiceScopeFactory scopeFactory,
            IValidator<AckPayloadModel> validator,
            IConfiguration configuration,
            string queueName,
            string virtualHostKey = "Default")
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _scopeFactory = scopeFactory;
            _validator = validator;
            _configuration = configuration;
            _queueName = queueName;
            _virtualHostKey = virtualHostKey;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // PROTECT AGAINST WRONG QUEUE NAME
            if (!_queueName.EndsWith("_ack", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("RabbitMQAckConsumer started for NON-ACK queue: {QueueName}. Skipping.", _queueName);
                return;
            }

            try
            {
                _connection = await _connectionFactory.CreateConnectionAsync(_virtualHostKey, stoppingToken);
                _channel = await _connectionFactory.CreateChannelAsync(_virtualHostKey, stoppingToken);

                await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += ProcessAckMessage;

                await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "CRITICAL ERROR: RabbitMQAckConsumer failed to start for queue: {QueueName}. Service is stopping.", _queueName);
                throw; // Re-throw to ensure the host knows it failed
            }
        }

        private async Task ProcessAckMessage(object sender, BasicDeliverEventArgs ea)
        {
            if (_channel == null)
            {
                _logger.LogError("Channel is not initialized");
                return;
            }

            string raw = Encoding.UTF8.GetString(ea.Body.Span);
            var basicProperties = ea.BasicProperties;
            Guid? ackUniqueId = null;
            string ackProcessStatus = "SUCCESS";
            string? ackProcessError = null;
            string? ackProcessErrorType = null;
            AckPayloadModel? ackPayload = null;

            if (!string.IsNullOrWhiteSpace(basicProperties.MessageId) && Guid.TryParse(basicProperties.MessageId, out var parsedId))
            {
                ackUniqueId = parsedId;
            }
            else
            {
                ackUniqueId = Guid.NewGuid();
                ackProcessErrorType = "MESSAGE_ID_NOT_FOUND";
                ackProcessError = "Message ID not found or invalid";
                ackProcessStatus = "ERROR";
                await SaveAckMessage(ackPayload, ackUniqueId, raw, ackProcessStatus, ackProcessError, ackProcessErrorType, ea);
                return;
            }

            try
            {
                ackPayload = JsonSerializer.Deserialize<AckPayloadModel>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize acknowledgment message from queue: {QueueName}", _queueName);
                ackProcessStatus = "ERROR";
                ackProcessError = ex.Message;
                ackProcessErrorType = "JSON_DESERIALIZATION_ERROR";
                await SaveAckMessage(ackPayload, ackUniqueId, raw, ackProcessStatus, ackProcessError, ackProcessErrorType, ea);
                return;
            }

            if (ackPayload == null)
            {
                _logger.LogError("Deserialized acknowledgment message is null from queue: {QueueName}", _queueName);
                ackProcessStatus = "ERROR";
                ackProcessError = "Deserialized acknowledgment message is null";
                ackProcessErrorType = "DESERIALIZATION_ERROR";
                await SaveAckMessage(ackPayload, ackUniqueId, raw, ackProcessStatus, ackProcessError, ackProcessErrorType, ea);
                return;
            }

            // Validate the deserialized message
            var validationResult = await _validator.ValidateAsync(ackPayload);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogError("Validation failed for acknowledgment message from queue: {QueueName}. Errors: {Errors}", _queueName, errors);
                ackProcessStatus = "ERROR";
                ackProcessError = errors;
                ackProcessErrorType = "VALIDATION_ERROR";
                await SaveAckMessage(ackPayload, ackUniqueId, raw, ackProcessStatus, ackProcessError, ackProcessErrorType, ea);
                return;
            }

            await SaveAckMessage(ackPayload, ackUniqueId, raw, ackProcessStatus, ackProcessError, ackProcessErrorType, ea);
        }

        private async Task SaveAckMessage(AckPayloadModel? ackPayload, Guid? messageId, string raw, string ackProcessStatus, string? ackProcessError, string? ackProcessErrorType, BasicDeliverEventArgs ea)
        {
            // Use ackPayload.MessageId if available, otherwise fallback to messageId passed in
            Guid? publishedMessageId = ackPayload?.MessageId;

            using var scope = _scopeFactory.CreateScope();
            var consumedAcknowledgementLogRepository = scope.ServiceProvider.GetRequiredService<IConsumedAcknowledgementLogRepository>();
            var messageQueueFailedLogsRepository = scope.ServiceProvider.GetRequiredService<IMessageQueueFailedLogsRepository>();

            var strategy = consumedAcknowledgementLogRepository.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Start a single transaction for all operations
                using var consumedAcknowledgementLogTransaction = await consumedAcknowledgementLogRepository.BeginTransactionAsync();
                using var messageQueueFailedLogTransaction = await messageQueueFailedLogsRepository.BeginTransactionAsync();
                try
                {
                    ConsumedAcknowledgementLogModel consumeLog = new ConsumedAcknowledgementLogModel
                    {
                        MessageId = messageId,
                        PublishedMessageId = publishedMessageId,
                        QueueName = _queueName,
                        MessageBody = raw,
                        ConsumedAt = DateTime.Now,
                        Status = ackProcessStatus,
                        ErrorMessages = ackProcessError,
                        ErrorType = ackProcessErrorType
                    };

                    if (ackPayload != null && !string.Equals(ackPayload.Status, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        await messageQueueFailedLogsRepository.InsertLogAsync(ackPayload);
                    }

                    await consumedAcknowledgementLogRepository.InsertNewLog(consumeLog);

                    await consumedAcknowledgementLogRepository.CommitTransactionAsync(consumedAcknowledgementLogTransaction);
                    await messageQueueFailedLogsRepository.CommitTransactionAsync(messageQueueFailedLogTransaction);

                    if (_channel != null)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    await consumedAcknowledgementLogRepository.RollbackTransactionAsync(consumedAcknowledgementLogTransaction);
                    await messageQueueFailedLogsRepository.RollbackTransactionAsync(messageQueueFailedLogTransaction);
                    _logger.LogError(ex, "Transaction failed for acknowledgment message from queue: {QueueName}", _queueName);

                    string basePath = $"{_configuration["Logging:MessageQueueLog:ErrorLogPath"]}/ConsumeAck/";
                    string errorLogPath = $"{basePath}{_queueName}_{messageId}";
                    CommonHelper.SaveErrorLocally(errorLogPath, raw, ex.ToString());

                    if (_channel != null)
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
            });
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
