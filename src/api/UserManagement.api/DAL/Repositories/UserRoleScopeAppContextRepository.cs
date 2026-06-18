using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserRoleScopeAppContextRepository : Repository<UserRoleScopeAppContext, UserManagementDBContext>,
        IUserRoleScopeAppContextRepository
    {
        public UserRoleScopeAppContextRepository(UserManagementDBContext userManagementDBContext) : base(userManagementDBContext)
        {
        }
    }
}
