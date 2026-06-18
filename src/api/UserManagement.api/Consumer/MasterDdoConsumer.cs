using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ.Constant;

namespace UserManagement.Consumer
{
    public class MasterDdoConsumer : RabbitMQConsumerBase<MasterDdoConsumerPayload>
    {
        public MasterDdoConsumer(
       ILogger<MasterDdoConsumer> logger,
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
           MessageQueueConstants.UM_MASTER_DDO, RabbitMqExchangeNames.DDOExchange)
        {
        }
    }
}
