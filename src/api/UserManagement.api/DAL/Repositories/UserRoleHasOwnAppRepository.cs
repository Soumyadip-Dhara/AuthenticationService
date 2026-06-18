using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserRoleHasOwnAppRepository : Repository<UserRoleHasOwnApp, UserManagementDBContext>, IUserRoleHasOwnAppRepository
    {
        public UserRoleHasOwnAppRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
