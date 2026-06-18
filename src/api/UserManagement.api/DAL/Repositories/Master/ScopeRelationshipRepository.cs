using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class ScopeRelationshipRepository : Repository<ScopeRelationship, UserManagementDBContext>, IScopeRelationshipRepository
    {
        private readonly UserManagementDBContext _context;

        public ScopeRelationshipRepository(UserManagementDBContext context) : base(context)
        {
            _context = context;
        }
    }
}
