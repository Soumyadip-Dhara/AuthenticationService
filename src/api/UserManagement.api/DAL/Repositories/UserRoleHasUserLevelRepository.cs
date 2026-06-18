using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserRoleHasUserLevelRepository : Repository<UserRoleHasUserLevel, UserManagementDBContext>, IUserRoleHasUserLevelRepository
    {
        public UserRoleHasUserLevelRepository(UserManagementDBContext context) : base(context) { }
    }
}
