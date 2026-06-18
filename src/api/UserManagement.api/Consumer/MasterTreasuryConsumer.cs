using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ.Constant;

namespace UserManagement.Consumer
{
    public class MasterTreasuryConsumer : RabbitMQConsumerBase<MasterTreasuryConsumerPayload>
    {
        public MasterTreasuryConsumer(
       ILogger<MasterTreasuryConsumer> logger,
       IRabbitMQConnectionFactory connectionFactory,
       IServiceScopeFactory serviceScopeFactory
       //IConfiguration configuration,
       //IMessageErrorLogger errorLogger
       )
       : base(
           logger,
           connectionFactory,
           serviceScopeFactory,
           //configuration,
           //errorLogger, 
           MessageQueueConstants.UM_MASTER_TREASURY,RabbitMqExchangeNames.TreasuryExchange)
        {
        }
    }
}
