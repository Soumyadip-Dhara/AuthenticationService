using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserLevelHasUserScopeRepository : Repository<UserLevelHasUserScope, UserManagementDBContext>, IUserLevelHasUserScopeRepository
    {
        public UserLevelHasUserScopeRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
