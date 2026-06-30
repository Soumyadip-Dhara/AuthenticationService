using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserHasModuleManagementRepository : Repository<UserHasModuleManagement, UserManagementDBContext>, IUserHasModuleManagementRepository
    {
        public UserHasModuleManagementRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
