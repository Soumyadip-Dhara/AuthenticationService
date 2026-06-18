using UserManagement.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace UserManagement.RbbitMQ
{
    public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory,IDisposable
    {
        private readonly RabbitMQMultiHostConfiguration _multiConfig;
        private readonly ILogger<RabbitMQConnectionFactory> _logger;
        private readonly ConcurrentDictionary<string, IConnection> _connections = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();


        public RabbitMQConnectionFactory(
            RabbitMQMultiHostConfiguration multiConfig,
            ILogger<RabbitMQConnectionFactory> logger)
        {
            _multiConfig = multiConfig;
            _logger = logger;
        }

        // Default overloads — delegate to "Default" key
        public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            => CreateConnectionAsync("Default", cancellationToken);

        public Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
            => CreateChannelAsync("Default", cancellationToken);

        public async Task<IConnection> CreateConnectionAsync(string hostKey, CancellationToken cancellationToken = default)

        {
            if (_connections.TryGetValue(hostKey, out var existing) && existing.IsOpen)
                return existing;


            var lockObj = _locks.GetOrAdd(hostKey, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(cancellationToken);

            try
            {
                if (_connections.TryGetValue(hostKey, out existing) && existing.IsOpen)
                    return existing;

                if (!_multiConfig.Hosts.TryGetValue(hostKey, out var config))
                    throw new InvalidOperationException($"RabbitMQ host key '{hostKey}' not found in configuration. Available keys: {string.Join(", ", _multiConfig.Hosts.Keys)}");


                var factory = new ConnectionFactory
                {
                    HostName = config.Host,
                    UserName = config.UserName,
                    Password = config.Password,
                    VirtualHost = config.VirtualHost,
                    Port = config.Port,
                    ConsumerDispatchConcurrency = 1
                };

                var connection = await factory.CreateConnectionAsync(cancellationToken);
                _logger.LogInformation("Connected to RabbitMQ — Host: {Host}, VirtualHost: {VHost}, Key: {Key}",
                    config.Host, config.VirtualHost, hostKey);


                connection.ConnectionShutdownAsync += (sender, args) =>
                {
                    _logger.LogWarning("RabbitMQ connection shutdown for key '{Key}'. Reason: {Reason}", hostKey, args.ReplyText);
                    _connections.TryRemove(hostKey, out _);
                    return Task.CompletedTask;
                };


                _connections[hostKey] = connection;
                return connection;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ for key '{Key}'", hostKey);
                throw;
            }
            finally
            {
                lockObj.Release();
            }
        }

        public async Task<IChannel> CreateChannelAsync(string hostKey, CancellationToken cancellationToken = default)
        {
            var connection = await CreateConnectionAsync(hostKey, cancellationToken);
            return await connection.CreateChannelAsync();
        }

        //private Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        //{
        //    _logger.LogWarning("RabbitMQ connection shutdown. Reason: {0}", e.ReplyText);
        //    _connection = null;
        //    return Task.CompletedTask;
        //}

        public void Dispose()
        {
            foreach (var conn in _connections.Values)
                conn?.Dispose();
            foreach (var lck in _locks.Values)
                lck?.Dispose();
            _connections.Clear();
            _locks.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
