using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Validation;
using System.Security.Claims;
using static OpenIddict.Validation.OpenIddictValidationEvents;
using System.Security.Cryptography;
using System.Text;

namespace UserManagement.api.Authentication;

public class IntrospectionCachingHandler : IOpenIddictValidationHandler<ProcessAuthenticationContext>
{
    private readonly IMemoryCache _cache;

    public IntrospectionCachingHandler(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        if (string.IsNullOrEmpty(context.AccessToken))
        {
            return ValueTask.CompletedTask;
        }

        Console.WriteLine($"\n--- JWE Token Received in API ---\n{context.AccessToken}\n---------------------------------\n");

        // Use SHA256 token hash as cache key
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(context.AccessToken));
        var cacheKey = $"introspection:{Convert.ToBase64String(tokenHash)}";

        if (_cache.TryGetValue(cacheKey, out ClaimsPrincipal? principal) && principal != null)
        {
            // Cache Hit: set principal and skip calling /connect/introspect
            context.AccessTokenPrincipal = principal;
            context.HandleRequest(); 
        }

        return ValueTask.CompletedTask;
    }
}

public class IntrospectionCacheSaver : IOpenIddictValidationHandler<HandleIntrospectionResponseContext>
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan MaxCacheTtl = TimeSpan.FromSeconds(45); // 45 seconds cache limit

    public IntrospectionCacheSaver(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ValueTask HandleAsync(HandleIntrospectionResponseContext context)
    {
        if (context.Principal != null && !string.IsNullOrEmpty(context.Token))
        {
            Console.WriteLine("\n--- Decoded JWE Token Claims (From Introspection) ---");
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }
            Console.WriteLine("-----------------------------------------------------\n");
            var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(context.Token));
            var cacheKey = $"introspection:{Convert.ToBase64String(tokenHash)}";

            // Calculate min(exp, MaxCacheTtl)
            var tokenExp = context.Principal.FindFirst("exp")?.Value;
            var cacheDuration = MaxCacheTtl;
            
            if (long.TryParse(tokenExp, out var expSeconds))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                var timeUntilExp = expirationTime - DateTimeOffset.UtcNow;
                if (timeUntilExp < MaxCacheTtl && timeUntilExp > TimeSpan.Zero)
                {
                    cacheDuration = timeUntilExp;
                }
            }

            _cache.Set(cacheKey, context.Principal, cacheDuration);
        }

        return ValueTask.CompletedTask;
    }
}
