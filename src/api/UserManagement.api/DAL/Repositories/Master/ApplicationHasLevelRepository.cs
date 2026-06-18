using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories.Master
{
    public class ApplicationHasLevelRepository : Repository<ApplicationHasLevel, UserManagementDBContext>, IApplicationHasLevelRepository
    {
        public ApplicationHasLevelRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}