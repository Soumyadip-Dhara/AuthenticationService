using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserRoleHasUserLevelRepository : Repository<UserRoleHasUserLevel, UserManagementDBContext>, IUserRoleHasUserLevelRepository
    {
        public UserRoleHasUserLevelRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
