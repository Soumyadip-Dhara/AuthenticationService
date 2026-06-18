using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserApplicationHasUserRoleRepository : Repository<UserApplicationHasUserRole, UserManagementDBContext>, IUserApplicationHasUserRoleRepository
    {
        public UserApplicationHasUserRoleRepository(UserManagementDBContext context) : base(context) { }
    }
}