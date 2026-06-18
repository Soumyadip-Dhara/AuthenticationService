using UserManagement.DAL.Entities;
using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Interfaces.MQueue;

public interface IPublishedAcknowledgementLogRepository : IRepository<PublishedAcknowledgementLog>
{
    Task<bool> InsertNewLog(PublishedAcknowledgementLogModel publishedAcknowledgementLog);
}