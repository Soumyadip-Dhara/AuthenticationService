using AutoMapper;
using UserManagement.Models.MQueue;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;

namespace UserManagement.DAL.Repositories.MQueue
{
    public class ConsumeLogRepository : Repository<ConsumeLog, UserManagementDBContext>, IConsumeLogRepository
    {
        private readonly UserManagementDBContext _dbContext;
        private readonly IMapper _mapper;

        public ConsumeLogRepository(UserManagementDBContext context, IMapper mapper) : base(context)
        {
            _dbContext = context;
            _mapper = mapper;
        }
        public async Task InsertNewLog(NewConsumeLogModel newConsumeLog)
        {
            ConsumeLog consumeLog = _mapper.Map<ConsumeLog>(newConsumeLog);
            await _dbContext.ConsumeLogs.AddAsync(consumeLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}
