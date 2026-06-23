using UserManagement.BAL.Services.MQueue;
using UserManagement.Consumer.ConsumeAck;
using UserManagement.RbbitMQ;
using FluentValidation;
using UserManagement.Models;
using UserManagement.RbbitMQ.Constant;
using UserManagement.RbbitMQ.Validators;
using UserManagement.Models.MQueue;
using UserManagement.BAL.Services.MQueue;
using UserManagement.Consumer;

namespace UserManagement.Extensions
{
    public static class RabbitMqRegiserExtensions
    {
        public static IServiceCollection AddRabbitMQ(
         this IServiceCollection services,
         IConfiguration configuration)
        {

            // Try new multi-host format first: "RabbitMQConnections"
            var hosts = configuration.GetSection("RabbitMQConnections")
                .Get<Dictionary<string, RabbitMQConfigurationModel>>();

            // Fallback to old single-host format: "RabbitMQConnection"
            if (hosts == null || hosts.Count == 0)
            {
                var singleConfig = configuration.GetSection("RabbitMQConnection")
                    .Get<RabbitMQConfigurationModel>();

                if (singleConfig == null)
                {
                    throw new InvalidOperationException(
                        "RabbitMQ configuration is missing. Ensure 'RabbitMQConnections' or 'RabbitMQConnection' section exists in appsettings.");
                }

                hosts = new Dictionary<string, RabbitMQConfigurationModel>
                {
                    { "Default", singleConfig }
                };
            }

            var multiConfig = new RabbitMQMultiHostConfiguration { Hosts = hosts };

            services.AddSingleton(multiConfig);    
            services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();

            return services;

        }

        public static IServiceCollection AddMessageProcessing(
            this IServiceCollection services)
        {

            services.AddScoped<IValidator<UserRegistrationModel>, UserRegistrationValidator>();
            services.AddScoped<IMessageProcessor<UserRegistrationModel>, UserRegistrationProcesser>();
            services.AddHostedService<UserRegistrationConsumer>();

            // ================= ADD ACK CONSUMERS HERE ==================

            // Register ACK Validator
            services.AddSingleton<IValidator<AckPayloadModel>, MQueueAckValidator>();
         
            var ackQueues = new[]
            {
                    MessageQueueConstants.UM_WBJIT_USER_ACK,

            };
            foreach (var queue in ackQueues)
            {
                services.AddSingleton<IHostedService>(sp =>
                    new RabbitMQAckConsumerHostedService(
                        sp.GetRequiredService<ILogger<RabbitMQAckConsumer>>(),
                        sp.GetRequiredService<IRabbitMQConnectionFactory>(),
                        sp.GetRequiredService<IServiceScopeFactory>(),
                        sp.GetRequiredService<IValidator<AckPayloadModel>>(),
                        sp.GetRequiredService<IConfiguration>(),
                        queue
                    ));
        }
             //============================================================


            return services;
        }
    }
}