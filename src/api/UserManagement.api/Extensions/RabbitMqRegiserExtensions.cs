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
            //var rabbitMQConfig = configuration.GetSection("RabbitMQConnection").Get<RabbitMQConfigurationModel>();
            //if (rabbitMQConfig == null)
            //{
            //    throw new InvalidOperationException("RabbitMQ configuration is missing");
            //}

            //services.AddSingleton(rabbitMQConfig);
            //services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();

            //return services;


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

            // Register default single config for backward compatibility (used by RabbitMqService)
            //if (hosts.TryGetValue("Default", out var defaultConfig))
            //{
                
            //    services.AddSingleton(defaultConfig);
            //}

            
            services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();

            return services;

        }

        public static IServiceCollection AddMessageProcessing(
            this IServiceCollection services)
        {
            //services.AddScoped<IValidator<abcmodel>, AbcValidator>();
            //services.AddScoped<IMessageProcessor<abcmodel>, AbcMessageProcesser>();
            //services.AddHostedService<AbcMessageConsumer>();

            services.AddScoped<IValidator<UserRegistrationModel>, UserRegistrationValidator>();
            services.AddScoped<IMessageProcessor<UserRegistrationModel>, UserRegistrationProcesser>();
            services.AddHostedService<UserRegistrationConsumer>();

            services.AddScoped<IValidator<SlsAgencyModel>, SlsAgencyValidator>();
            services.AddScoped<IMessageProcessor<SlsAgencyModel>, SlsAgencyProcessor>();
            services.AddHostedService<SlsAgencyConsumer>();

            services.AddScoped<IValidator<SnapshotRequestModel>, SnapshotRequestValidator>();
            services.AddScoped<IMessageProcessor<SnapshotRequestModel>, SnapshotRequestProcessor>();
            services.AddHostedService<SnapshotRequestConsumer>();

            services.AddScoped<IValidator<MasterTreasuryConsumerPayload>, MasterTreasuryValidator>();
            services.AddScoped<IMessageProcessor<MasterTreasuryConsumerPayload>, MasterTreasuryProcessor>();
            services.AddHostedService<MasterTreasuryConsumer>();

            services.AddScoped<IValidator<MasterDdoConsumerPayload>, MasterDdoValidator>();
            services.AddScoped<IMessageProcessor<MasterDdoConsumerPayload>, MasterDdoProcessor>();
            services.AddHostedService<MasterDdoConsumer>();


            // ================= ADD ACK CONSUMERS HERE ==================

            // Register ACK Validator
            services.AddSingleton<IValidator<AckPayloadModel>, MQueueAckValidator>();
         
            var ackQueues = new[]
            {
                    MessageQueueConstants.UM_WBJIT_USER_ACK,
                    //MessageQueueConstants.USER_REGISTRATION_QUEUE_ACK,

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