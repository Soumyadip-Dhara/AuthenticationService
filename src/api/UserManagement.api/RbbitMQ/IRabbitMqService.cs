using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserManagement.RbbitMQ
{
    public interface IRabbitMqService
    {
        //void Publish<T>(string routingKey, T message, string exchange = "") where T : class;
        //void Consume<T>(string queueName, Action<T> handleMessage) where T : class;
        Task PublishAsync<T>(string routingKey, T message, string exchange = "") where T : class;
        Task<string> GetPayloadRabbitMQAsync<T>(string queueName);

        Task PublishAsync<T>(string routingKey, T message, string queueId, string exchange = "") where T : class;
        Task<string> PushMessageAsync(string queueName, string message, string queueId, string exchange = "", string? correlationId = "");
    }

}