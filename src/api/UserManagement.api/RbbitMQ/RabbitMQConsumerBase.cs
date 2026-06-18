    using System.Text;
    using System.Text.Json;
    using UserManagement.DAL.Interfaces.MQueue;
    using UserManagement.Helper;
    using UserManagement.Models.MQueue;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using UserManagement.Enum;
    namespace UserManagement.RbbitMQ
    {
        public abstract class RabbitMQConsumerBase<T> : BackgroundService where T : class
        {
            private IConnection? _connection;
            private IChannel? _channel;
            private readonly ILogger _logger;
            private readonly string _queueName;
            private readonly string _errorQueueName;
            private string _ackQueueName;
            private readonly IRabbitMQConnectionFactory _connectionFactory;
            private readonly IServiceScopeFactory _serviceScopeFactory;
            private readonly JsonSerializerOptions _jsonOptions;
            private readonly string? _exchangeName;
            private NewConsumeLogModel _newConsumeLog;
            private readonly string _routingKey;

            private IMessageProcessor<T> _messageProcessor;
            private IConsumeLogRepository _consumeLogRepo;
            private IConsumeFailedLogRepository _consumeFailedLogRepo;
            private IMessageQueueRepository _messageQueueRepo;
            private IPublishedAcknowledgementLogRepository _publishedAcknowledgementLogRepo;
            private IConsumedAcknowledgementLogRepository _consumedAcknowledgementLogRepo;
            private IReadOnlyBasicProperties _basicProperties;
            //private readonly IReadOnlyBasicProperties _mqBasicProperties;
            private readonly string _virtualHostKey;

            protected RabbitMQConsumerBase(
                ILogger logger,
                IRabbitMQConnectionFactory connectionFactory,
                IServiceScopeFactory serviceScopeFactory,
                //IReadOnlyBasicProperties mqBasicProperties,
                string queueName,
                string? exchangeName = null,
                string? routingKey = null,
                bool isDurable = true,
                string virtualHostKey = "Default")
            {
                _logger = logger;
                _connectionFactory = connectionFactory;
                _serviceScopeFactory = serviceScopeFactory;
                _queueName = queueName;
                _errorQueueName = $"{_queueName}_error";
                _exchangeName = exchangeName;
                _ackQueueName = $"{_queueName}_ack";
                _routingKey = routingKey;
                _virtualHostKey = virtualHostKey;
                //_mqBasicProperties = mqBasicProperties;
                _jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _newConsumeLog = new NewConsumeLogModel
                {
                    QueueName = queueName
                };
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ConnectAndConsume(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in consumer execution. Retrying in 5 seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }

            private async Task ConnectAndConsume(CancellationToken stoppingToken)
            {
                _connection = await _connectionFactory.CreateConnectionAsync(_virtualHostKey, stoppingToken);
                _channel = await _connectionFactory.CreateChannelAsync(_virtualHostKey, stoppingToken);


                // Configure channel
                await _channel.QueueDeclareAsync(_queueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false);
                if (!string.IsNullOrWhiteSpace(_exchangeName))
                {
                    await _channel.ExchangeDeclareAsync(
                        exchange: _exchangeName,
                        type: ExchangeType.Fanout, // You can change to Topic/Fanout if required
                        durable: true,
                        autoDelete: false);
                    await _channel.QueueBindAsync(
                       queue: _queueName,
                       exchange: _exchangeName,
                         routingKey: "");
                }

                // if (!_queueName.Contains("_ack", StringComparison.OrdinalIgnoreCase))
                // {
                //     await _channel.QueueDeclareAsync(_ackQueueName,
                //         durable: true,
                //         exclusive: false,
                //         autoDelete: false);
                // }

                await _channel.BasicQosAsync(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                // Keep the connection alive until cancellation is requested
                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    _logger.LogInformation("Consumer shutdown initiated");
                }
            }

            private async Task HandleMessageAsync(object? sender, BasicDeliverEventArgs ea)
            {
                _newConsumeLog.Reset();
                _newConsumeLog.ConsumedAt = DateTime.Now;
                using var scope = _serviceScopeFactory.CreateScope();
                _messageProcessor = scope.ServiceProvider.GetRequiredService<IMessageProcessor<T>>();
                _consumeLogRepo = scope.ServiceProvider.GetRequiredService<IConsumeLogRepository>();
                _consumeFailedLogRepo = scope.ServiceProvider.GetRequiredService<IConsumeFailedLogRepository>();
                _messageQueueRepo = scope.ServiceProvider.GetRequiredService<IMessageQueueRepository>();
                _publishedAcknowledgementLogRepo = scope.ServiceProvider.GetRequiredService<IPublishedAcknowledgementLogRepository>();
                _consumedAcknowledgementLogRepo = scope.ServiceProvider.GetRequiredService<IConsumedAcknowledgementLogRepository>();
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.Span);
                    var isRedelivered = ea.Redelivered;
                    _newConsumeLog.IsRedelivered = isRedelivered;
                    _newConsumeLog.MessageBody = message;
                    _basicProperties = ea.BasicProperties;
                    if (ea.BasicProperties.ReplyTo != null)
                    {
                        _ackQueueName = ea.BasicProperties.ReplyTo;
                    }
                    _logger.LogInformation("Processing message. Redelivered: {IsRedelivered}", isRedelivered);
                    if (await ProcessMessageAsync())
                    {
                        _channel?.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        _channel?.BasicNackAsync(ea.DeliveryTag, false, !isRedelivered);
                    }
                }
                catch (Exception ex)
                {
                    await FailedProcess("ProcessingError", ex.ToString(), false, ConsumeStatusEnums.PENDING);
                }
            }

            private async Task<bool> ProcessMessageAsync()
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_basicProperties.MessageId))
                    {
                        await FailedProcess("MessageIdMissing", "MessageId is missing in the message properties");
                        return true;
                    }

                    if (Guid.TryParse(_basicProperties.MessageId, out var msgId))
                    {
                        _newConsumeLog.MessageId = msgId;
                    }
                    else
                    {
                        await FailedProcess("InvalidMessageId", "MessageId is not a valid GUID");
                        return true;
                    }
                    var payload = JsonSerializer.Deserialize<T>(_newConsumeLog.MessageBody, _jsonOptions);

                    if (payload == null)
                    {
                        await FailedProcess("Deserialization", "Message deserialization resulted in null");
                        return true;
                    }

                    var validationResult = await _messageProcessor.ValidateMessage(payload);
                    if (!validationResult.IsValid)
                    {
                        var validationErrors = validationResult.Errors
                            .Select(error => new ValidationError
                            {
                                PropertyName = error.PropertyName,
                                ErrorMessage = error.ErrorMessage
                            });
                        await FailedProcess("ValidationFailure", JsonSerializer.Serialize(validationErrors));
                        return true;
                    }
                    await _messageProcessor.ProcessMessage(payload, _basicProperties);
                    await SuccessProcess();
                    return true;
                }
                catch (JsonException ex)
                {
                    await FailedProcess("Deserialization", ex.Message);
                    return true;
                }
                catch (Exception ex)
                {
                    await FailedProcess("ProcessingError", ex.Message, true, ConsumeStatusEnums.NO_ACTION);
                    return true;
                }
            }

            private async Task SuccessProcess(bool isPublish = true)
            {
                try
                {
                    _newConsumeLog.Status = ConsumeStatusEnums.SUCCESS;
                    _newConsumeLog.ExchangeName = _exchangeName;
                    await _consumeLogRepo.InsertNewLog(_newConsumeLog);

                    if (isPublish)
                    {
                        await PublishAckAsync();
                    }
                }
                catch (Exception ex)
                {
                    SaveLocally("ConsumeLog", _newConsumeLog.MessageId, JsonSerializer.Serialize(_newConsumeLog), ex.ToString());
                }
            }

            private async Task FailedProcess
            (
                string failedType,
                string failedMessage,
                bool isPublish = true,
                string actionStatus = ConsumeStatusEnums.NO_ACTION
            )
            {
                try
                {
                    DateTime failedAt = DateTime.Now;
                    _newConsumeLog.FailedType = failedType;
                    _newConsumeLog.FailedMessage = failedMessage;
                    _newConsumeLog.FailedAt = failedAt;
                    _newConsumeLog.ActionStatus = actionStatus;
                    _newConsumeLog.ExchangeName = _exchangeName;
                    await _consumeFailedLogRepo.InsertNewLog(_newConsumeLog);
                    if (isPublish)
                    {
                        await PublishNackAsync(failedType, failedMessage);
                    }
                }
                catch (Exception ex)
                {
                    SaveLocally("consumeFailedLog", _newConsumeLog.MessageId, JsonSerializer.Serialize(_newConsumeLog), ex.ToString());
                }
            }
            private async Task PublishAcknowledgementAsync(
                Guid? consumeMessageQueueId,
                string ackMessageId,
                string routingKey,
                bool isSuccess,
                string statusMessage = "",
                string failedType = ""
            )
            {
                AckPayloadNullGuidModel ackPayload = new AckPayloadNullGuidModel();
                BasicProperties basicProperties = new BasicProperties();
                try
                {
                    DateTime timestamp = DateTime.Now;
                    ackPayload = new AckPayloadNullGuidModel
                    {
                        MessageId = consumeMessageQueueId,
                        Status = isSuccess ? ConsumeStatusEnums.SUCCESS : ConsumeStatusEnums.FAILED,
                        StatusMsg = isSuccess ? "Message Consumed Successfully." : statusMessage,
                        FailedType = isSuccess ? null : failedType,
                        Timestamp = timestamp,
                        ModuleId = "1"  // HardCoded for UserManagement
                    };

                    var ackPayloadString = JsonSerializer.Serialize(ackPayload);
                    var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(ackPayloadString));

                    basicProperties = new BasicProperties
                    {
                        MessageId = ackMessageId,
                        AppId = "1", // HardCoded for UserManagement
                        CorrelationId = _basicProperties.CorrelationId,
                    };

                    if (_channel == null)
                    {
                        throw new Exception("Channel is null");
                    }

                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: routingKey,
                        mandatory: true,  // true for NACK, false for ACK
                        basicProperties: basicProperties,
                        body: body
                    );

                    await InsertAckLogAsync(ackPayload, basicProperties);
                }
                catch (Exception ex)
                {
                    var data = new
                    {
                        AckPayload = ackPayload,
                        BasicProperties = basicProperties
                    };
                    SaveLocally("PublishAcknowledgementAsync", consumeMessageQueueId, JsonSerializer.Serialize(data), ex.ToString());
                }
            }

            // Wrapper method for publishing NACK (for backward compatibility)
            private async Task PublishNackAsync(string failedType, string failedMessage)
            {
                await PublishAcknowledgementAsync(
                    consumeMessageQueueId: _newConsumeLog.MessageId,
                    ackMessageId: Guid.NewGuid().ToString(),
                    routingKey: _ackQueueName,
                    isSuccess: false,
                    statusMessage: failedMessage,
                    failedType: failedType
                );
            }

            // Wrapper method for publishing ACK (for backward compatibility)
            private async Task PublishAckAsync()
            {
                await PublishAcknowledgementAsync(
                    consumeMessageQueueId: _newConsumeLog.MessageId,
                    ackMessageId: Guid.NewGuid().ToString(),
                    routingKey: _ackQueueName,
                    isSuccess: true
                );
            }

            private void SaveLocally(string folderName, Guid? queueId, string data, string error)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                string basePath = configuration["Logging:MessageQueueLog:ErrorLogPath"];

                // Ensure basePath ends with slash if not empty
                if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/") && !basePath.EndsWith("\\"))
                {
                    basePath += "/";
                }

                string path = $"{basePath}{folderName}/{_queueName}_{queueId}";
                CommonHelper.SaveErrorLocally(path, data, error);
            }
            private async Task InsertAckLogAsync(AckPayloadNullGuidModel ackPayload, BasicProperties basicProperties)
            {
                try
                {
                    PublishedAcknowledgementLogModel publishedAcknowledgementLogModel = new PublishedAcknowledgementLogModel()
                    {
                        UniqueId = Guid.Parse(basicProperties.MessageId),
                        MessageId = (Guid)ackPayload.MessageId,
                        QueueName = _ackQueueName,
                        MessageBody = JsonSerializer.Serialize(ackPayload),
                        QueueOptions = JsonSerializer.Serialize(basicProperties),
                        PublishAt = ackPayload.Timestamp
                    };
                    var result = await _publishedAcknowledgementLogRepo.InsertNewLog(publishedAcknowledgementLogModel);
                    if (!result)
                    {
                        throw new Exception("Failed to insert published acknowledgement log table");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to insert published acknowledgement log table", ex);
                }
            }

            public override async Task StopAsync(CancellationToken cancellationToken)
            {
                _logger.LogInformation("Stopping consumer");
                await base.StopAsync(cancellationToken);
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
