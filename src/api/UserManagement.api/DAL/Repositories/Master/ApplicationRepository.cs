using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.DAL.Repositories.Master
{
    public class ApplicationRepository : Repository<Application, UserManagementDBContext>, IApplicationRepository
    {
        public ApplicationRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}
