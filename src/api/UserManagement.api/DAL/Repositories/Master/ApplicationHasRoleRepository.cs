using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories.Master
{
    public class ApplicationHasRoleRepository : Repository<ApplicationHasRole, UserManagementDBContext>, IApplicationHasRoleRepository
    {
        public ApplicationHasRoleRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}