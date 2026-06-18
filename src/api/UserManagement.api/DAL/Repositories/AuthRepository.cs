using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class AuthRepository : Repository<UserMaster, UserManagementDBContext>, IAuthRepository
    {
        public AuthRepository(UserManagementDBContext userManagementDBContext) : base(userManagementDBContext)
        {
        }
    }
}
