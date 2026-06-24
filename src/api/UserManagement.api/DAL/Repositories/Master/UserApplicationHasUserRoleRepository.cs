using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserApplicationHasUserRoleRepository : Repository<UserApplicationHasUserRole, UserManagementDBContext>, IUserApplicationHasUserRoleRepository
    {
        public UserApplicationHasUserRoleRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
