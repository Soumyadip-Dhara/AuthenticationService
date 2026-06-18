using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserRoleHasUserPermissionRepository : Repository<UserRoleHasUserPermission, UserManagementDBContext>, IUserRoleHasUserPermissionRepository
    {
        public UserRoleHasUserPermissionRepository(UserManagementDBContext context) : base(context) { }
    }
}
