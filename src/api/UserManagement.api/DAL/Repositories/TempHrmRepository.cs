using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
namespace UserManagement.DAL
{
    public class TempHrmRepository : Repository<TempHrm, UserManagementDBContext>, ITempHrmRepository
    {
        public TempHrmRepository(UserManagementDBContext context) : base(context)
        {
        }
    }
}