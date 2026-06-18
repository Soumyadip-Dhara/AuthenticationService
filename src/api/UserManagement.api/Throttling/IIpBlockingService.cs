namespace UserManagement.Throttling
{
    public interface IIpBlockingService
    {
        public Task<bool> IsBlockedAsync(string ipAddress);
        public Task<bool> AddToBlocklistAsync(string ipAddress, string reason);
    }
}
