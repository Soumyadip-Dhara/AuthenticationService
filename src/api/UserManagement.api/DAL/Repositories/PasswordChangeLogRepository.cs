using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class PasswordChangeLogRepository : Repository<PasswordChangeLog, UserManagementDBContext>, IPasswordChangeLogRepository
    {
        public PasswordChangeLogRepository(UserManagementDBContext context) : base(context) { }
    }
}
