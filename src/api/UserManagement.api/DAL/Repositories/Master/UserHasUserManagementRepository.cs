using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserHasUserManagementRepository : Repository<UserHasUserManagement, UserManagementDBContext>, IUserHasUserManagementRepository
    {
        public UserHasUserManagementRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
