using System.Collections.Concurrent;

namespace UserManagement.Throttling
{
    public static class BlacklistStore
    {
        public static readonly ConcurrentDictionary<string, DateTime> BlacklistedIps = new();
    }
}
