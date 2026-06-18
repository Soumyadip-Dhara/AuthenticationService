using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.DAL.Repositories
{
    public class BlockedIPAddressesRepository : Repository<BlockedIpAddress, UserManagementDBContext>, IBlockedIPAddressesRepository
    {
        public BlockedIPAddressesRepository(UserManagementDBContext userManagementDBContext) : base(userManagementDBContext)
        {

        }
    }
}
