using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class OtpLogRepository : Repository<OtpLog, UserManagementDBContext>, IOtpLogRepository
    {
        public OtpLogRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
