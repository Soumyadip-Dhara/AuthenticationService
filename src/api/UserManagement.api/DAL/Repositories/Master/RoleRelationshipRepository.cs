using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class RoleRelationshipRepository : Repository<RoleRelationship, UserManagementDBContext>, IRoleRelationshipRepository
    {
        public RoleRelationshipRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}