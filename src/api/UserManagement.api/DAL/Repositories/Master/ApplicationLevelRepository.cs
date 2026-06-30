using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class ApplicationLevelRepository : Repository<ApplicationLevel, UserManagementDBContext>, IApplicationLevelRepository
    {
        public ApplicationLevelRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
