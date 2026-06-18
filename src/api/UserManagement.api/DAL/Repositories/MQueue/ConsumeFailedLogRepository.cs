using AutoMapper;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Repositories.MQueue
{
    public class ConsumeFailedLogRepository : Repository<ConsumeFailedLog, UserManagementDBContext>, IConsumeFailedLogRepository
    {
        private readonly UserManagementDBContext _dbContext;
        private readonly IMapper _mapper;

        public ConsumeFailedLogRepository(UserManagementDBContext context, IMapper mapper) : base(context)
        {
            _dbContext = context;
            _mapper = mapper;
        }
        public async Task InsertNewLog(NewConsumeLogModel newConsumeLog)
        {
            ConsumeFailedLog consumeLog = _mapper.Map<ConsumeFailedLog>(newConsumeLog);
            //ConsumeFailedLog consumeFailedLog = new ConsumeFailedLog
            //{
            //    MessageId = Guid.Parse(newConsumeLog.MessageId),
            //    QueueName = newConsumeLog.QueueName,
            //    ActionStatus = newConsumeLog.ActionStatus,
            //    ConsumedAt = newConsumeLog.ConsumedAt,
            //    FailedAt = newConsumeLog.FailedAt,
            //    ExchangeName = newConsumeLog.ExchangeName,
            //    FailedMessage = newConsumeLog.FailedMessage,
            //    FailedType = newConsumeLog.FailedType,
            //    RaoutingKey = newConsumeLog.RaoutingKey,
            //    MessageBody = newConsumeLog.MessageBody,
            //};
            await _dbContext.ConsumeFailedLogs.AddAsync(consumeLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}
