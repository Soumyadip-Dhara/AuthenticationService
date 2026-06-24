using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class RoleRepository : Repository<Role, UserManagementDBContext>, IRoleRepository
    {
        public RoleRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
