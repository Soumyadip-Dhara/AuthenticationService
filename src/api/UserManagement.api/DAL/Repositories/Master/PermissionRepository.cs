using Microsoft.EntityFrameworkCore;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories.Master
{
    public class PermissionRepository : Repository<Permission, UserManagementDBContext>, IPermissionRepository
    {
        private readonly UserManagementDBContext _context;

        public PermissionRepository(UserManagementDBContext context) : base(context)
        {
            _context = context;
            _context.Set<Permission>()
                .Include(t => t.RoleHasPermissions);
        }
        
    }
}
