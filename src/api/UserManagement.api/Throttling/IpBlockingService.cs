using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;

namespace UserManagement.Throttling
{
    public class IpBlockingService : IIpBlockingService
    {
        private readonly IBlockedIPAddressesRepository _blockedIPAddressesRepository;

        public IpBlockingService(IBlockedIPAddressesRepository blockedIPAddressesRepository)
        {
            _blockedIPAddressesRepository = blockedIPAddressesRepository;
        }

        public async Task<bool> IsBlockedAsync(string ipAddress)
        {
            var blockedIp = await _blockedIPAddressesRepository.GetSingleSelectedColumnByConditionAsync(ip => ip.IpAddress == ipAddress && ip.BlockedUntil > DateTime.UtcNow, ip => ip.Id);
            return blockedIp != 0;
        }

        public Task<bool> AddToBlocklistAsync(string ipAddress, string reason)
        {

            var blockedIp = new BlockedIpAddress
            {
                IpAddress = ipAddress,
                Reason = reason,
            };
            if (_blockedIPAddressesRepository.Add(blockedIp))
            {
                _blockedIPAddressesRepository.SaveChangesManaged();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

    }
}
