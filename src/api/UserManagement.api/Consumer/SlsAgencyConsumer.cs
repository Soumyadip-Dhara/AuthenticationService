using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ.Constant;

namespace UserManagement.Consumer
{
    public class SlsAgencyConsumer : RabbitMQConsumerBase<SlsAgencyModel>
    {
        public SlsAgencyConsumer(
       ILogger<SlsAgencyConsumer> logger,
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
           MessageQueueConstants.WBJIT_UM_SLS_AGENCY, RabbitMqExchangeNames.SlsAgencyExchange)
        {
        }
    }
}