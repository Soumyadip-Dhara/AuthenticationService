using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class OtpRepository : Repository<Otp, UserManagementDBContext>, IOtpRepository
    {
        public OtpRepository(UserManagementDBContext context) : base(context) { }
    }
}
