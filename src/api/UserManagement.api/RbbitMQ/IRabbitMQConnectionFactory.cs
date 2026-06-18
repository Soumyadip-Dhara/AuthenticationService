using RabbitMQ.Client;

namespace UserManagement.RbbitMQ
{
    public interface IRabbitMQConnectionFactory
    {
        Task<IConnection> CreateConnectionAsync(string hostKey, CancellationToken cancellationToken = default);
        Task<IChannel> CreateChannelAsync(string hostKey, CancellationToken cancellationToken = default);
    }
}
