using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class UserHasModuleManagementRepository : Repository<UserHasModuleManagement, UserManagementDBContext>, IUserHasModuleManagementRepository
    {
        public UserHasModuleManagementRepository(UserManagementDBContext context) : base(context) { }
    }
}
