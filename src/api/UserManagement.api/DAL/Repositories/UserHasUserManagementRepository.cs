using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserHasUserManagementRepository : Repository<UserHasUserManagement, UserManagementDBContext>, IUserHasUserManagementRepository
    {
        public UserHasUserManagementRepository(UserManagementDBContext context) : base(context) { }
    }
}
