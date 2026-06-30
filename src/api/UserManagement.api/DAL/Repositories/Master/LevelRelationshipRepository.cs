using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class LevelRelationshipRepository : Repository<LevelRelationship, UserManagementDBContext>, ILevelRelationshipRepository
    {
        public LevelRelationshipRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
