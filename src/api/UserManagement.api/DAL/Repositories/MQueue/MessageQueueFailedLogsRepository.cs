using AutoMapper;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Enum;
using UserManagement.RbbitMQ;
namespace UserManagement.DAL.Repositories.MQueue
{
    public class MessageQueueFailedLogsRepository : Repository<MessageQueueFailedLog, UserManagementDBContext>, IMessageQueueFailedLogsRepository
    {
        private readonly UserManagementDBContext _dbContext;
        private readonly IMapper _mapper;

        public MessageQueueFailedLogsRepository(UserManagementDBContext context, IMapper mapper) : base(context)
        {
            _dbContext = context;
            _mapper = mapper;
        }

        public async Task InsertLogAsync(AckPayloadModel ackPayload)
        {
            //MessageQueueFailedLog queueFailedLog = _mapper.Map<MessageQueueFailedLog>(ackPayload);
            var queueFailedLog = new MessageQueueFailedLog
            {
                UniqueId = Guid.NewGuid(),
                MessageId = ackPayload.MessageId,
                ActionStatus = ConsumeStatusEnums.PENDING,
                FailedType = ackPayload.FailedType,
                FailedAt = ackPayload.Timestamp.ToUniversalTime(),
                FailedMessage = ackPayload.StatusMsg
            };

            await _dbContext.MessageQueueFailedLogs.AddAsync(queueFailedLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}
