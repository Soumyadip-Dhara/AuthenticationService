using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Interfaces.MQueue;

public interface IConsumedAcknowledgementLogRepository : IRepository<ConsumedAcknowledgementLog>
{
    Task<bool> InsertNewLog(ConsumedAcknowledgementLogModel consumedAcknowledgementLog);
}