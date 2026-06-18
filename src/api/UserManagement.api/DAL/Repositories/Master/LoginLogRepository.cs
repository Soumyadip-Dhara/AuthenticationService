using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class LoginLogRepository : Repository<LoginLog, UserManagementDBContext>, ILoginLogRepository
    {
        public LoginLogRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}