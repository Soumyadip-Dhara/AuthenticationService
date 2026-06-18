using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserLevelHasUserScopeRepository : Repository<UserLevelHasUserScope, UserManagementDBContext>, IUserLevelHasUserScopeRepository
    {
        public UserLevelHasUserScopeRepository(UserManagementDBContext context) : base(context) { }
    }
}
