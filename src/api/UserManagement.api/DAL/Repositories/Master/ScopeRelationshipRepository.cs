using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class ScopeRelationshipRepository : Repository<ScopeRelationship, UserManagementDBContext>, IScopeRelationshipRepository
    {
        public ScopeRelationshipRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
