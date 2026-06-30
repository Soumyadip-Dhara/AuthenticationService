using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserHasApplicationRepository : Repository<UserHasApplication, UserManagementDBContext>, IUserHasApplicationRepository
    {
        public UserHasApplicationRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
