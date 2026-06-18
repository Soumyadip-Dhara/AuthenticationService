using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class AppScopeRepository : Repository<ApplicationScope, UserManagementDBContext>, IAppScopeRepository
    {
        public AppScopeRepository(UserManagementDBContext userManagementDBContext) : base(userManagementDBContext)
        {
        }
    }
}
