using Microsoft.Extensions.Caching.Memory;

namespace UserManagement.Throttling
{
    public class RateLimitingCache
    {
        public static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    }
}
