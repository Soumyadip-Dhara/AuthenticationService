using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class LevelHasAllowedRoleRepository : Repository<LevelHasAllowedRole, UserManagementDBContext>, ILevelHasAllowedRoleRepository
    {
        public LevelHasAllowedRoleRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}