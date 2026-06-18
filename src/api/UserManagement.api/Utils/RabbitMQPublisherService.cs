using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using UserManagement.Utils.Interfaces;

namespace UserManagement.Utils
{
    public class RabbitMQPublisherService : IRabbitMQPublisherService
        {
            private readonly IConfiguration _configuration;
            private readonly ConnectionFactory _factory;


            public RabbitMQPublisherService(IConfiguration configuration)
            {
                _configuration = configuration;
                _factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQConnection:Host"],
                    Port = int.Parse(_configuration["RabbitMQConnection:Port"]),
                    UserName = _configuration["RabbitMQConnection:UserName"],
                    Password = _configuration["RabbitMQConnection:Password"],
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                    RequestedHeartbeat = TimeSpan.FromSeconds(10)
                };
            }
            public async Task PublishMessage<T>(string queueName, T message, string exchange = "") where T : class
            {
                await Task.Run(async () =>
                {
                    using (var connection = await _factory.CreateConnectionAsync())
                    using (var channel = await connection.CreateChannelAsync())
                    {
                        await channel.QueueDeclareAsync(queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

                        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                        var properties = new BasicProperties
                        {
                            Persistent = true
                        };

                        await channel.BasicPublishAsync(exchange: exchange, routingKey: queueName, mandatory: true, basicProperties: properties, body: messageBody);
                    }
                });
            }
     }
}
    
