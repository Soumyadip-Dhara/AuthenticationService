using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class RoleHasPermissionRepository : Repository<RoleHasPermission, UserManagementDBContext>, IRoleHasPermissionRepository
    {
        public RoleHasPermissionRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
