using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class ScopeRepository : Repository<ScopeMaster, UserManagementDBContext>, IScopeRepository
    {
        public ScopeRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
